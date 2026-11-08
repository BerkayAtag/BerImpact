# Troubleshooting

- **401:** sign in again; cookies expire after 30 minutes. Check same-origin routing and production HTTPS.
- **400 CSRF:** fetch `/auth/csrf` again after login; send `X-CSRF-TOKEN` with every mutation. Do not disable protection.
- **403:** role lacks permission. PlatformAdministrator does not implicitly grant ChangeApprover.
- **409:** stale change version, conflicting idempotency key, duplicate identity, owner review or unresolved evidence gates. Refresh; do not blindly retry a modified payload with an old key.
- **429:** per-principal/IP rate limit or queue full. Allow jobs to finish; no unbounded retry.
- **Readiness 503:** verify PostgreSQL credentials and migration state. Run migrations with owner credentials, not the runtime user.
- **Job remains Queued:** start standalone worker or enable `RunWorker=true` only for development. Check local worker logs. Expired leases recover up to three claims.
- **Job Failed:** inspect safe diagnostics; a >5000 asset / >50000 edge snapshot is explicitly unsupported. No partial analysis is fabricated.
- **No live assets after import:** test records appear only with the test toggle; stable source IDs and evidence timestamps must be valid. Older incoming data does not replace newer observations.
- **Risk Indeterminate:** expected when required history/dependency/technical evidence is missing. Range is not a failure probability and cannot be converted to Low without evidence.
- **Zabbix export failure:** verify institution certificate trust, API role, explicit endpoint and the 1000-record limit. No TLS bypass option is provided.
- **Windows 0x800711C7:** Application Control blocked an assembly. Request institution-approved signing/allowlisting from the owner; do not disable application control. Linux Docker is a separately supported deployment target.
- **Docker Windows context:** scripts explicitly select `desktop-linux`; the user's default context is not changed.
- **Production key permissions:** Linux API UID 1654 must write its key directory and read certificate secrets. Do not solve this with world-writable keys.
