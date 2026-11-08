using System.Data;
using BerImpact.Application;
using BerImpact.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BerImpact.Infrastructure;

public sealed class AnalysisWorker(IServiceScopeFactory scopes, ILogger<AnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (!await RunOneAsync(stoppingToken)) await Task.Delay(1000, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("Worker iteration failed ({ExceptionType}); retrying", ex.GetType().Name); await Task.Delay(5000, stoppingToken); }
        }
    }

    public async Task<bool> RunOneAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDb>();
        AnalysisJob? job;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var rows = await db.Jobs.FromSqlRaw("""
                SELECT *, xmin FROM "Jobs" WHERE ("Status" = 'Queued' OR ("Status" = 'Running' AND "LeaseUntil" < now()))
                AND "Attempts" < 3 ORDER BY "CreatedAt" LIMIT 1 FOR UPDATE SKIP LOCKED
                """).ToListAsync(ct);
            job = rows.SingleOrDefault();
            if (job == null) {
                await db.Jobs.Where(j => j.Status == "Running" && j.LeaseUntil < DateTime.UtcNow && j.Attempts >= 3)
                    .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, "Failed").SetProperty(j => j.Error, "Worker recovery attempts exhausted."), ct);
                await tx.CommitAsync(ct); return false;
            }
            job.Status = "Running"; job.LeaseUntil = DateTime.UtcNow.AddMinutes(2); job.LeaseToken = Guid.NewGuid(); job.Attempts++;
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            AnalysisInput input;
            await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, timeout.Token))
            {
                var change = await db.Changes.AsNoTracking().SingleAsync(c => c.Id == job.ChangeId, timeout.Token);
                if (change.Version != job.ChangeVersion) throw new InvalidOperationException("Change version changed after queueing; request a new assessment.");
                var assets = await db.Assets.AsNoTracking().Where(a => a.IsTest == change.IsTest).OrderBy(a => a.Id).Take(AnalysisEngine.MaxAssets + 1).ToArrayAsync(timeout.Token);
                var assetIds = assets.Select(a => a.Id).ToArray();
                var edges = await db.Relationships.AsNoTracking().Where(e => assetIds.Contains(e.SourceId) && assetIds.Contains(e.TargetId)).OrderBy(a => a.Id).Take(AnalysisEngine.MaxEdges + 1).ToArrayAsync(timeout.Token);
                var scenarios = Json.Read<Scenario[]>(job.ScenariosJson);
                if (scenarios.Length == 0) scenarios = [new("Temel kapsam", Json.Read<TargetFilter>(change.TargetJson))];
                input = new(change, assets, edges, scenarios, DateTime.UtcNow);
                await tx.CommitAsync(timeout.Token);
            }
            var results = scope.ServiceProvider.GetRequiredService<AnalysisEngine>().Analyze(input);
            timeout.Token.ThrowIfCancellationRequested();
            var snapshot = Json.Write(input);
            var assessment = new Assessment { JobId = job.Id, ChangeId = job.ChangeId, ChangeVersion = input.Change.Version, RulesetVersion = AnalysisEngine.RulesetVersion, SnapshotJson = snapshot, SnapshotHash = Json.Hash(snapshot), ResultJson = Json.Write(results) };
            // xmin fences out a stale worker whose lease has been reclaimed by another worker.
            job.Status = "Completed"; job.LeaseUntil = null;
            db.Assessments.Add(assessment);
            db.AddAudit(job.RequestedBy, "AnalysisCompleted", assessment.Id.ToString(), $"Snapshot {assessment.SnapshotHash}");
            await db.SaveChangesAsync(timeout.Token);
        }
        catch (DbUpdateConcurrencyException) { logger.LogWarning("Job {JobId} lease was reclaimed", job.Id); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            db.ChangeTracker.Clear();
            var current = await db.Jobs.SingleAsync(j => j.Id == job.Id, ct);
            if (current.LeaseToken == job.LeaseToken) {
                current.Status = "Failed"; current.LeaseUntil = null;
                current.Error = ex is InvalidOperationException && (ex.Message.StartsWith("Analysis limit") || ex.Message.StartsWith("Change version changed")) ? ex.Message : "Analysis failed or timed out. Review worker diagnostics.";
                db.AddAudit(job.RequestedBy, "AnalysisFailed", job.Id.ToString(), ex.GetType().Name);
                await db.SaveChangesAsync(ct);
            }
            logger.LogWarning("Job {JobId} failed ({ExceptionType})", job.Id, ex.GetType().Name);
        }
        return true;
    }
}
