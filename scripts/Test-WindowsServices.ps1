param([Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference = 'Stop'
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$names = @('BerImpact API', 'BerImpact Worker')
foreach ($name in $names) {
    if (Get-Service -Name $name -ErrorAction SilentlyContinue) { throw "Service already exists: $name. Use an isolated validation host." }
}
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Service smoke testing requires an elevated isolated validation host.' }
$keys = Join-Path $env:TEMP ('berimpact-service-keys-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $keys | Out-Null
# Test-only connection: liveness and graceful startup must not require an available database.
$connection = 'Host=127.0.0.1;Port=1;Database=unavailable;Username=qa;Timeout=1'
$created = @()
try {
    $api = '"' + (Join-Path $packagePath 'api/BerImpact.Api.exe') + '" --urls http://127.0.0.1:5099 --DataProtectionPath "' + $keys + '" --ConnectionStrings:Database "' + $connection + '"'
    $worker = '"' + (Join-Path $packagePath 'worker/BerImpact.Workers.exe') + '" --ConnectionStrings:Database "' + $connection + '"'
    foreach ($item in @(@($names[0], $api), @($names[1], $worker))) {
        New-Service -Name $item[0] -BinaryPathName $item[1] -StartupType Manual | Out-Null
        $created += $item[0]
        Start-Service -Name $item[0]
        (Get-Service -Name $item[0]).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
    }
    $healthy = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try { $healthy = (Invoke-RestMethod 'http://127.0.0.1:5099/api/v1/health' -TimeoutSec 2).status -eq 'alive' } catch { }
        if ($healthy) { break }
        Start-Sleep -Seconds 1
    }
    if (-not $healthy) { throw 'Windows service liveness failed.' }
    $response = Invoke-WebRequest 'http://127.0.0.1:5099/api/v1/health/ready' -SkipHttpErrorCheck
    if ($response.StatusCode -ne 503) { throw 'Readiness must report unavailable database.' }
    foreach ($name in $names) {
        if ((Get-Service -Name $name).Status -ne 'Running') { throw "Service exited: $name" }
        Restart-Service -Name $name
        (Get-Service -Name $name).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
    }
    Write-Output 'Windows SCM start, liveness, unavailable-database readiness and restart passed.'
} finally {
    foreach ($name in $created) {
        Stop-Service -Name $name -ErrorAction Continue
        & sc.exe delete $name | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not unregister test service: $name" }
    }
}
