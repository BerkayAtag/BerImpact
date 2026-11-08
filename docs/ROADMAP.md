# Milestones and honest release status

This repository is an implementation in progress, not an enterprise-certified release.

1. Foundation: API, Identity, RBAC, audit, PostgreSQL migrations, React console, local and Compose deployment. Gate: build, PostgreSQL migration and authentication tests, browser workflow.
2. Inventory: compiled SDK and validated idempotent JSON import; read-only Zabbix adapter contract tests. Pending: scheduled source jobs, secret store, AD/GPO, SCCM, BigFix live validation and strong-key reconciliation.
3. Impact: changes, bounded reverse dependency traversal, target attribute filters, deterministic explainable risk ranges, immutable evidence snapshots and durable analysis jobs. Pending: effective policy/RSoP, full rules DSL and large graph UI.
4. Simulation: shared snapshot scenario comparison, stratified pilot, JSON/HTML/CSV reports. Pending: PDF renderer, configurable rollout waves and historical factor calibration.
5. Governance: state transitions, separation of duties, evidence gate, immutable decisions. Pending: multi-stage workflows, optional BerMap/WFI/vCenter integrations, outcome collection.
6. Orchestration: deliberately unavailable. Add only after explicit write scopes, approval-bound version locks, health gates and supported rollback are implemented and tested.
7. Hardening: pending offline host validation, Windows service installation test, restore drill, HA/load tests at 30k/100k/1m scale and production security assessment.

See TESTING.md for executed evidence; no milestone is complete merely because code exists.
