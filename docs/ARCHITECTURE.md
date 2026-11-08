# Architecture — implemented vs planned

The modular monolith has Domain (entities), Application (pure risk/target/dependency/governance), SDK (read-only connector contracts), Infrastructure (EF/Identity/jobs), API (versioned endpoints), Workers (standalone service), Connectors and collector CLI. React assets are bundled into the API in release images. PostgreSQL is the only required data service.

```mermaid
flowchart LR
  Browser[React console] --> TLS[TLS reverse proxy]
  TLS --> API[ASP.NET Core API]
  API --> PG[(PostgreSQL)]
  Worker[Background worker] --> PG
  CLI[Optional Zabbix collector] -->|HTTPS read only| Zabbix[Zabbix 7.x]
  CLI --> JSON[Validated inventory JSON]
  JSON -->|Authorized import| API
```

```mermaid
flowchart LR
  Source[Explicit source namespace] --> Validate[Bounded schema validation]
  Validate --> Lock[Source advisory lock]
  Lock --> Identity[Source plus stable external ID]
  Identity --> Upsert[Keep newer evidence; never delete missing rows]
  Upsert --> Receipt[Import receipt and audit transaction]
```

```mermaid
stateDiagram-v2
  [*] --> Draft
  Draft --> Submitted: rollback + success criteria
  Submitted --> Rejected: independent approver
  Submitted --> Approved: independent approver + evidence gates
  Rejected --> Submitted
  Approved --> Draft: content edit, version increment
  Submitted --> Draft: content edit
  Rejected --> Draft: content edit
```

Approved is a supported domain state but current history/coverage gaps prevent passing the API approval gate. Scheduled/executing/paused/completed states and infrastructure orchestration are planned, not simulated.

```mermaid
flowchart LR
  Change[Versioned change] --> Job[Bounded durable job]
  Job --> Snapshot[Repeatable-read inventory snapshot]
  Snapshot --> Targets[Attribute / explicit ID target filter]
  Targets --> Graph[Reverse dependency traversal]
  Graph --> Factors[Five weighted factors]
  Factors --> Unknown[Unknown factors preserve uncertainty]
  Unknown --> Result[Range + findings + confidence + pilot]
  Result --> Evidence[Append-only assessment + snapshot hash]
```

```mermaid
flowchart LR
  App[Application] -->|DependsOn| DB[Database server]
  Device[Device] -->|UsesService| App
  VM[VM] -->|HostedOn| Host[Host]
  DB -. changed target impact .-> App
  App -. indirect impact .-> Device
```

Edges point from dependent to dependency; analysis traverses the reverse adjacency. Cycles are visited once. Depth limit produces a finding. Global snapshot limits reject oversized input. This is not a 100k-asset certified graph implementation.

```mermaid
flowchart TD
  Submit[Submit version] --> Owner[Independent approver?]
  Owner --> Evidence[Matching current assessment and scope?]
  Evidence --> Gates[No missing evidence / critical gates?]
  Gates --> Decision[Append-only decision]
  Decision --> Observe[Observe mode: no deployment capability]
```

Ten module boundaries from the master spec are retained in ROADMAP: Discovery, Change, Target, Dependency, Risk, Simulation, Pilot, Governance, Deployment Guard, Outcome. The latter two have no executable infrastructure operations. No arbitrary DLL plugins, cloud endpoints or AI dependency are introduced.
