namespace BerImpact.Domain;

public enum EvidenceState { Verified, Inferred, Unknown, Stale, Conflicting, Unsupported }
public enum ChangeState { Draft, Submitted, UnderReview, Approved, Rejected, Cancelled }
public enum ChangeType { GpoModification, RegistryModification, WindowsFeatureUpdate, WindowsQualityUpdate, SoftwareDeployment, DriverUpdate, SccmDeployment, BigFixBaseline, IntuneConfiguration, ServiceConfiguration, FirewallRule, Certificate, NetworkConfiguration, DnsDhcp, VMwareInfrastructure, Custom }

public sealed class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "Computer";
    public string Os { get; set; } = "";
    public string Model { get; set; } = "";
    public string Location { get; set; } = "";
    public bool Critical { get; set; }
    public bool? Online { get; set; }
    public bool IsTest { get; set; }
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public DateTime ObservedAt { get; set; }
    public uint RowVersion { get; set; }
}

// Source depends on Target. Impact traverses in reverse from a changed target.
public sealed class AssetRelationship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }
    public string Type { get; set; } = "DependsOn";
    public string Source { get; set; } = "";
    public string Evidence { get; set; } = "";
    public EvidenceState State { get; set; }
    public DateTime ObservedAt { get; set; }
}

public sealed class ChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public bool IsTest { get; set; }
    public ChangeType Type { get; set; }
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string TargetJson { get; set; } = "{}";
    public string RollbackPlan { get; set; } = "";
    public int? RollbackDifficulty { get; set; }
    public int? TechnicalSensitivity { get; set; }
    public string FactorEvidence { get; set; } = "";
    public string SuccessCriteria { get; set; } = "";
    public ChangeState State { get; set; }
    public int Version { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
}

public sealed class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChangeId { get; set; }
    public int ChangeVersion { get; set; }
    public string RequestedBy { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string ScenariosJson { get; set; } = "[]";
    public string Status { get; set; } = "Queued";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeaseUntil { get; set; }
    public Guid? LeaseToken { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public uint RowVersion { get; set; }
}

public sealed class Assessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid ChangeId { get; set; }
    public int ChangeVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string RulesetVersion { get; set; } = "1.0.0";
    public string SnapshotJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public string SnapshotHash { get; set; } = "";
}

public sealed class ApprovalDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChangeId { get; set; }
    public int ChangeVersion { get; set; }
    public Guid AssessmentId { get; set; }
    public string ActorId { get; set; } = "";
    public bool Approved { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEvent
{
    public long Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Detail { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}

public sealed class ImportReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public int RecordCount { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public bool IsTest { get; set; }
}
