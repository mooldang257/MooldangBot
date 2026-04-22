# [Console]::OutputEncoding 설정으로 한글 깨짐 방지
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# [오시리스의 항해]: MooldangBot 로컬 기동 스크립트 (전체 서비스 모드)
# 모니터링 스택을 포함하여 정의된 모든 서비스를 기동합니다.

Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "       🌊 MooldangBot Local - Full Stack 🌊       " -ForegroundColor Cyan
Write-Host "--------------------------------------------------" -ForegroundColor Cyan

# 1. 환경 파일 체크
if (-not (Test-Path ".env")) {
    Write-Host "[!] .env 파일이 존재하지 않습니다. 설정을 확인해 주세요." -ForegroundColor Red
    exit 1
}

# 2. 전체 이미지 체크 및 풀링
Write-Host "[*] 전체 시스템 이미지 체크 및 업데이트 중..." -ForegroundColor Gray
docker-compose pull

# 3. 전체 시스템 기동 (인프라 및 모니터링 포함)
Write-Host "[*] 전체 서비스 기동 중 (모든 시스템 활성화)..." -ForegroundColor Yellow
docker-compose up -d

# 4. 애플리케이션 서비스 핵심 브릿지 재빌드 (코드 최신화 보장)
# 코드가 빈번하게 바뀌는 핵심 로직들은 명시적으로 다시 빌드하여 올립니다.
$CoreServices = @("chzzk-bot", "app", "studio", "admin", "overlay", "migration")
Write-Host "[*] 핵심 애플리케이션 서비스 재빌드 및 초기화 중..." -ForegroundColor Yellow
docker-compose rm -f -s $CoreServices
docker-compose up -d --build $CoreServices

if ($LASTEXITCODE -ne 0) {
    Write-Host "[!] 시스템 기동 중 오류가 발생했습니다." -ForegroundColor Red
    exit 1
}

Write-Host "--------------------------------------------------" -ForegroundColor Green
Write-Host "[V] 전체 시스템이 성공적으로 기동되었습니다." -ForegroundColor Green
Write-Host "[V] 모든 모니터링 및 개발 도구를 사용하실 수 있습니다." -ForegroundColor Green
Write-Host "--------------------------------------------------" -ForegroundColor Green

# 5. 앱 로그 추적 시작
Write-Host "[*] 메인 API(mooldang-app) 로그를 추적합니다. (Ctrl+C로 중단)" -ForegroundColor Cyan
docker-compose logs -f app
