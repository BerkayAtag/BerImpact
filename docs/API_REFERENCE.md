# API reference

Authenticated OpenAPI: `/openapi/v1.json`. API prefix: `/api/v1`. Same-origin cookie authentication; no public registration. All mutations, including login/logout, require `X-CSRF-TOKEN` from `GET /auth/csrf`. Fetch a new token after login. Cookies are HttpOnly/SameSite Strict; production uses Secure. Errors use ProblemDetails where applicable.

| Method / path | Behavior | Permission |
|---|---|---|
| GET /health, /health/ready | Liveness; database/migration readiness | Anonymous |
| GET /auth/csrf | Antiforgery cookie and token | Anonymous |
| POST /auth/login | Identity password sign-in, lockout and rate limit | Anonymous + CSRF |
| GET /auth/me, POST /auth/logout | Current principal / sign out | Signed in |
| GET /system/status?test=false | Bounded dashboard aggregates; test data excluded | Signed in |
| GET /system/capabilities | Explicit supported features and limits | Signed in |
| GET /connectors, /imports | Manifest catalog and recent import receipts | Signed in |
| GET /assets?search=&test=false&page=1&pageSize=25 | Inventory; pageSize ≤100 | Signed in |
| GET /assets/{id}, /assets/{id}/relationships | Asset and ≤200 adjacent edges with truncation flag | Signed in |
| POST /assets/import | Atomic idempotent upsert, ≤1000 records, 4 MiB request limit | Admin / IntegrationAdmin |
| POST /relationships | Evidence-backed relationship; same data partition | Admin / IntegrationAdmin |
| GET /changes?page=1&test=false | 25 change records; omit test to see both classes | Signed in |
| POST /changes | Create client-UUID draft | Admin / ChangeManager |
| GET /changes/{id}, PUT /changes/{id} | Read / edit using current version; invalidate approval | Read / ChangeWrite |
| POST /changes/{id}/submit | Requires version, rollback, success criteria | ChangeWrite |
| POST /changes/{id}/assessments | Queue analysis or ≤5 scenarios; returns 202 and job URL | Admin / Manager / Analyst |
| GET /jobs/{id} | Queued/Running/Completed/Failed + assessment ID | Signed in |
| GET /changes/{id}/assessments | Latest 25 analysis summaries | Signed in |
| GET /risk/assessments/{id} | Immutable full snapshot and result | Signed in |
| POST /changes/{id}/decision | Independent approver; current version/scope and evidence gates | ChangeApprover |
| GET /changes/{id}/decisions | Immutable decision history | Signed in |
| GET /reports/{id}?format=json\|html\|csv | Download assessment report | Signed in |
| GET /audit/events?page=1 | 50 immutable audit events | Admin / Auditor |

Import and assessment requests require `Idempotency-Key` (8–100 characters). Reusing a key with different content returns 409. Other operations use stable IDs, unique constraints or expected change versions; they do not provide universal response replay. Queue capacity is 100 unfinished jobs. Worker lease is 120 seconds, work timeout 90 seconds, crash recovery up to 3 claims. Expired exhausted leases fail explicitly.

Targets support exact source/OS/model/location filters and optional stable UUID list. Empty filters mean all records in the selected test/live partition. This is not LDAP, WMI or an executable query language. Simulations supply `{ scenarios: [{ name, target }, ...] }`; an empty array runs the saved change target.

No deployment, remote command, arbitrary SQL, policy-edit, password-reset or connector-secret endpoint is exposed.

## Conflict semantics (0.1.1)

Observation timestamps are normalized to PostgreSQL microsecond precision. Conflicting content at the same observation time returns 409 without partial writes. Relationships accept newer evidence and ignore stale observations. Change test/live classification cannot be edited. Rejected changes require an edit (new version) before resubmission. Empty or duplicate explicit target IDs return 400.
