using BerImpact.Application;
using BerImpact.Domain;
using BerImpact.Infrastructure;
using BerImpact.Sdk;
using Microsoft.EntityFrameworkCore;

namespace BerImpact.Api;

public sealed record RelationshipInput(Guid SourceId, Guid TargetId, string Type, string Source, string Evidence, EvidenceState State, DateTime ObservedAt);

public static class InventoryEndpoints
{
    public static void MapInventory(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization();
        api.MapGet("/system/capabilities", () => new { mode = "Observe", infrastructureWrites = false, capabilities = new[] { "JsonImport", "ChangeDrafts", "AnalysisJobs", "Simulation", "PilotRecommendation", "HtmlCsvJsonReports" }, limitations = new[] { "No effective GPO targeting", "No connected outcome history", "No infrastructure execution", "Maximum 5000 assets / 50000 edges per snapshot" } });
        api.MapGet("/system/status", async (PlatformDb db, bool? test, CancellationToken ct) => {
            var isTest = test ?? false;
            var assets = db.Assets.Where(a => a.IsTest == isTest);
            var freshAfter = DateTime.UtcNow.AddDays(-7);
            var changes = db.Changes.Where(c => c.IsTest == isTest);
            var assessments = await db.Assessments.Where(a => changes.Any(c => c.Id == a.ChangeId)).OrderByDescending(a => a.CreatedAt).Take(20).Select(a => a.ResultJson).ToArrayAsync(ct);
            return Results.Ok(new { mode = "Observe", isTest, totalAssets = await assets.CountAsync(ct), activeChanges = await changes.CountAsync(c => c.State != ChangeState.Cancelled, ct), pendingApprovals = await changes.CountAsync(c => c.State == ChangeState.Submitted || c.State == ChangeState.UnderReview, ct), criticalFindings = assessments.SelectMany(Json.Read<AnalysisResult[]>).Sum(r => r.Findings.Count(f => f.Severity == "Critical")), staleAssets = await assets.CountAsync(a => a.ObservedAt < freshAfter, ct), importedSources = await assets.Select(a => a.Source).Distinct().CountAsync(ct), recentAssessmentWindow = 20 });
        });
        api.MapGet("/connectors", () => ConnectorCatalog.All);
        api.MapGet("/imports", async (PlatformDb db, CancellationToken ct) => await db.Imports.AsNoTracking().OrderByDescending(i => i.CompletedAt).Take(100).ToArrayAsync(ct));
        api.MapGet("/assets", async (PlatformDb db, string? search, bool? test, int? page, int? pageSize, CancellationToken ct) => {
            var p = Math.Max(1, page ?? 1); var size = Math.Clamp(pageSize ?? 25, 1, 100);
            if (p > 100000) return Results.BadRequest();
            var query = db.Assets.AsNoTracking().Where(a => a.IsTest == (test ?? false));
            if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); if (term.Length > 200) return Results.BadRequest(); query = query.Where(a => a.Name.Contains(term) || a.Source.Contains(term) || a.Location.Contains(term)); }
            return Results.Ok(new { total = await query.CountAsync(ct), page = p, pageSize = size, items = await query.OrderBy(a => a.Name).ThenBy(a => a.Id).Skip((p - 1) * size).Take(size).ToArrayAsync(ct) });
        });
        api.MapGet("/assets/{id:guid}", async (Guid id, PlatformDb db, CancellationToken ct) => await db.Assets.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct) is { } a ? Results.Ok(a) : Results.NotFound());
        api.MapGet("/assets/{id:guid}/relationships", async (Guid id, PlatformDb db, CancellationToken ct) => {
            var edges = await db.Relationships.AsNoTracking().Where(e => e.SourceId == id || e.TargetId == id).Take(201).ToArrayAsync(ct);
            return Results.Ok(new { items = edges.Take(200), truncated = edges.Length > 200 });
        });
        api.MapPost("/assets/import", async (ImportBatch input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            Validate(input);
            var key = ctx.Key(); var hash = Json.Hash(Json.Write(input));
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // Serialize imports for each namespace so concurrent batches cannot lose updates.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({input.Source}, 0))", ct);
            var receipt = await db.Imports.SingleOrDefaultAsync(i => i.Source == input.Source && i.IdempotencyKey == key, ct);
            if (receipt != null) return receipt.ContentHash == hash ? Results.Ok(receipt) : Results.Conflict(new { detail = "Idempotency key reused with a different payload." });
            var ids = input.Records.Select(r => r.ExternalId).ToArray();
            var existing = await db.Assets.Where(a => a.Source == input.Source && ids.Contains(a.ExternalId)).ToDictionaryAsync(a => a.ExternalId, ct);
            foreach (var record in input.Records) {
                var observedAt = StoredTimestamp(record.ObservedAt);
                if (!existing.TryGetValue(record.ExternalId, out var asset)) { asset = new Asset { Source = input.Source, ExternalId = record.ExternalId }; db.Assets.Add(asset); }
                else if (asset.IsTest != input.IsTest) return Results.Conflict(new { detail = "A source identity cannot switch test/live classification." });
                if (asset.ObservedAt > observedAt) continue;
                if (asset.ObservedAt == observedAt && (asset.Name != record.Name || asset.Kind != record.Kind || asset.Os != record.Os || asset.Model != record.Model || asset.Location != record.Location || asset.Critical != record.Critical || asset.Online != record.Online || asset.Evidence != record.Evidence))
                    return Results.Conflict(new { detail = "Conflicting asset evidence at the same observation time. Submit a newer observation." });
                asset.Name = record.Name; asset.Kind = record.Kind; asset.Os = record.Os; asset.Model = record.Model; asset.Location = record.Location;
                asset.Critical = record.Critical; asset.Online = record.Online; asset.Evidence = record.Evidence; asset.ObservedAt = observedAt; asset.IsTest = input.IsTest;
            }
            receipt = new ImportReceipt { Source = input.Source, IdempotencyKey = key, ContentHash = hash, IsTest = input.IsTest, RecordCount = input.Records.Count };
            db.Imports.Add(receipt); db.AddAudit(ctx.Actor(), "InventoryImported", receipt.Id.ToString(), $"Source={input.Source}; Count={receipt.RecordCount}; Test={input.IsTest}", ctx.TraceIdentifier);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(receipt);
        }).RequireAuthorization("InventoryWrite");
        api.MapPost("/relationships", async (RelationshipInput input, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            if (input.SourceId == input.TargetId || string.IsNullOrWhiteSpace(input.Evidence) || input.Evidence.Length > 4000 || string.IsNullOrWhiteSpace(input.Source) || input.Source.Length > 120 || input.Type is not ("DependsOn" or "HostedOn" or "UsesService") || !Enum.IsDefined(input.State) || input.ObservedAt.Kind != DateTimeKind.Utc || input.ObservedAt > DateTime.UtcNow || input.ObservedAt < DateTime.UnixEpoch) return Results.BadRequest();
            input = input with { ObservedAt = StoredTimestamp(input.ObservedAt) };
            var endpoints = await db.Assets.Where(a => a.Id == input.SourceId || a.Id == input.TargetId).ToArrayAsync(ct);
            if (endpoints.Length != 2 || endpoints[0].IsTest != endpoints[1].IsTest) return Results.BadRequest(new { detail = "Both assets must exist in the same test/live partition." });
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var identity = $"relationship:{input.SourceId}:{input.TargetId}:{input.Type}:{input.Source}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({identity}, 0))", ct);
            var prior = await db.Relationships.SingleOrDefaultAsync(e => e.SourceId == input.SourceId && e.TargetId == input.TargetId && e.Type == input.Type && e.Source == input.Source, ct);
            if (prior != null) {
                if (prior.ObservedAt > input.ObservedAt) return Results.Ok(prior);
                if (prior.ObservedAt == input.ObservedAt) return prior.Evidence == input.Evidence && prior.State == input.State ? Results.Ok(prior) : Results.Conflict(new { detail = "Conflicting relationship evidence at the same observation time." });
                prior.Evidence = input.Evidence; prior.State = input.State; prior.ObservedAt = input.ObservedAt;
                db.AddAudit(ctx.Actor(), "RelationshipUpdated", prior.Id.ToString(), "Newer source evidence", ctx.TraceIdentifier);
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(prior);
            }
            var edge = new AssetRelationship { SourceId = input.SourceId, TargetId = input.TargetId, Type = input.Type, Source = input.Source, Evidence = input.Evidence, State = input.State, ObservedAt = input.ObservedAt };
            db.Relationships.Add(edge); db.AddAudit(ctx.Actor(), "RelationshipAdded", edge.Id.ToString(), "", ctx.TraceIdentifier); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Created($"/api/v1/assets/{input.SourceId}/relationships", edge);
        }).RequireAuthorization("InventoryWrite");
        api.MapGet("/audit/events", async (PlatformDb db, int? page, CancellationToken ct) => {
            var p = Math.Clamp(page ?? 1, 1, 100000);
            return new { total = await db.Audit.CountAsync(ct), page = p, items = await db.Audit.AsNoTracking().OrderByDescending(a => a.Id).Skip((p - 1) * 50).Take(50).ToArrayAsync(ct) };
        }).RequireAuthorization("Audit");
    }

    // PostgreSQL timestamptz stores microseconds; .NET DateTime has 100ns ticks.
    private static DateTime StoredTimestamp(DateTime time) => new(time.Ticks - time.Ticks % 10, DateTimeKind.Utc);

    private static void Validate(ImportBatch batch)
    {
        if (string.IsNullOrWhiteSpace(batch.Source) || batch.Source.Length > 120 || batch.Records is not { Count: > 0 and <= 1000 }) throw new ArgumentException("Source and 1..1000 records are required.");
        if (batch.Records.Any(r => r is null)) throw new ArgumentException("Asset records cannot be null.");
        if (batch.Records.Select(r => r.ExternalId).Distinct(StringComparer.Ordinal).Count() != batch.Records.Count) throw new ArgumentException("Duplicate source IDs in batch.");
        foreach (var r in batch.Records) {
            if (string.IsNullOrWhiteSpace(r.ExternalId) || r.ExternalId.Length > 200 || string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 200 || r.Kind is not ("Computer" or "Server" or "Service" or "Application" or "Network") || !Enum.IsDefined(r.Evidence) || r.ObservedAt.Kind != DateTimeKind.Utc || r.ObservedAt < DateTime.UnixEpoch || r.ObservedAt > DateTime.UtcNow.AddMinutes(5) || new[] { r.Os, r.Model, r.Location }.Any(s => s == null || s.Length > 200)) throw new ArgumentException("Invalid asset record. Stable ID, bounded fields and UTC observation timestamp required.");
        }
    }
}

