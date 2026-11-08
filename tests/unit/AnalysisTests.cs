using BerImpact.Application;
using BerImpact.Domain;
using Xunit;

namespace BerImpact.Tests;

public sealed class AnalysisTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
    private static Asset Asset(string name = "device", bool critical = false) => new() { Name = name, Source = "test", ExternalId = Guid.NewGuid().ToString(), IsTest = true, ObservedAt = Now, Evidence = EvidenceState.Verified, Online = true, Critical = critical, Os = "Windows", Model = "M1", Location = "Branch" };
    private static AnalysisInput Input(Asset[] assets, AssetRelationship[]? edges = null, TargetFilter? target = null) => new(new ChangeRequest { RollbackPlan = "Restore backed-up setting" }, assets, edges ?? [], [new("baseline", target ?? new(IsTest: true))], Now);

    [Fact] public void Weighted_factors_produce_exact_index() {
        Factor[] factors = [new("a", "Scope", .25m, 80, ""), new("b", "Dependency", .25m, 40, ""), new("c", "History", .20m, 25, ""), new("d", "Sensitivity", .15m, 100, ""), new("e", "Rollback", .15m, 0, "")];
        Assert.Equal((50m, 50m), AnalysisEngine.ScoreRange(factors));
    }
    [Fact] public void Missing_factor_expands_range_instead_of_becoming_zero() {
        Assert.Equal((25m, 75m), AnalysisEngine.ScoreRange([new("a", "", .5m, 50, ""), new("b", "", .5m, null, "")]));
    }
    [Theory][InlineData(-1)][InlineData(101)] public void Out_of_range_factors_rejected(int value) => Assert.Throws<ArgumentException>(() => AnalysisEngine.ScoreRange([new("a", "", 1m, value, "")]));
    [Fact] public void Invalid_weights_rejected() => Assert.Throws<ArgumentException>(() => AnalysisEngine.ScoreRange([new("a", "", .9m, 10, "")]));
    [Fact] public void Empty_inventory_is_indeterminate_and_gated() {
        var r = new AnalysisEngine().Analyze(Input([])).Single(); Assert.Null(r.Score); Assert.True(r.ReviewRequired); Assert.Equal(100m, r.MaximumScore); Assert.Contains(r.Findings, f => f.RuleId == "TARGET-001");
    }
    [Fact] public void Test_partition_does_not_leak_into_live_analysis() {
        var r = new AnalysisEngine().Analyze(Input([Asset()], target: new(IsTest: false))).Single(); Assert.Equal(0, r.DirectCount);
    }
    [Fact] public void Reverse_traversal_is_cycle_safe_and_explains_path() {
        var server = Asset("server"); var client = Asset("client", true);
        AssetRelationship[] edges = [new() { SourceId = client.Id, TargetId = server.Id, State = EvidenceState.Verified, ObservedAt = Now }, new() { SourceId = server.Id, TargetId = client.Id, State = EvidenceState.Verified, ObservedAt = Now }];
        var r = new AnalysisEngine().Analyze(Input([server, client], edges, new(AssetIds: [server.Id], IsTest: true))).Single();
        Assert.Equal(1, r.DirectCount); Assert.Equal(1, r.IndirectCount); Assert.Equal(new[] { server.Id, client.Id }, r.Impacts.Single(p => p.AssetId == client.Id).Path); Assert.Contains(r.Findings, f => f.RuleId == "CRIT-001");
    }
    [Fact] public void Depth_limit_is_reported_not_silently_hidden() {
        var assets = Enumerable.Range(0, 12).Select(i => Asset(i.ToString())).ToArray();
        var edges = Enumerable.Range(1, 11).Select(i => new AssetRelationship { SourceId = assets[i].Id, TargetId = assets[i - 1].Id, State = EvidenceState.Verified, ObservedAt = Now }).ToArray();
        var r = new AnalysisEngine().Analyze(Input(assets, edges, new(AssetIds: [assets[0].Id], IsTest: true))).Single(); Assert.Contains(r.Findings, f => f.RuleId == "GRAPH-001"); Assert.True(r.ReviewRequired);
    }
    [Fact] public void Stale_data_reduces_confidence_and_excludes_pilot() {
        var a = Asset(); a.ObservedAt = Now.AddDays(-8); var r = new AnalysisEngine().Analyze(Input([a])).Single(); Assert.Equal(1, r.UnknownAssetCount); Assert.Equal(0, r.EvidenceConfidence); Assert.Empty(r.Pilot.Assets);
    }
    [Fact] public void Unresolved_explicit_ids_are_counted_unknown() {
        var a = Asset(); var r = new AnalysisEngine().Analyze(Input([a], target: new(AssetIds: [a.Id, Guid.NewGuid()], IsTest: true))).Single(); Assert.Equal(1, r.UnknownAssetCount);
    }
    [Fact] public void Pilot_represents_distinct_strata_and_excludes_critical_offline() {
        var a = Asset(); var b = Asset(); b.Model = "M2"; var critical = Asset(critical: true); critical.Model = "M3"; var offline = Asset(); offline.Online = false; offline.Model = "M4";
        var pilot = AnalysisEngine.PlanPilot([a, b, critical, offline], Now, 100); Assert.Equal(2, pilot.Assets.Length); Assert.Equal(4, pilot.TotalStrata); Assert.DoesNotContain(critical.Id, pilot.Assets);
    }
    [Fact] public void Gpo_without_rsop_never_claims_effective_targeting() {
        var input = Input([Asset()]); input.Change.Type = ChangeType.GpoModification; var r = new AnalysisEngine().Analyze(input).Single(); Assert.Contains(r.Findings, f => f.RuleId == "GPO-001"); Assert.True(r.ReviewRequired);
    }
    [Fact] public void Oversized_inventory_rejected() => Assert.Throws<InvalidOperationException>(() => new AnalysisEngine().Analyze(Input(Enumerable.Range(0, 5001).Select(_ => Asset()).ToArray())));
    [Fact] public void Owner_cannot_approve_even_without_evidence_gates() {
        var c = new ChangeRequest { OwnerId = "owner", State = ChangeState.Submitted }; Assert.Throws<InvalidOperationException>(() => Governance.Decide(c, "owner", true, false));
    }
    [Fact] public void Missing_evidence_blocks_approval_but_permits_rejection() {
        var c = new ChangeRequest { OwnerId = "owner", State = ChangeState.Submitted }; Assert.Throws<InvalidOperationException>(() => Governance.Decide(c, "reviewer", true, true)); Governance.Decide(c, "reviewer", false, true); Assert.Equal(ChangeState.Rejected, c.State);
    }
    [Fact] public void Editing_invalidates_approval_and_increments_version() {
        var c = new ChangeRequest { State = ChangeState.Approved, Version = 3 }; Governance.InvalidateApproval(c); Assert.Equal(ChangeState.Draft, c.State); Assert.Equal(4, c.Version);
    }
    [Fact] public void Submit_requires_recovery_and_success_criteria() => Assert.Throws<InvalidOperationException>(() => Governance.Submit(new()));
    [Fact] public void Legacy_snapshot_rules_remain_replayable() {
        var input = Input([Asset()]); var engine = new AnalysisEngine();
        Assert.Equal(0m, engine.Analyze(input, "1.0.0")[0].CoverageConfidence);
        Assert.Null(engine.Analyze(input, "1.1.0")[0].CoverageConfidence);
        Assert.Throws<ArgumentException>(() => engine.Analyze(input, "untrusted-code"));
    }
    [Fact] public void Narrowing_known_explicit_targets_does_not_invent_missing_assets() {
        var a = Asset(); var b = Asset(); b.Model = "M2";
        var input = Input([a, b], target: new(Model: "M1", AssetIds: [a.Id, b.Id], IsTest: true));
        Assert.Equal(0, new AnalysisEngine().Analyze(input)[0].UnknownAssetCount);
        Assert.Equal(1, new AnalysisEngine().Analyze(input, "1.1.0")[0].UnknownAssetCount);
    }
    [Fact] public void Stale_alternate_dependency_evidence_is_not_hidden_by_visited_node() {
        var a = Asset(); var b = Asset();
        AssetRelationship[] edges = [new() { SourceId = b.Id, TargetId = a.Id, State = EvidenceState.Verified, ObservedAt = Now }, new() { SourceId = b.Id, TargetId = a.Id, State = EvidenceState.Stale, ObservedAt = Now.AddDays(-9) }];
        var input = Input([a, b], edges, new(AssetIds: [a.Id], IsTest: true));
        Assert.Contains(new AnalysisEngine().Analyze(input)[0].Findings, f => f.RuleId == "GRAPH-002");
        Assert.DoesNotContain(new AnalysisEngine().Analyze(input, "1.1.0")[0].Findings, f => f.RuleId == "GRAPH-002");
    }
    [Fact] public void Rejected_version_requires_edit_before_another_review() {
        var change = new ChangeRequest { State = ChangeState.Rejected, RollbackPlan = "Restore", SuccessCriteria = "Healthy" };
        Assert.Throws<InvalidOperationException>(() => Governance.Submit(change));
        Governance.InvalidateApproval(change); Governance.Submit(change);
        Assert.Equal(2, change.Version); Assert.Equal(ChangeState.Submitted, change.State);
    }
}
