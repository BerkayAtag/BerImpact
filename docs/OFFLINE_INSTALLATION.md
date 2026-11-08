# Offline installation

No runtime CDN, public API, telemetry collector or AI service is required. Connected build-time dependency retrieval is separate from offline operation.

On an approved connected build host: restore locked NuGet/npm dependencies, run tests, build both Docker targets and pull PostgreSQL/nginx images. Run `deployment/offline/Export-Images.ps1`; it exports images, Compose files, docs and SHA-256. Transfer via approved media. Validate SHA-256 at destination, then `docker load -i berimpact-images.tar`. Configure local secret files/certificates and run Compose with `--no-build --pull never`.

Windows alternative: `scripts/Publish-Windows.ps1` emits self-contained win-x64 API/worker, frontend, docs and a hash manifest. PostgreSQL, TLS termination and service identities remain host prerequisites. No SDK or Node is needed on the target when using published binaries.

Offline release acceptance must test a fresh disconnected host: database initialization, account provisioning, login, inventory import, job execution, report download and restore. A packaged application running locally is not evidence that this full disconnected-host test has been completed. Intune/Entra integration is not implemented and cannot create a hidden runtime network requirement.

## Downloaded Linux release bundle

Extract `berimpact-0.1.1-linux-offline.tar.gz` into an empty directory. Validate the archive against the release `SHA256SUMS.txt` first. The archive contains images, Compose configuration, documentation and an internal manifest; it contains no credentials or certificates.

```sh
docker load -i berimpact-images.tar
# Prepare secrets and set BERIMPACT_SECRETS_DIR / BERIMPACT_KEYS_DIR as described in docs/DEPLOYMENT.md.
docker compose -f compose/compose.yml up -d --no-build --pull never
docker compose -f compose/compose.yml ps
```

Use `compose/compose.yml` instead of the source checkout path in the deployment guide. Provision the first administrator with the local CLI described there. No default account is shipped. `images.jsonl` records the exact image IDs included in this bundle.
