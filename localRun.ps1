# ---------------------------------------------------------
# [MoolDangBot] Windows Local Test Script (localRun.ps1)
# ---------------------------------------------------------

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Show-Header {
    Clear-Host
    Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "   MoolDangBot Local Test Automator (Project Osiris)     " -ForegroundColor Cyan
    Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
}

function Show-Step ([string]$msg) {
    Write-Host "`n>> $msg..." -ForegroundColor Green
}

function Show-Error ([string]$msg) {
    Write-Host "`n[!] ERROR: $msg" -ForegroundColor Red
    exit 1
}

# 1. Header & Args
Show-Header
$cleanBuild = $args -contains "--clean"

# 2. Env Check
Show-Step "Checking .env file"
if (-not (Test-Path ".env")) {
    Show-Error ".env file not found. Please copy .env.sample to .env."
}

# 3. Docker Check
Show-Step "Checking Docker Engine"
try {
    docker ps > $null 2>&1
} catch {
    Show-Error "Docker Desktop is not running. Please start it first."
}

# 4. Preliminary Compile Check
Show-Step "Pre-build check (dotnet build)"
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Show-Error "DOTNET Build failed. Please check the source code."
}

# 5. Clean Orphans
Show-Step "Cleaning existing containers (Orphans)"
docker-compose down --remove-orphans

# 6. Docker Build & Up
$buildOpts = if ($cleanBuild) { "--no-cache" } else { "" }
if ($cleanBuild) {
    Write-Host " [CLEAN BUILD] option detected." -ForegroundColor Yellow
}

Show-Step "Docker Build & Compose Up"
docker-compose build $buildOpts
docker-compose up -d

# 7. Health Check
Show-Step "Final Status Check"
Start-Sleep -Seconds 3
docker-compose ps

# 8. Finish
Write-Host "`n[SUCCESS] Local test environment is ready!" -ForegroundColor Green
Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
Write-Host " Admin Dashboard: http://localhost:3000" -ForegroundColor White
Write-Host " API Swagger:     http://localhost:8080/swagger" -ForegroundColor White
Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
Write-Host " View Logs: docker-compose logs -f app" -ForegroundColor Gray
