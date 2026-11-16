function Resolve-BerImpactDotnet {
    $candidates = @()
    foreach ($sdkRoot in @($env:DOTNET_ROOT, $env:DOTNET_ROOT_X64)) {
        if ($sdkRoot) { $candidates += Join-Path $sdkRoot 'dotnet.exe' }
    }
    $candidates += @(Get-Command dotnet -CommandType Application -All -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
    foreach ($sdkRoot in @("$env:ProgramFiles/dotnet", "$env:USERPROFILE/.dotnet", "$env:LOCALAPPDATA/Microsoft/dotnet", "$env:LOCALAPPDATA/BerImpact/dotnet")) {
        $candidates += Join-Path $sdkRoot 'dotnet.exe'
    }
    # --version honors global.json, unlike merely detecting an installed runtime.
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
        try { $version = & $candidate --version 2>$null } catch { continue }
        if ($LASTEXITCODE -eq 0 -and $version -match '^10\.') { return $candidate }
    }
    throw 'Compatible .NET SDK not found. Install the SDK specified by global.json or set DOTNET_ROOT to its directory.'
}
