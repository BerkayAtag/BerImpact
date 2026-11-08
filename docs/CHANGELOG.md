# Changelog

## 0.1.1 — 2026-11-05

- Fixed atomic import validation and conflicting observations at PostgreSQL timestamp precision.
- Updated relationship evidence safely; older observations cannot replace newer evidence.
- Kept test/live change classification immutable and required a new version after rejection.
- Ruleset 1.2.0 fixes filtered explicit targets and stale alternate graph edges; retains 1.0.0/1.1.0 replay.
- Fixed stale analysis selection, simulation state, import notices and large HTML report lookup cost.
- Hardened Zabbix health response validation and proxy recovery after API replacement.
- Verified 29 unit/connector and 9 PostgreSQL integration tests, HTTPS worker smoke and browser workflows.
- Added a concise screenshot-led GitHub page, CI and downloadable Windows/Linux packages.


## 0.1.0 — 2026-11-05

- Created modular .NET 10 solution, PostgreSQL migrations and React console.
- Added local Identity, cookie/CSRF/RBAC, lockout, bounded API and append-only audit/evidence.
- Added typed read-only connector SDK, idempotent JSON inventory import and bounded Zabbix CLI adapter.
- Implemented versioned changes, target filters, reverse dependency paths, explanatory risk ranges, durable worker jobs and immutable snapshots.
- Added same-snapshot simulations, representative pilot candidates, evidence-gated governance and JSON/HTML/CSV downloads.
- Added local startup, TLS Compose, offline image export and Windows publishing procedures.
- Added behavioral/unit/connector/PostgreSQL integration tests and in-app browser verification.

Known omissions and release gates are documented in ROADMAP.md. No live enterprise connectors or infrastructure writes were exercised.
