# ---------------------------------------------------------
# [MoolDangBot] UI High-Speed Development Launch (uiRun.ps1)
# ---------------------------------------------------------

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Show-Header {
    Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "   MoolDangBot UI Port-3000 Reloader (Project Osiris)   " -ForegroundColor Cyan
    Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
}

function Show-Step ([string]$msg) {
    Write-Host "`n>> $msg..." -ForegroundColor Green
}

# 1. Configuration
$UI_PROJECT_NAME = "MooldangBot.Studio"
$UI_PATH = "./$UI_PROJECT_NAME"

# 2. Header
Show-Header

# 3. Stop Docker Services for Port 3000 (Nginx & Studio)
Show-Step "Docker Nginx & Studio 컨테이너 일시 중지 (3000번 포트 확보)"
docker-compose stop nginx studio

# 4. NPM Dependencies Check
Show-Step "UI 의존성 설치 확인 ($UI_PROJECT_NAME)"
Push-Location "$UI_PATH"
try {
    if (-not (Test-Path "node_modules\@sveltejs\kit")) {
        Write-Host " [Notice] 핵심 의존성이 누락되었습니다. 새로 설치합니다..." -ForegroundColor Yellow
        npm.cmd install --include=dev --legacy-peer-deps
    }
}
finally {
    Pop-Location
}

# 5. Start Host UI Dashboard on Port 3000
Show-Step "UI 호스트 개발 서버 가동 (Port: 3000)"
Write-Host " [Info] $UI_PROJECT_NAME 기동을 시작합니다. Ctrl+C를 눌러 종료할 수 있습니다." -ForegroundColor Green

Set-Location "$UI_PATH"
npx.cmd vite dev --port 3000

