# [오시리스의 도구함]: MooldangBot 통합 마이그레이션 & 번들 빌드 스크립트 v1.0
# 사용법: .\bundle-all.ps1 [-MigrationName "마이그레이션이름"]

param (
    [string]$MigrationName = ""
)

$ErrorActionPreference = "Stop"

Write-Host "`n🌊 [물멍의 지휘]: 통합 빌드 프로세스를 시작합니다..." -ForegroundColor Cyan

try {
    # 1. 마이그레이션 이름 설정 (자동 명명 정책 적용)
    if ([string]::IsNullOrEmpty($MigrationName)) {
        $Timestamp = Get-Date -Format "yyyyMMdd_HHmm"
        $MigrationName = "AutoMig_v6_2_$Timestamp"
        Write-Host "🔖 [파로스의 각인]: 이름이 지정되지 않아 자동 명패를 부여합니다 -> $MigrationName" -ForegroundColor Yellow
    }

    # 2. 마이그레이션 생성
    Write-Host "✍️ [기록관의 붓]: 새 마이그레이션을 생성 중입니다..." -ForegroundColor Cyan
    dotnet ef migrations add $MigrationName -p MooldangBot.Infrastructure -s MooldangBot.Api
    
    # 3. 런타임 종속성 복원
    Write-Host "📦 [보급로 확보]: 런타임별 종속성을 복원합니다 (linux-x64, win-x64)..." -ForegroundColor Cyan
    dotnet restore MooldangAPI.sln -r linux-x64
    dotnet restore MooldangAPI.sln -r win-x64

    # 4. 서버용 번들 빌드 (Linux)
    Write-Host "🐧 [서버의 전령]: Linux용 efbundle_linux를 빌드합니다..." -ForegroundColor Cyan
    dotnet ef migrations bundle --runtime linux-x64 -p MooldangBot.Infrastructure -s MooldangBot.Api --self-contained -o efbundle_linux --force

    # 5. 로컬용 번들 빌드 (Windows)
    Write-Host "🪟 [로컬의 검]: Windows용 efbundle_win.exe를 빌드합니다..." -ForegroundColor Cyan
    dotnet ef migrations bundle --runtime win-x64 -p MooldangBot.Infrastructure -s MooldangBot.Api --self-contained -o efbundle_win.exe --force

    Write-Host "`n✅ [물멍의 전언]: 모든 증표가 완성되었습니다!" -ForegroundColor Green
    Write-Host "📍 생성된 파일: efbundle_linux (서버용), efbundle_win.exe (로컬용)" -ForegroundColor White
}
catch {
    Write-Host "`n❌ [오시리스의 거절]: 빌드 프로세스 중 예상치 못한 오류가 발생했습니다." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
    exit 1
}
