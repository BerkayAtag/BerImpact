using BerImpact.Domain;

namespace BerImpact.Sdk;

public sealed record ConnectorManifest(string Id, string Name, string Version, string[] Capabilities, string[] RequiredPermissions, bool ReadOnly, string Status);
public sealed record ConnectorHealth(bool Healthy, string Message, DateTime CheckedAt);
public sealed record ConnectorPage(IReadOnlyList<AssetRecord> Records, string? NextCheckpoint);
public sealed record AssetRecord(string ExternalId, string Name, string Kind, string Os, string Model, string Location, bool Critical, bool? Online, EvidenceState Evidence, DateTime ObservedAt);
public sealed record ImportBatch(string Source, bool IsTest, IReadOnlyList<AssetRecord> Records);

public interface IReadOnlyConnector
{
    ConnectorManifest Manifest { get; }
    Task<ConnectorHealth> TestConnectionAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<ConnectorPage> DiscoverAsync(string? checkpoint, CancellationToken cancellationToken);
}

public static class ConnectorCatalog
{
    public static IReadOnlyList<ConnectorManifest> All { get; } = [
        new("json-import", "Generic JSON Import", "1.0.0", ["Inventory", "IdempotentImport", "Provenance"], ["IntegrationAdministrator"], true, "Available"),
        new("zabbix", "Zabbix", "0.1.1", ["Hosts"], ["API token: host.get read"], true, "AdapterOnly"),
        new("ad-gpo", "Active Directory / GPO", "0.0.0", [], ["Read directory / GPMC"], true, "Planned"),
        new("sccm", "Microsoft Configuration Manager", "0.0.0", [], ["Administration Service read"], true, "Planned"),
        new("bigfix", "HCL BigFix", "0.0.0", [], ["REST inventory read"], true, "Planned"),
        new("vcenter", "VMware vCenter / Horizon", "0.0.0", [], ["Read-only role"], true, "Planned"),
        new("bermap", "BerMap", "0.0.0", [], [], true, "Planned"),
        new("wfi", "WFI", "0.0.0", [], [], true, "Planned"),
        new("intune", "Intune / Entra (optional)", "0.0.0", [], [], true, "Planned")
    ];
}
