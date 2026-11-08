using System.Text;
using System.Text.Encodings.Web;
using BerImpact.Application;
using BerImpact.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BerImpact.Api;

public static class ReportEndpoints
{
    public static void MapReports(this WebApplication app)
    {
        app.MapGet("/api/v1/reports/{id:guid}", async (Guid id, string? format, PlatformDb db, CancellationToken ct) => {
            var a = await db.Assessments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct);
            if (a == null) return Results.NotFound();
            if (format == "json" || format == null) return Results.File(Encoding.UTF8.GetBytes(Json.Write(a)), "application/json", $"berimpact-{id}.json");
            var input = Json.Read<AnalysisInput>(a.SnapshotJson); var results = Json.Read<AnalysisResult[]>(a.ResultJson);
            if (format == "csv") {
                string Cell(string value) => "\"" + ((value.Length > 0 && "=+-@\t\r\n".Contains(value[0])) ? "'" + value : value).Replace("\"", "\"\"") + "\"";
                var lines = new List<string> { "Scenario,MinimumScore,MaximumScore,Direct,Indirect,Unknown,SnapshotHash,RulesetVersion" };
                lines.AddRange(results.Select(r => string.Join(',', new[] { r.Scenario, r.MinimumScore.ToString(System.Globalization.CultureInfo.InvariantCulture), r.MaximumScore.ToString(System.Globalization.CultureInfo.InvariantCulture), r.DirectCount.ToString(), r.IndirectCount.ToString(), r.UnknownAssetCount.ToString(), a.SnapshotHash, a.RulesetVersion }.Select(Cell))));
                return Results.File(Encoding.UTF8.GetBytes(string.Join("\r\n", lines)), "text/csv; charset=utf-8", $"berimpact-{id}.csv");
            }
            if (format != "html") return Results.BadRequest(new { detail = "Supported formats: json, csv, html." });
            string E(string s) => HtmlEncoder.Default.Encode(s);
            var assetsById = input.Assets.ToDictionary(x => x.Id);
            var html = new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><title>BerImpact assessment</title><body><h1>Change Impact Report</h1>");
            html.Append($"<h2>{E(input.Change.Title)}</h2><p>Assessment: {a.Id} · Change version: {a.ChangeVersion} · UTC: {a.CreatedAt:O}</p><p>Snapshot SHA-256: {a.SnapshotHash} · Ruleset: {E(a.RulesetVersion)}</p><p>Observe mode. No infrastructure action was executed. Risk is an index, not a failure probability. Enterprise inventory completeness is unknown.</p>");
            foreach (var r in results) {
                html.Append($"<h2>{E(r.Scenario)}</h2><p>Risk range: {r.MinimumScore}–{r.MaximumScore}. {E(r.Level)}. Review required: {r.ReviewRequired}</p><p>Direct: {r.DirectCount}; indirect: {r.IndirectCount}; unknown: {r.UnknownAssetCount}; evidence confidence: {r.EvidenceConfidence}; freshness: {r.DataFreshness}</p><h3>Findings</h3><ul>");
                foreach (var f in r.Findings) html.Append($"<li><strong>{E(f.RuleId)} v{E(f.RuleVersion)} / {E(f.Severity)}</strong>: {E(f.Explanation)} Mitigation: {E(f.Mitigation)}</li>");
                html.Append("</ul><h3>Factors</h3><ul>");
                foreach (var f in r.Factors) html.Append($"<li>{E(f.Name)} ({f.Weight:P0}): {f.Value?.ToString() ?? "Unknown"}. {E(f.Evidence)}</li>");
                html.Append("</ul><h3>Affected assets</h3><ul>");
                foreach (var path in r.Impacts) { var asset = assetsById[path.AssetId]; html.Append($"<li>{E(asset.Name)} — {E(asset.Source)} / {E(asset.ExternalId)} — {E(path.Classification)} — {E(asset.Evidence.ToString())} — {asset.ObservedAt:O} — Test: {asset.IsTest}</li>"); }
                html.Append($"</ul><h3>Pilot</h3><p>{r.Pilot.Assets.Length} devices; {r.Pilot.RepresentedStrata}/{r.Pilot.TotalStrata} strata represented.</p><ul>");
                foreach (var condition in r.Pilot.Conditions) html.Append($"<li>{E(condition)}</li>");
                html.Append("</ul>");
            }
            html.Append("</body></html>");
            return Results.File(Encoding.UTF8.GetBytes(html.ToString()), "text/html; charset=utf-8", $"berimpact-{id}.html");
        }).RequireAuthorization();
    }
}
