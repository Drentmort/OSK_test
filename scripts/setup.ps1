#Requires -Version 5.1
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "== OSK Tech setup ==" -ForegroundColor Cyan

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker не найден в PATH." -ForegroundColor Yellow
    Write-Host "Установите Docker Desktop и перезапустите терминал, затем снова запустите этот скрипт."
    Write-Host "  winget install -e --id Docker.DockerDesktop"
    exit 1
}

Write-Host "Starting PostgreSQL + Redis..." -ForegroundColor Green
docker compose up -d
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Waiting for PostgreSQL..." -ForegroundColor Green
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    docker compose exec -T postgres pg_isready -U osk -d osktech 2>$null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $ready) {
    Write-Host "PostgreSQL did not become ready in time." -ForegroundColor Red
    exit 1
}

Write-Host "Restoring dotnet tools..." -ForegroundColor Green
dotnet tool restore

Write-Host "Applying migrations..." -ForegroundColor Green
dotnet run --project src/OskTech.Migrator
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Setup complete. Start the app:" -ForegroundColor Green
Write-Host "  dotnet run --project src/OskTech.Host"
Write-Host "  https://localhost:7079/register"
