param([string]$DockerContext = 'desktop-linux')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root
New-Item -ItemType Directory -Force artifacts/offline | Out-Null
docker --context $DockerContext save -o artifacts/offline/berimpact-images.tar berimpact-api:0.1.1 berimpact-worker:0.1.1 postgres:17.11 nginx:stable-alpine
if ($LASTEXITCODE -ne 0) { throw 'Image export failed. Build and pull all images first.' }
New-Item -ItemType Directory -Force artifacts/offline/compose,artifacts/offline/docs | Out-Null
Copy-Item deployment/docker/* artifacts/offline/compose -Recurse -Force
Copy-Item docs/* artifacts/offline/docs -Recurse -Force
docker --context $DockerContext image inspect berimpact-api:0.1.1 berimpact-worker:0.1.1 postgres:17.11 nginx:stable-alpine --format '{{json .}}' | Set-Content artifacts/offline/images.jsonl -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Image manifest export failed.' }
Get-FileHash artifacts/offline/berimpact-images.tar -Algorithm SHA256 | Format-List | Out-File artifacts/offline/SHA256.txt
