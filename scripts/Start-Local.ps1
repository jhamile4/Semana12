param([switch]$LoadOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $repoRoot '.env'
if (-not (Test-Path -LiteralPath $envFile)) {
    Copy-Item -LiteralPath (Join-Path $repoRoot '.env.example') -Destination $envFile
    Write-Host 'Se creo .env con PostgreSQL local de laboratorio.'
}
foreach ($line in Get-Content -LiteralPath $envFile -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
    if ($line.Trim() -match '^postgres(?:ql)?://') {
        [Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', $line.Trim(), 'Process')
        continue
    }
    $pair = $line -split '=', 2
    if ($pair.Count -ne 2 -or $pair[0].Trim() -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Línea inválida en .env.' }
    [Environment]::SetEnvironmentVariable($pair[0].Trim(), $pair[1].Trim(), 'Process')
}
if (-not $env:ConnectionStrings__DefaultConnection -and $env:DATABASE_URL) {
    $env:ConnectionStrings__DefaultConnection = $env:DATABASE_URL
}
if ($env:ConnectionStrings__DefaultConnection -match '^\s*["'']?postgres(?:ql)?://') {
    $env:LocalDatabase__Enabled = 'false'
}
if (-not $env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT = 'Development' }
if (-not $env:ASPNETCORE_URLS) { $env:ASPNETCORE_URLS = 'http://localhost:5080' }
if ($LoadOnly) { return }
if ($env:ConnectionStrings__DefaultConnection -match 'TU_HOST|TU_USUARIO|TU_PASSWORD|TU-ENDPOINT') {
    throw 'Completa los marcadores de la conexion en .env antes de iniciar.'
}
if ($env:LocalDatabase__Enabled -eq 'true') {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Instala e inicia Docker Desktop para la base local, o configura Neon.' }
    docker compose --env-file $envFile -f (Join-Path $repoRoot 'compose.yaml') up -d --wait --wait-timeout 60 postgres
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar PostgreSQL. Revisa Docker Desktop y el puerto POSTGRES_PORT.' }
}
$localUrls = $env:ASPNETCORE_URLS -split ';'
foreach ($localUrl in $localUrls) {
    $listenUri = $null
    if ([Uri]::TryCreate($localUrl, [UriKind]::Absolute, [ref]$listenUri)) {
        if (Get-NetTCPConnection -LocalPort $listenUri.Port -State Listen -ErrorAction SilentlyContinue) {
            throw "El puerto $($listenUri.Port) ya esta ocupado. Deten la instancia anterior de la API con Ctrl+C o desde Visual Studio y vuelve a iniciar."
        }
    }
}
Write-Host 'Swagger: http://localhost:5080/ (o la URL que configuraste en ASPNETCORE_URLS). Ctrl+C detiene la API.'
dotnet run --project (Join-Path $repoRoot 'TallerMecanico/TallerMecanico.csproj') --no-launch-profile
if ($LASTEXITCODE -ne 0) { throw 'La API termino con error. Revisa el mensaje anterior.' }
