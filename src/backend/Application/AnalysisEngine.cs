using BerImpact.Domain;

namespace BerImpact.Application;

public sealed record TargetFilter(string? Source = null, string? Os = null, string? Model = null, string? Location = null, Guid[]? AssetIds = null, bool IsTest = false);
public sealed record Scenario(string Name, TargetFilter Target);
public sealed record AnalysisInput(ChangeRequest Change, Asset[] Assets, AssetRelationship[] Relationships, Scenario[] Scenarios, DateTime CapturedAt);
public sealed record Factor(string Id, string Name, decimal Weight, int? Value, string Evidence);
public sealed record Finding(string RuleId, string RuleVersion, string Severity, string Explanation, string Mitigation, Guid[] Assets, string Source, string Confidence);
public sealed record ImpactPath(Guid AssetId, Guid[] Path, string Classification);
public sealed record PilotPlan(Guid[] Assets, int RepresentedStrata, int TotalStrata, string[] Conditions);
public sealed record AnalysisResult(string Scenario, int DirectCount, int IndirectCount, int UnknownAssetCount, decimal? CoverageConfidence, decimal EvidenceConfidence, decimal DataFreshness, decimal MinimumScore, decimal MaximumScore, decimal? Score, string Level, bool ReviewRequired, Factor[] Factors, Finding[] Findings, ImpactPath[] Impacts, PilotPlan Pilot);

public sealed class AnalysisEngine
{
    public const string RulesetVersion = "1.2.0";
    public const int MaxAssets = 5000;
    public const int MaxEdges = 50000;
    public const int MaxDepth = 8;

    public AnalysisResult[] Analyze(AnalysisInput input, string rulesetVersion = RulesetVersion)
    {
        if (rulesetVersion is not ("1.0.0" or "1.1.0" or RulesetVersion)) throw new ArgumentException("Unsupported ruleset version.");
        if (input.Assets.Length > MaxAssets || input.Relationships.Length > MaxEdges)
            throw new InvalidOperationException("Analysis limit exceeded; narrow the inventory scope. No partial result was produced.");
        if (input.Scenarios.Length is < 1 or > 5) throw new ArgumentException("One to five scenarios required.");
        return input.Scenarios.Select(s => AnalyzeScenario(input, s, rulesetVersion)).ToArray();
    }

    public static bool Matches(Asset a, TargetFilter f) => a.IsTest == f.IsTest
        && (string.IsNullOrWhiteSpace(f.Source) || a.Source == f.Source)
        && (string.IsNullOrWhiteSpace(f.Os) || a.Os == f.Os)
        && (string.IsNullOrWhiteSpace(f.Model) || a.Model == f.Model)
        && (string.IsNullOrWhiteSpace(f.Location) || a.Location == f.Location)
        && (f.AssetIds is not { Length: > 0 } || f.AssetIds.Contains(a.Id));

    private static AnalysisResult AnalyzeScenario(AnalysisInput input, Scenario scenario, string version)
    {
        var population = input.Assets.Where(a => a.IsTest == scenario.Target.IsTest).ToArray();
        var direct = population.Where(a => Matches(a, scenario.Target)).ToArray();
        var known = population.ToDictionary(a => a.Id);
        var paths = direct.ToDictionary(a => a.Id, a => new ImpactPath(a.Id, [a.Id], "Direct"));
        var queue = new Queue<Guid>(paths.Keys);
        var reverse = input.Relationships.GroupBy(e => e.TargetId).ToDictionary(g => g.Key, g => g.ToArray());
        var bounded = false;
        var uncertainEdges = false;
        while (queue.TryDequeue(out var current))
        {
            if (!reverse.TryGetValue(current, out var edges)) continue;
            foreach (var edge in edges)
            {
                if (version == RulesetVersion && known.ContainsKey(edge.SourceId))
                    uncertainEdges |= edge.State != EvidenceState.Verified || edge.ObservedAt > input.CapturedAt || input.CapturedAt - edge.ObservedAt > TimeSpan.FromDays(7);
                if (!known.ContainsKey(edge.SourceId) || paths.ContainsKey(edge.SourceId)) continue;
                if (paths[current].Path.Length > MaxDepth) { bounded = true; continue; }
                uncertainEdges |= edge.State != EvidenceState.Verified || input.CapturedAt - edge.ObservedAt > TimeSpan.FromDays(7);
                paths[edge.SourceId] = new(edge.SourceId, [.. paths[current].Path, edge.SourceId], "Indirect");
                queue.Enqueue(edge.SourceId);
            }
        }
        var affected = paths.Keys.Select(id => known[id]).ToArray();
        var fresh = affected.Count(a => IsFresh(a, input.CapturedAt));
        var verified = affected.Count(a => a.Evidence == EvidenceState.Verified && IsFresh(a, input.CapturedAt));
        var unknown = affected.Length - verified;
        var requestedMissing = scenario.Target.AssetIds?.Except(version == RulesetVersion ? known.Keys : direct.Select(a => a.Id)).Count() ?? 0;
        unknown += requestedMissing;
        var findings = new List<Finding>();
        void Add(string id, string severity, string explanation, string mitigation, Guid[] ids) => findings.Add(new(id, version, severity, explanation, mitigation, ids, "Snapshot", "Evidence-dependent"));
        if (unknown > 0) Add("DATA-001", "High", "Some targets are missing, stale or unverified.", "Collect current verified evidence before approval.", affected.Where(a => a.Evidence != EvidenceState.Verified || !IsFresh(a, input.CapturedAt)).Select(a => a.Id).ToArray());
        if (direct.Length == 0) Add("TARGET-001", "High", "No targets resolved. Absence of records does not prove absence of impact.", "Check filters and inventory coverage.", []);
        if (bounded) Add("GRAPH-001", "High", "Traversal reached its depth limit; impact is incomplete.", "Review dependency paths and split the scope.", []);
        if (uncertainEdges) Add("GRAPH-002", "High", "Indirect impact includes unverified or stale relationships.", "Verify dependency evidence.", []);
        var critical = affected.Where(a => a.Critical).Select(a => a.Id).ToArray();
        if (critical.Length > 0) Add("CRIT-001", "Critical", "Critical assets are in the impact scope.", "Obtain service-owner review and a representative pilot.", critical);
        if (string.IsNullOrWhiteSpace(input.Change.RollbackPlan)) Add("ROLLBACK-001", "High", "No rollback plan is recorded.", "Document a supported recovery procedure.", []);
        if (input.Change.Type == ChangeType.GpoModification) Add("GPO-001", "High", "Attribute selection does not establish effective Group Policy targeting; RSoP is unavailable.", "Validate links, inheritance, security/WMI filters and loopback with RSoP evidence.", direct.Select(a => a.Id).ToArray());
        Factor[] factors = [
            new("scope", "Impact Scope", .25m, direct.Length == 0 ? null : Math.Min(100, (int)Math.Ceiling(100m * affected.Length / Math.Max(1, population.Length))), "Affected / imported population; enterprise coverage is unknown."),
            new("dependency", "Critical Dependency", .25m, critical.Length > 0 ? 100 : null, "Known critical flags; absence of known dependencies does not establish graph completeness."),
            new("history", "Historical Failure", .20m, null, "Outcome history is not connected."),
            new("sensitivity", "Technical Sensitivity", .15m, input.Change.TechnicalSensitivity, input.Change.FactorEvidence),
            new("rollback", "Rollback Difficulty", .15m, input.Change.RollbackDifficulty, input.Change.FactorEvidence)
        ];
        // Preserve replay of snapshots produced before the completeness correction.
        if (version == "1.0.0") factors[1] = new("dependency", "Critical Dependency", .25m, direct.Length == 0 || bounded || uncertainEdges ? null : critical.Length > 0 ? 100 : input.Relationships.Length == 0 ? null : 0, "Known critical flags and dependency snapshot; missing graph is unknown.");
        var (min, max) = ScoreRange(factors);
        var unknownFactors = factors.Any(f => f.Value == null);
        if (unknownFactors) Add("DATA-002", "High", "Risk factors lack evidence; the score is a range, not a probability.", "Collect evidence for the unknown factors.", []);
        var denominator = Math.Max(1, affected.Length + requestedMissing);
        return new(scenario.Name, direct.Length, affected.Length - direct.Length, unknown,
            // Enterprise inventory completeness is not measurable from imported records alone.
            version == "1.0.0" ? 0 : null, Math.Round(100m * verified / denominator, 1), Math.Round(100m * fresh / denominator, 1), min, max,
            unknownFactors ? null : min, unknownFactors ? "Indeterminate" : min >= 80 ? "Critical" : min >= 60 ? "High" : min >= 30 ? "Medium" : "Low",
            unknownFactors || unknown > 0 || bounded || findings.Any(f => f.Severity is "Critical" or "High"), factors, findings.ToArray(), paths.Values.ToArray(), PlanPilot(direct, input.CapturedAt, 100));
    }

    public static (decimal Minimum, decimal Maximum) ScoreRange(IEnumerable<Factor> source)
    {
        var factors = source.ToArray();
        if (factors.Any(f => f.Weight < 0 || f.Value is < 0 or > 100) || factors.Sum(f => f.Weight) != 1m)
            throw new ArgumentException("Weights must total 1 and values must be 0..100.");
        return (factors.Sum(f => f.Weight * (f.Value ?? 0)), factors.Sum(f => f.Weight * (f.Value ?? 100)));
    }

    public static bool IsFresh(Asset asset, DateTime now) => asset.ObservedAt <= now && now - asset.ObservedAt <= TimeSpan.FromDays(7);

    public static PilotPlan PlanPilot(Asset[] assets, DateTime now, int limit)
    {
        var strata = assets.GroupBy(a => (a.Os, a.Model, a.Location)).ToArray();
        var candidates = strata.Select(g => g.Where(a => !a.Critical && a.Online == true && a.Evidence == EvidenceState.Verified && IsFresh(a, now)).OrderBy(a => a.Id).FirstOrDefault()).Where(a => a != null).Select(a => a!.Id).Take(limit).ToArray();
        return new(candidates, candidates.Length, strata.Length, ["Recommendation only; no deployment is executed.", "Verify service-owner approval and supported rollback.", "Stop on any critical service regression; define numeric health gates before rollout.", "Unknown, offline, stale and critical devices are excluded from the first pilot."]);
    }
}
