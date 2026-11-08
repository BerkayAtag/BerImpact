# Development

Prerequisites: .NET SDK 10.0.403, Node 24.22.1 (Vite 8 requires Node ≥22.12), Docker Desktop Linux engine for PostgreSQL. Package locks are committed. Start with `dotnet tool restore`, `dotnet restore`, and `npm ci` in `src/frontend`. There is no runtime cloud dependency.

On Windows, `scripts/Start-Local.ps1` provisions a dedicated `berimpact-dev-db` container on 127.0.0.1:55432 with random credentials, named persistent volume `berimpact-dev-data`, migrations, random local administrator, built frontend and API on 127.0.0.1:5080. Secrets stay under `.local` with restricted ACLs. The script does not touch production sources or change Docker’s default context. It supports Windows PowerShell 5.1 and newer. Run `scripts/Stop-Local.ps1` to stop its matching API process; the database remains intact.

This host’s Application Control later blocked `BerImpact.Sdk.dll`. Linux Docker is the verified runtime for the current build: `scripts/Start-LocalDocker.ps1` reuses the isolated development PostgreSQL and local account, builds a Linux image, applies migrations and publishes the console only to loopback. It uses a development cookie/key configuration and embedded worker; do not expose it externally. Production uses the separate TLS Compose definition.

For a new machine where native account provisioning is blocked, initialize the named PostgreSQL database and protected password file using Docker, then run the image’s `--create-user true --username <name> --roles PlatformAdministrator` command with a password piped to stdin (`docker run -i`). Do not pass the password as a command argument. This is an explicit local provisioning step, not a built-in default account.

Hot reload: run the API on port 5080, then `npm run dev` in `src/frontend`. Vite binds only 127.0.0.1:5173 and proxies API traffic. Release images serve bundled static assets directly. No CDN/fonts are fetched at runtime.

Changes: edit domain/application logic first, add appropriate behavioral tests, create EF migrations, build frontend and run tests. Never label unimplemented connector capabilities Available. Keep `docs/ROADMAP.md` honest. Preserve all observations’ source identity, evidence state, UTC timestamp and test classification.
