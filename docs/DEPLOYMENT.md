# Deployment

## Linux Compose

`deployment/docker/compose.yml` defines PostgreSQL 17.11, one-time migrator, non-root API, independent worker and TLS nginx proxy. API includes built frontend. Only the proxy is published, to loopback by default. Database network is internal; volumes survive container replacement.

Prepare a protected secrets directory outside source control containing `db-owner-password`, `db-runtime-password`, `key-protection.pfx`, `key-protection-password`, `tls.crt`, `tls.key`. Generate independent random database passwords. Use an institution-issued TLS certificate with the deployed hostname and a separate encryption certificate for Data Protection. Set `BERIMPACT_SECRETS_DIR` and `BERIMPACT_KEYS_DIR`; the key directory must be writable by container UID 1654. `BERIMPACT_BIND_ADDRESS` defaults to 127.0.0.1 and HTTPS port to 8443.

```text
docker compose -f deployment/docker/compose.yml build
docker compose -f deployment/docker/compose.yml up -d
docker compose -f deployment/docker/compose.yml ps
```

The database initializer creates a non-owner runtime role. The migrator uses a separate owner secret; after migrations the runtime role cannot mutate evidence/audit rows or migration history. Never reuse owner credentials for the API. For external PostgreSQL, create equivalent roles and run migrations with the owner before starting services.

Provision the first named administrator locally by piping a strong password to `docker compose ... run --rm -T api --create-user true --username <chosen-name> --roles PlatformAdministrator`. There is no default administrator password. Provision an independent ChangeApprover separately for segregation of duties. Do not put passwords on command lines or in shell history.

Verify HTTPS certificate trust, `/api/v1/health/ready`, login, CSRF rejection and a test-only import/analysis. Proxy health checks database readiness through the API. Worker status is observable from durable job completion; a dedicated worker heartbeat endpoint is not implemented.

The default web subnet is `172.29.197.0/24`, its dynamic allocation range is `172.29.197.0/25`, and the trusted proxy is fixed at `172.29.197.254`. This keeps the proxy outside the dynamic pool. If the subnet overlaps an institutional route, set `BERIMPACT_WEB_SUBNET`, `BERIMPACT_WEB_DYNAMIC_RANGE` and `BERIMPACT_PROXY_IP` together. Only that proxy may forward client IP and HTTPS scheme; do not trust all networks.

## Windows Server

See WINDOWS_INSTALLATION.md. Windows self-contained publishing is supported by script, but service installation and Application Control approval require environment validation.

## High availability — design only

Multiple API instances require shared Data Protection keys/certificate, PostgreSQL HA and external load balancing. Jobs use `FOR UPDATE SKIP LOCKED`, leases and xmin fencing. Multi-worker failure recovery must be load-tested before HA claims. Rate limits are local to an instance. Do not claim enterprise HA certification from these primitives.

Release images use pinned .NET/Node/PostgreSQL versions. nginx stable tag should be resolved to an approved immutable digest in a release manifest. Rebuild and revalidate security patches on a connected build host, then transfer an offline bundle.
