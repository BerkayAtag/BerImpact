# Windows service installation — deployment procedure

Build `scripts/Publish-Windows.ps1` on an approved build host. Copy the immutable package to a versioned program directory. Configure `appsettings.Production.json` beside each binary with external PostgreSQL connection (password via protected `DatabasePasswordFile`), persistent DataProtectionPath for the API, and loopback Kestrel URL behind institutional TLS reverse proxy. Use distinct least-privilege service identities. Grant only read/execute to binaries and write to designated log/key directories. DPAPI keys are machine-bound; plan certificate-based recovery if moving hosts.

Run migrations with the owner connection before switching to runtime credentials. Pipe a password to `BerImpact.Api.exe --create-user true --username <chosen-name> --roles PlatformAdministrator`. Ensure logs and shell history never contain the password.

The API calls `UseWindowsService`; the worker calls `AddWindowsService`. An authorized server administrator can register them using `New-Service` with fully qualified quoted executable paths, automatic startup and the chosen credentials; configure restart recovery and TLS forwarding according to local policy. No service was registered on this workstation by the development task.

Upgrade: stop services during maintenance, back up database and key material, validate migration on a restored copy, deploy new version directory, run migrations, switch executable paths, start worker/API and validate health/login/test workflow. Roll back application binaries only when the schema remains compatible; otherwise restore the validated backup under an approved recovery plan.

Uninstall: stop and unregister services; retain database, keys and evidence until explicitly authorized archival/deletion. Do not remove shared PostgreSQL or certificates automatically.

**0.1.2 validation (2026-11-17):** Native Windows unit and PostgreSQL integration tests passed on the development workstation. GitHub Windows CI also publishes the self-contained package and validates API/worker SCM registration, start, liveness, database-unavailable readiness, restart, stop and unregister from a directory containing spaces. This service smoke uses an intentionally unavailable database; production service identities, TLS, recovery and institutional AD/GPO/EDR configurations require separate acceptance. Earlier Application Control failures were not reproduced; no protection was disabled. Use institution-approved signing/allowlisting where policy requires it.

Build prerequisites: install the SDK matching global.json and Node.js. For an SDK outside PATH, set DOTNET_ROOT. Choose a new output directory for each publish; existing outputs are rejected. The target server needs neither SDK nor Node.js for the self-contained package. `Test-WindowsServices.ps1` requires PowerShell 7 and an elevated, isolated validation host; it must not be run against an existing BerImpact service installation.
