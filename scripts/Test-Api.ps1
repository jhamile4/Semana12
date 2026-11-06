param(
    [string]$BaseUrl = 'http://localhost:5080',
    [switch]$Cloud
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$uri = $null
if (-not [Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -notin @('http', 'https') -or $uri.AbsolutePath -ne '/' -or $uri.Query -or $uri.Fragment -or $uri.UserInfo) {
    throw 'Usa una URL base HTTP(S) sin ruta, parametros ni credenciales.'
}
if ($Cloud -and ($uri.Scheme -ne 'https' -or $uri.IsLoopback -or $uri.Host -match 'TU-SERVICIO|NOMBRE-REAL')) {
    throw 'La verificacion de nube requiere la URL HTTPS real publicada, no localhost ni marcadores.'
}
if (-not (Get-Command npx.cmd -ErrorAction SilentlyContinue)) { throw 'Instala Node.js con npm para usar Newman, o importa la coleccion en Postman.' }
$base = $BaseUrl.TrimEnd('/')
$mode = if ($Cloud) { 'nube' } else { 'local' }
$reportDir = Join-Path $repoRoot 'artifacts'
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
# Primero comprueba que Swagger y el documento esten accesibles.
$swagger = Invoke-WebRequest -Uri "$base/" -UseBasicParsing -TimeoutSec 120
if ($swagger.StatusCode -ne 200 -or $swagger.Content -notmatch 'swagger-ui') { throw 'No se encontro Swagger en la raiz.' }
# El HTML solo no demuestra que Swagger funcione: validar tambien JavaScript y CSS.
$assetMatches = [regex]::Matches($swagger.Content, '<(?:script|link)\b[^>]*\b(?:src|href)="([^"]+\.(?:js|css)(?:\?[^"\s]*)?)"')
foreach ($assetMatch in $assetMatches) {
    $assetUri = [Uri]::new([Uri]"$base/", $assetMatch.Groups[1].Value)
    $asset = Invoke-WebRequest -Uri $assetUri -UseBasicParsing -TimeoutSec 120
    if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -eq 0) {
        throw 'Un recurso JavaScript o CSS de Swagger no esta disponible.'
    }
}
$spec = Invoke-RestMethod -Uri "$base/swagger/v1/swagger.json" -TimeoutSec 120
$endpointCount = 0
foreach ($path in $spec.paths.PSObject.Properties) {
    $endpointCount += @($path.Value.PSObject.Properties | Where-Object Name -in @('get', 'post', 'put', 'delete')).Count
}
if ($endpointCount -lt 10) { throw "Se esperaban al menos 10 endpoints; se encontraron $endpointCount." }
npx.cmd --yes newman@6.2.3 run (Join-Path $repoRoot 'postman/TallerMecanico.postman_collection.json') --env-var "baseUrl=$base" --timeout-request 120000 --reporters cli,json --reporter-json-export (Join-Path $reportDir "postman-$mode.json")
if ($LASTEXITCODE -ne 0) { throw "Hay pruebas fallidas. Revisa artifacts/postman-$mode.json y la salida anterior." }
Write-Host "Swagger y $endpointCount endpoints verificados. Reporte: artifacts/postman-$mode.json"
if (-not $Cloud) { Write-Host 'Pruebas locales: no sustituyen las capturas de la API publicada requeridas por el laboratorio.' }
