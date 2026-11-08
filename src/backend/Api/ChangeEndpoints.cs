using System.Text.Json;
using BerImpact.Application;
using BerImpact.Domain;
using BerImpact.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BerImpact.Api;

public sealed record ChangeInput(Guid Id, string Title, string Description, ChangeType Type, JsonElement Before, JsonElement After, TargetFilter Target, string RollbackPlan, int? RollbackDifficulty, int? TechnicalSensitivity, string FactorEvidence, string SuccessCriteria, int Version = 1);
public sealed record AssessmentRequest(Scenario[] Scenarios);
public sealed record DecisionRequest(Guid AssessmentId, int Version, bool Approved, string Reason);

public static class ChangeEndpoints
{
    public static void MapChanges(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization();
        api.MapGet("/changes", async (PlatformDb db, int? page, bool? test, CancellationToken ct) => {
            var p = Math.Clamp(page ?? 1, 1, 100000);
            var query = db.Changes.AsNoTracking();
            if (test.HasValue) query = query.Where(c => c.IsTest == test.Value);
            return new { total = await query.CountAsync(ct), page = p, items = await query.OrderByDescending(c => c.UpdatedAt).Skip((p - 1) * 25).Take(25).ToArrayAsync(ct) };
        });
        api.MapGet("/changes/{id:guid}", async (Guid id, PlatformDb db, CancellationToken ct) => await db.Changes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct) is { } change ? Results.Ok(change) : Results.NotFound());
        api.MapPost("/changes", async (ChangeInput input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            Validate(input);
            var existing = await db.Changes.SingleOrDefaultAsync(c => c.Id == input.Id, ct);
            if (existing != null) return Results.Conflict(new { detail = "Change ID already exists; use the edit endpoint with its current version." });
            var change = new ChangeRequest { Id = input.Id, OwnerId = ctx.Actor() }; Apply(input, change);
            db.Changes.Add(change); db.AddAudit(ctx.Actor(), "ChangeCreated", change.Id.ToString(), "Version 1", ctx.TraceIdentifier); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/changes/{change.Id}", change);
        }).RequireAuthorization("ChangeWrite");
        api.MapPut("/changes/{id:guid}", async (Guid id, ChangeInput input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            Validate(input); if (input.Id != id) return Results.BadRequest();
            var change = await db.Changes.SingleOrDefaultAsync(c => c.Id == id, ct); if (change == null) return Results.NotFound();
            if (change.Version != input.Version) return Results.Conflict(new { detail = "Change version mismatch." });
            if (change.IsTest != input.Target.IsTest) return Results.Conflict(new { detail = "A change cannot switch test/live classification. Create a separate draft." });
            Apply(input, change); Governance.InvalidateApproval(change);
            db.AddAudit(ctx.Actor(), "ChangeEditedApprovalInvalidated", change.Id.ToString(), $"Version {change.Version}", ctx.TraceIdentifier); await db.SaveChangesAsync(ct); return Results.Ok(change);
        }).RequireAuthorization("ChangeWrite");
        api.MapPost("/changes/{id:guid}/submit", async (Guid id, VersionRequest input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            var change = await db.Changes.SingleOrDefaultAsync(c => c.Id == id, ct); if (change == null) return Results.NotFound();
            if (change.Version != input.Version) return Results.Conflict();
            try { Governance.Submit(change); } catch (InvalidOperationException e) { return Results.Conflict(new { detail = e.Message }); }
            change.UpdatedAt = DateTime.UtcNow; db.AddAudit(ctx.Actor(), "ChangeSubmitted", id.ToString(), $"Version {change.Version}", ctx.TraceIdentifier); await db.SaveChangesAsync(ct); return Results.Ok(change);
        }).RequireAuthorization("ChangeWrite");
        api.MapPost("/changes/{id:guid}/assessments", async (Guid id, AssessmentRequest request, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            if (request.Scenarios == null || request.Scenarios.Length > 5 || request.Scenarios.Any(s => s == null || string.IsNullOrWhiteSpace(s.Name) || s.Name.Length > 100 || s.Target == null)) return Results.BadRequest();
            foreach (var scenario in request.Scenarios) ValidateTarget(scenario.Target);
            var change = await db.Changes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
            if (change == null) return Results.NotFound();
            if (request.Scenarios.Any(s => s.Target.IsTest != change.IsTest)) return Results.BadRequest(new { detail = "Scenario data class must match the change." });
            var key = ctx.Key(); var actor = ctx.Actor(); var hash = Json.Hash(id + Json.Write(request));
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(72389421)", ct);
            var prior = await db.Jobs.SingleOrDefaultAsync(j => j.RequestedBy == actor && j.IdempotencyKey == key, ct);
            if (prior != null) return prior.RequestHash == hash ? Results.Accepted($"/api/v1/jobs/{prior.Id}", prior) : Results.Conflict(new { detail = "Idempotency key reused with a different request." });
            if (await db.Jobs.CountAsync(j => j.Status == "Queued" || j.Status == "Running", ct) >= 100) return Results.Problem("Analysis queue is full. Retry later.", statusCode: 429);
            var job = new AnalysisJob { ChangeId = id, ChangeVersion = change.Version, RequestedBy = actor, IdempotencyKey = key, RequestHash = hash, ScenariosJson = Json.Write(request.Scenarios) };
            db.Jobs.Add(job); db.AddAudit(actor, "AnalysisQueued", job.Id.ToString(), "", ctx.TraceIdentifier); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Accepted($"/api/v1/jobs/{job.Id}", job);
        }).RequireAuthorization("Analyze");
        api.MapGet("/jobs/{id:guid}", async (Guid id, PlatformDb db, CancellationToken ct) => {
            var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == id, ct); if (job == null) return Results.NotFound();
            return Results.Ok(new { job.Id, job.ChangeId, job.Status, job.Error, job.Attempts, assessmentId = await db.Assessments.Where(a => a.JobId == id).Select(a => (Guid?)a.Id).SingleOrDefaultAsync(ct) });
        });
        api.MapGet("/changes/{id:guid}/assessments", async (Guid id, PlatformDb db, CancellationToken ct) => await db.Assessments.AsNoTracking().Where(a => a.ChangeId == id).OrderByDescending(a => a.CreatedAt).Take(25).Select(a => new { a.Id, a.CreatedAt, a.ChangeVersion, a.RulesetVersion, a.SnapshotHash, a.ResultJson }).ToArrayAsync(ct));
        api.MapGet("/risk/assessments/{id:guid}", async (Guid id, PlatformDb db, CancellationToken ct) => await db.Assessments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct) is { } assessment ? Results.Ok(assessment) : Results.NotFound());
        api.MapPost("/changes/{id:guid}/decision", async (Guid id, DecisionRequest input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000) return Results.BadRequest();
            var change = await db.Changes.SingleOrDefaultAsync(c => c.Id == id, ct); if (change == null) return Results.NotFound();
            if (change.Version != input.Version) return Results.Conflict();
            var assessment = await db.Assessments.SingleOrDefaultAsync(a => a.Id == input.AssessmentId && a.ChangeId == id && a.ChangeVersion == change.Version, ct);
            if (assessment == null) return Results.Conflict(new { detail = "A matching assessment is required." });
            var stale = assessment.CreatedAt < DateTime.UtcNow.AddHours(-24);
            var inputSnapshot = Json.Read<AnalysisInput>(assessment.SnapshotJson);
            var matchingScope = inputSnapshot.Scenarios.Length == 1 && Json.Write(inputSnapshot.Scenarios[0].Target) == Json.Write(Json.Read<TargetFilter>(change.TargetJson));
            var reviewRequired = Json.Read<AnalysisResult[]>(assessment.ResultJson).Any(r => r.ReviewRequired) || stale || !matchingScope;
            try { Governance.Decide(change, ctx.Actor(), input.Approved, reviewRequired); } catch (InvalidOperationException e) { return Results.Conflict(new { detail = e.Message }); }
            change.UpdatedAt = DateTime.UtcNow;
            var decision = new ApprovalDecision { ChangeId = id, ChangeVersion = change.Version, AssessmentId = assessment.Id, ActorId = ctx.Actor(), Approved = input.Approved, Reason = input.Reason };
            db.Approvals.Add(decision); db.AddAudit(ctx.Actor(), input.Approved ? "ChangeApproved" : "ChangeRejected", id.ToString(), input.Reason, ctx.TraceIdentifier); await db.SaveChangesAsync(ct); return Results.Ok(change);
        }).RequireAuthorization("Approve");
        api.MapGet("/changes/{id:guid}/decisions", async (Guid id, PlatformDb db, CancellationToken ct) => await db.Approvals.AsNoTracking().Where(a => a.ChangeId == id).OrderBy(a => a.CreatedAt).Take(100).ToArrayAsync(ct));
    }

    public sealed record VersionRequest(int Version);
    private static void Apply(ChangeInput i, ChangeRequest c) {
        c.IsTest = i.Target.IsTest;
        c.Title = i.Title.Trim(); c.Description = i.Description; c.Type = i.Type; c.BeforeJson = i.Before.GetRawText(); c.AfterJson = i.After.GetRawText(); c.TargetJson = Json.Write(i.Target); c.RollbackPlan = i.RollbackPlan; c.RollbackDifficulty = i.RollbackDifficulty; c.TechnicalSensitivity = i.TechnicalSensitivity; c.FactorEvidence = i.FactorEvidence; c.SuccessCriteria = i.SuccessCriteria;
    }
    private static void Validate(ChangeInput i) {
        if (i.Id == Guid.Empty || string.IsNullOrWhiteSpace(i.Title) || i.Title.Length > 200 || !Enum.IsDefined(i.Type) || i.Target == null || i.Before.ValueKind != JsonValueKind.Object || i.After.ValueKind != JsonValueKind.Object || i.Before.GetRawText().Length > 16000 || i.After.GetRawText().Length > 16000 || i.RollbackDifficulty is < 0 or > 100 || i.TechnicalSensitivity is < 0 or > 100 || new[] { i.Description, i.RollbackPlan, i.FactorEvidence, i.SuccessCriteria }.Any(s => s == null || s.Length > 4000)) throw new ArgumentException("Invalid change fields. Title, object configurations and bounded factor values are required.");
        if ((i.RollbackDifficulty != null || i.TechnicalSensitivity != null) && string.IsNullOrWhiteSpace(i.FactorEvidence)) throw new ArgumentException("Manual factor values require an evidence explanation.");
        ValidateTarget(i.Target);
    }
    private static void ValidateTarget(TargetFilter f) {
        if (f.AssetIds is { Length: > 1000 } || new[] { f.Source, f.Os, f.Model, f.Location }.Any(s => s?.Length > 200)) throw new ArgumentException("Target filter exceeds limits.");
        if (f.AssetIds is { } ids && (ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)) throw new ArgumentException("Target asset IDs must be nonempty and unique.");
    }
}
