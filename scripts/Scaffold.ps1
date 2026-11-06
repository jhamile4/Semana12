$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Start-Local.ps1') -LoadOnly
Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo restaurar dotnet-ef.' }
    $stamp = [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $folder = "../ScaffoldPreview/$stamp"
    dotnet ef dbcontext scaffold 'Name=ConnectionStrings:DefaultConnection' Npgsql.EntityFrameworkCore.PostgreSQL --project TallerMecanico --output-dir "$folder/Models" --context-dir "$folder/Data" --context TallerScaffoldContext --namespace ScaffoldPreview.Models --context-namespace ScaffoldPreview.Data --data-annotations --no-onconfiguring
    if ($LASTEXITCODE -ne 0) { throw 'Scaffold fallo. Comprueba que la base existe y ejecutaste el script SQL.' }
    Write-Host "Scaffold generado en ScaffoldPreview/$stamp. No se sobrescribieron los modelos con validaciones."
} finally { Pop-Location }
