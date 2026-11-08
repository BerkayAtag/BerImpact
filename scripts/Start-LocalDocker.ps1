param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
if (-not (Test-Path '.local/db-password')) { throw 'Provision the local development database first; see DEVELOPMENT.md.' }
$engine = docker --context desktop-linux info --format '{{.OSType}}'
if ($LASTEXITCODE -ne 0 -or $engine -ne 'linux') { throw 'Docker Linux engine must be running. Select Linux containers in Docker Desktop.' }
docker --context desktop-linux start berimpact-dev-db | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Development database container is missing or cannot start.' }
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    docker --context desktop-linux exec berimpact-dev-db pg_isready -U berimpact -d berimpact *> $null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 1
}
if ($LASTEXITCODE -ne 0) { throw 'Development database did not become ready.' }
$network = docker --context desktop-linux network ls --filter name=^berimpact-dev$ --format '{{.Name}}'
if (-not $network) { docker --context desktop-linux network create berimpact-dev | Out-Null }
$networks = docker --context desktop-linux inspect berimpact-dev-db --format '{{json .NetworkSettings.Networks}}' | ConvertFrom-Json
if (-not $networks.'berimpact-dev') { docker --context desktop-linux network connect berimpact-dev berimpact-dev-db }
if (-not $NoBuild) {
    docker --context desktop-linux build -f deployment/docker/Dockerfile --target api -t berimpact-api:0.1.1 .
    if ($LASTEXITCODE -ne 0) { throw 'API image build failed.' }
}
$passwordPath = (Resolve-Path '.local/db-password').Path
$common = @('--context','desktop-linux','run','--rm','--network','berimpact-dev','-e','ConnectionStrings__Database=Host=berimpact-dev-db;Database=berimpact;Username=berimpact','-e','DatabasePasswordFile=/run/secrets/db-password','-e','Logging__LogLevel__Default=Warning','--mount',"type=bind,source=$passwordPath,target=/run/secrets/db-password,readonly")
& docker @common berimpact-api:0.1.1 --migrate true
if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
$existing = docker --context desktop-linux ps -a --filter name=^berimpact-dev-api$ --format '{{.Names}}'
if ($existing) { docker --context desktop-linux rm -f berimpact-dev-api | Out-Null }
docker --context desktop-linux run -d --name berimpact-dev-api --label app=berimpact-dev --network berimpact-dev -p 127.0.0.1:5080:8080 -e 'ConnectionStrings__Database=Host=berimpact-dev-db;Database=berimpact;Username=berimpact' -e DatabasePasswordFile=/run/secrets/db-password -e ASPNETCORE_ENVIRONMENT=Development -e RunWorker=true -e Logging__LogLevel__Default=Warning --mount "type=bind,source=$passwordPath,target=/run/secrets/db-password,readonly" berimpact-api:0.1.1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Development API failed to start.' }
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try { $health = Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/health/ready' -TimeoutSec 2; break } catch { Start-Sleep -Seconds 1 }
}
if (-not $health) { throw 'Readiness check failed.' }
Write-Output 'Docker development console ready: http://127.0.0.1:5080. Development only; production uses TLS Compose.'
