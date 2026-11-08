$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$pidFile = Join-Path $root '.local/api.pid'
if (Test-Path $pidFile) {
    $apiPid = [int](Get-Content $pidFile -Raw)
    $process = Get-CimInstance Win32_Process -Filter "ProcessId=$apiPid"
    if ($process -and $process.CommandLine -like "*$root*BerImpact.Api.dll*") { Stop-Process -Id $apiPid }
    elseif ($process) { throw 'PID belongs to a different process; not stopping it.' }
}
Write-Output 'BerImpact API stopped. PostgreSQL volume is preserved.'
