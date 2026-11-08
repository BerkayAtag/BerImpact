param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
function New-RandomHex([int]$Length) {
    $bytes = New-Object byte[] $Length
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [BitConverter]::ToString($bytes).Replace('-', '')
}
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$local = Join-Path $root '.local'
New-Item -ItemType Directory -Force $local | Out-Null
# Development secrets and logs are restricted to the current Windows identity and SYSTEM.
if ($IsWindows -or $env:OS -eq 'Windows_NT') {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls $local /inheritance:r /grant:r "${identity}:(OI)(CI)F" 'SYSTEM:(OI)(CI)F' | Out-Null
}
if (-not (Test-Path "$local/db-password")) {
    $password = New-RandomHex 24
    [IO.File]::WriteAllText("$local/db-password", $password)
    [IO.File]::WriteAllText("$local/database.env", "POSTGRES_PASSWORD=$password")
}
$container = & docker --context desktop-linux ps -a --filter name=^berimpact-dev-db$ --format '{{.Names}}'
if (-not $container) {
    & docker --context desktop-linux run -d --name berimpact-dev-db --label app=berimpact-dev --env-file "$local/database.env" -e POSTGRES_USER=berimpact -e POSTGRES_DB=berimpact -p 127.0.0.1:55432:5432 -v berimpact-dev-data:/var/lib/postgresql/data postgres:17.11 | Out-Null
} else { & docker --context desktop-linux start berimpact-dev-db | Out-Null }
if ($LASTEXITCODE -ne 0) { throw 'Local PostgreSQL failed to start.' }
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    & docker --context desktop-linux exec berimpact-dev-db pg_isready -U berimpact -d berimpact | Out-Null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 1
}
$env:ConnectionStrings__Database = 'Host=127.0.0.1;Port=55432;Database=berimpact;Username=berimpact;Password=' + (Get-Content "$local/db-password" -Raw).Trim()
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
$env:RunWorker = 'true'
$env:DataProtectionPath = "$local/keys"
$env:Logging__LogLevel__Default = 'Warning'
if (-not $NoBuild) {
    & dotnet build BerImpact.sln
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
    Push-Location src/frontend
    & npm ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
    & npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    Pop-Location
}
$apiDir = Join-Path $root 'src/backend/Api'
New-Item -ItemType Directory -Force "$apiDir/wwwroot" | Out-Null
Copy-Item src/frontend/dist/* "$apiDir/wwwroot" -Recurse -Force
$apiDll = "$apiDir/bin/Debug/net10.0/BerImpact.Api.dll"
& dotnet $apiDll --migrate true
if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
if (-not (Test-Path "$local/credentials.json")) {
    $adminPassword = 'Bi!' + (New-RandomHex 20) + 'a1'
    $adminPassword | & dotnet $apiDll --create-user true --username local.admin --roles PlatformAdministrator
    if ($LASTEXITCODE -ne 0) { throw 'Local user provisioning failed.' }
    @{ username = 'local.admin'; password = $adminPassword; url = 'http://127.0.0.1:5080' } | ConvertTo-Json | Set-Content "$local/credentials.json" -Encoding utf8
}
try { $health = Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/health/ready' -TimeoutSec 2 } catch { $health = $null }
if (-not $health) {
    $process = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @($apiDll) -WorkingDirectory $apiDir -WindowStyle Hidden -RedirectStandardOutput "$local/api.log" -RedirectStandardError "$local/api-error.log" -PassThru
    $process.Id | Set-Content "$local/api.pid"
}
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try { $health = Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/health/ready' -TimeoutSec 2; break } catch { Start-Sleep -Seconds 1 }
}
if (-not $health) { throw 'API readiness failed; inspect .local/api-error.log.' }
Write-Output 'BerImpact ready: http://127.0.0.1:5080. Local credentials: .local/credentials.json (not printed).'

