param([string]$Output = 'artifacts/windows-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$destination = [IO.Path]::GetFullPath((Join-Path $root $Output))
if (-not $destination.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must stay in workspace.' }
dotnet publish src/backend/Api -c Release -r win-x64 --self-contained true -p:NuGetLockFilePath=obj/packages.publish-win-x64.lock.json -o "$destination/api"
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
dotnet publish src/backend/Workers -c Release -r win-x64 --self-contained true -p:NuGetLockFilePath=obj/packages.publish-win-x64.lock.json -o "$destination/worker"
if ($LASTEXITCODE -ne 0) { throw 'Worker publish failed.' }
Push-Location src/frontend
npm ci
if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency restore failed.' }
npm run build
if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
Pop-Location
New-Item -ItemType Directory -Force "$destination/api/wwwroot" | Out-Null
Copy-Item src/frontend/dist/* "$destination/api/wwwroot" -Recurse -Force
New-Item -ItemType Directory -Force "$destination/docs" | Out-Null
Copy-Item docs/* "$destination/docs" -Recurse -Force
Get-ChildItem $destination -Recurse -File | Where-Object { $_.FullName -ne (Join-Path $destination 'manifest.json') } | ForEach-Object { [PSCustomObject]@{ Path = $_.FullName.Substring($destination.Length + 1); SHA256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash } } | ConvertTo-Json | Set-Content "$destination/manifest.json" -Encoding utf8
Write-Output "Self-contained package: $destination"
