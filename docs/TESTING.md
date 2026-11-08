# Verification — 2026-11-08

This is development-release evidence, not enterprise production certification.

| Check | Observed result |
|---|---|
| `dotnet build BerImpact.sln --no-restore` after restore | 10 projects, zero warnings/errors, including collector CLI |
| Frontend `npm run build` and `npm run lint` | Passed |
| Linux .NET unit/connector contract tests | 29 passed, zero failed/skipped |
| Real PostgreSQL integration tests | 9 passed, zero failed/skipped; PostgreSQL 17.11 |
| EF model versus migrations | No pending model changes; fresh isolated database migrations succeeded |
| HTTPS Compose smoke | Passed with explicit CA trust; Secure/HttpOnly cookie, import under runtime role, separate worker, test/live dashboard isolation; ruleset 1.2.0 |
| Browser/IAB | Login, import, draft, analysis and model-filter simulation exercised with synthetic test data; direct targets 2 versus 1 |
| Windows self-contained publish | API and worker package built; native execution/service installation not certified |
| Isolated development restore | Custom-format dump restored into a new database; five table counts matched; broader restore acceptance remains pending |

Latest complete Linux TRX files: `artifacts/linux-test-results/_a5fa5252ed4a_2026-11-05_19_50_21_net10.0.trx` (29) and `_a5fa5252ed4a_2026-11-05_19_50_25_net10.0.trx` (9). Filenames contain UTC host time. Test results are local artifacts outside Git.

Coverage includes unknown risk factors/ranges, weighted scores, old ruleset replay, cyclic/reverse dependencies, stale evidence, bounded analysis, pilot exclusion/stratification, role and CSRF denial, idempotent import/conflict, preservation of newer observations, no partial invalid import, immutable snapshots and canonical hash replay, scenario comparison, HTML/CSV/JSON reports, review gates, optimistic concurrency and database audit mutation rejection. The latest integration flow also verifies that editing a queued change causes its job to fail and that cross-partition scenarios are rejected.

## Reproduce

Use an isolated development PostgreSQL instance. Tests refuse non-loopback database hosts and create/drop UUID-named test databases. They never connect to institutional systems.

```powershell
docker --context desktop-linux build -f deployment/docker/Dockerfile --target tests -t berimpact-tests:0.1.1 .
# Prepare a protected env file with BERIMPACT_TEST_DATABASE pointing to the test DB container's loopback port 5432.
docker --context desktop-linux run --rm --network container:berimpact-dev-db --env-file .local/test-container.env --mount "type=bind,source=$PWD/artifacts/linux-test-results,target=/results" berimpact-tests:0.1.1
```

`tests/integration/https_smoke.py` uses the separately provisioned local validation Compose account and explicit `tls.crt` trust. It creates only test-class data and does not disable TLS certificate verification or modify machine trust. It is a validation fixture, not a production provisioning script.

## Environment limitations

Windows Application Control rejected locally generated assemblies with `0x800711C7`; the latest native test run could not discover tests. That failed TRX is retained. No protection was disabled. Linux is an intended deployment target and the successful test result above comes from that target. Windows hosting needs institution-approved signing/allowlisting and a separate service acceptance test.

After the interrupted session Docker was in Windows-container mode. With no running Windows containers, it was returned to Linux mode and the development database restarted without deleting volumes. `Start-LocalDocker.ps1` now starts the existing database and checks readiness.

Pending: real AD/GPO/SCCM/BigFix/Zabbix source acceptance, large-scale load tests (30k/100k/1m), HA/failover, disconnected-host acceptance, full restore/login/replay drill, penetration test and Windows service installation. Existing tests do not establish those claims. See ROADMAP.md and design/VERIFICATION.md.

## 0.1.1 browser regression checks

IAB, 1440 × 1000, local Linux API: edited a test draft from v1 to v2; verified the historical-analysis warning; completed a new ruleset 1.2.0 job; downloaded CSV; submitted for review; compared two scenarios (2 versus 1 direct targets). Screenshots in site/assets use synthetic test records. API integration tests additionally cover null import entries, atomic timestamp conflicts, relationship refresh, immutable data partition, empty/duplicate target IDs and report encoding.

## Release verification — 2026-11-08

GitHub Actions [Verify run](https://github.com/BerkayAtag/BerImpact/actions/runs/42474095983) passed on Ubuntu and Windows runners: Linux unit/integration tests plus frontend build/lint; Windows unit tests. This adds native Windows test evidence, but does not certify Windows service installation. HTTPS Compose smoke was repeated successfully with ruleset 1.2.0. The live Pages site passed light/dark image loading, mobile overflow checks and browser console checks. Windows publishing now uses a separate runtime lock file and preserves portable restore locks. Release archives are checked against internal SHA-256 manifests and include no local credentials.
