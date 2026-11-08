$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
if (-not $env:BERIMPACT_TEST_DATABASE) {
    $env:BERIMPACT_TEST_DATABASE = 'Host=127.0.0.1;Port=55432;Database=postgres;Username=berimpact;Password=' + (Get-Content '.local/db-password' -Raw).Trim()
}
dotnet test BerImpact.sln --logger 'trx;LogFilePrefix=berimpact' --results-directory artifacts/test-results
if ($LASTEXITCODE -ne 0) { throw 'Backend tests failed.' }
Push-Location src/frontend
npm run build
if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
npm run lint
if ($LASTEXITCODE -ne 0) { throw 'Frontend lint failed.' }
Pop-Location
