# Changelog

## 0.1.2 — 2026-11-17

- Fixed Windows development startup with quoted paths and SDK discovery honoring global.json and DOTNET_ROOT.
- Fail early when Docker/PostgreSQL is unavailable; refuse reused Windows publish directories to prevent stale package contents.
- Recover exhausted analysis leases even when the queue remains busy.
- Refresh CSRF before login so expired sessions can sign in again without reloading the application.
- Add simulation pagination, show retained explicit asset scope and prevent editing test/live classification.
- Export UTF-8 CSV with a BOM for Windows Excel while retaining formula-injection escaping.
- Add PostgreSQL recovery/CSV regressions and Windows CI self-contained publishing plus SCM lifecycle smoke checks.
- Retain Observe Mode and ruleset 1.2.0; no database migration or design-template change is required.

## 0.1.1 — 2026-11-08

- Fixed atomic import validation and conflicting observations at PostgreSQL timestamp precision.
- Updated relationship evidence safely; older observations cannot replace newer evidence.
- Kept test/live change classification immutable and required a new version after rejection.
- Ruleset 1.2.0 fixes filtered explicit targets and stale alternate graph edges; retains 1.0.0/1.1.0 replay.
- Fixed stale analysis selection, simulation state, import notices and large HTML report lookup cost.
- Hardened Zabbix health response validation and proxy recovery after API replacement.
- Verified 29 unit/connector and 9 PostgreSQL integration tests, HTTPS worker smoke and browser workflows.
- Added a concise screenshot-led GitHub page, CI and downloadable Windows/Linux packages.
- Isolated Windows publish lock files so portable locked restore remains reproducible.


## 0.1.0 — 2026-11-05

- Created modular .NET 10 solution, PostgreSQL migrations and React console.
- Added local Identity, cookie/CSRF/RBAC, lockout, bounded API and append-only audit/evidence.
- Added typed read-only connector SDK, idempotent JSON inventory import and bounded Zabbix CLI adapter.
- Implemented versioned changes, target filters, reverse dependency paths, explanatory risk ranges, durable worker jobs and immutable snapshots.
- Added same-snapshot simulations, representative pilot candidates, evidence-gated governance and JSON/HTML/CSV downloads.
- Added local startup, TLS Compose, offline image export and Windows publishing procedures.
- Added behavioral/unit/connector/PostgreSQL integration tests and in-app browser verification.

Known omissions and release gates are documented in ROADMAP.md. No live enterprise connectors or infrastructure writes were exercised.
