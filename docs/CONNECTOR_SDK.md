# Read-only connector SDK

`IReadOnlyConnector` exposes a compiled manifest, connection test and cancellable async sequence of pages. Manifests report capabilities, version, permissions and actual implementation status. Arbitrary third-party DLL loading is unsupported.

Stable identity is namespace + immutable source ID. Namespace must distinguish forest/site/instance. Display hostname is not an identity key. JSON import validates the whole batch, holds a source-specific transaction lock, deduplicates stable IDs, refuses test/live identity conversion and preserves newer observations when older data arrives. A failed batch never deletes prior inventory. Cross-source auto-merge is intentionally absent.

## Zabbix collector

The adapter implements HTTPS `host.get` against Zabbix 7.x with Bearer API token, normal certificate validation, redirects disabled by the CLI, 20-second requests, bounded body and limited backoff on 429/5xx. It reads hosts and selected inventory attributes only. Availability and business criticality are not inferred from host enabled status. Evidence is marked Inferred because source inventory is not endpoint validation.

```text
dotnet run --project src/collectors/cli -- https://zabbix.example.internal/api_jsonrpc.php zabbix-site-a /protected/token-file /protected/new-export.json
```

Run only against an explicitly authorized endpoint, using an API role permitted to read hosts. CLI output is real-data JSON, imported through the console by IntegrationAdministrator. Token is read from a protected file, never printed. The CLI refuses to overwrite an existing output file. The adapter does not support incremental checkpoints, scheduling, host writes, problem history or arbitrary JSON-RPC methods. More than 1000 hosts fails the CLI export explicitly; no silently truncated inventory is accepted.

Contract tests use a fake HTTP handler and validate method, token scheme, stable IDs, unknown availability, insecure URL rejection, API failures and oversized results. No live Zabbix instance was contacted.

Official interface: [Zabbix host.get](https://www.zabbix.com/documentation/7.0/en/manual/api/reference/host/get), [API authentication](https://www.zabbix.com/documentation/7.0/en/manual/api).

AD/GPO, SCCM, BigFix, vCenter/Horizon, BerMap, WFI and optional Intune manifests are marked Planned with no claimed capabilities. Full connector lifecycle, health history, scheduling, encrypted secret management, multi-forest identity reconciliation and source-specific capability discovery remain roadmap work.
