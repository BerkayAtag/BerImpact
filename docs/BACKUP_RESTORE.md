# Backup and restore

Back up PostgreSQL using `pg_dump -Fc` under an authorized backup role and export role definitions separately. Keep database credentials outside command history. Back up the Data Protection key ring and its separate certificate/private key/password securely; database-only restore does not preserve sessions or encrypted key material. Protect inventory exports as enterprise data.

For the isolated development container, a task-local pre-upgrade dump was made before replacing PostgreSQL 17.7 with 17.11. The named data volume was preserved. This is not a production backup policy.

On 2026-11-05 a fresh custom-format development dump was restored successfully with `--no-owner --exit-on-error` into a new UUID-named database, then removed after comparison. Counts matched: 2 assets, 1 change, 3 assessments, 13 audit events and 5 migrations. Evidence: `artifacts/restore-check.txt`. This verifies backup readability and table-count preservation; restored login/session continuity and full assessment replay still require a broader drill.

Restore into a **new** isolated database using `pg_restore --exit-on-error --no-owner`, then validate migration history, counts, constraints, audit mutation rejection, login and replay of a stored assessment. Compare canonical snapshot hashes. Do not restore over a live database as a test. Keep writes paused until validation and authorized cutover.

Production restore drills, point-in-time recovery, retention, encryption and off-host storage must be validated for the institution. No unmeasured RPO/RTO guarantee is claimed.
