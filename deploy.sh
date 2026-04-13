#!/bin/bash

# ---------------------------------------------------------
# 🌊 [물댕봇] 서버 지능형 선택 배포 스크립트 (deploy.sh)
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 물댕봇 지능형 배포 시스템을 가동합니다...${NC}"

# 1. 환경 변수 체크
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다. .env.sample을 복사하여 먼저 생성해주세요.${NC}"
    exit 1
fi

# 2. 소스 코드 동기화
echo -e "${GREEN}📥 Git: 최신 소스 코드를 동기화 중...${NC}"
git pull

# 3. 파라미터 분석
DEPLOY_APP=false
DEPLOY_UI=false
DEPLOY_DB=false
CLEAN_BUILD=false
SERVICES=""

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --app) DEPLOY_APP=true ;;
        --ui) DEPLOY_UI=true ;;
        --db) DEPLOY_DB=true; DEPLOY_APP=true ;; # [ER 종속성]: DB 변경 시 App 배포 필수
        --clean) CLEAN_BUILD=true ;;
        *) echo "알 수 없는 옵션: $1"; exit 1 ;;
    esac
    shift
done

# 아무 옵션도 없을 경우 전체 배포
if [ "$DEPLOY_APP" = false ] && [ "$DEPLOY_UI" = false ] && [ "$DEPLOY_DB" = false ]; then
    echo -e "${YELLOW}ℹ️ 옵션이 지정되지 않아 전체 함대 서비스를 배포합니다.${NC}"
    SERVICES=""
else
    # 배포 대상 서비스 리스트 구성
    [ "$DEPLOY_DB" = true ] && SERVICES="$SERVICES db migration"
    [ "$DEPLOY_APP" = true ] && SERVICES="$SERVICES app chzzk-bot"
    [ "$DEPLOY_UI" = true ] && SERVICES="$SERVICES studio admin overlay"
    SERVICES="$SERVICES nginx redis rabbitmq" # 핵심 인프라는 항상 상태 유지 확인
fi

# 4. 사전 빌드 검사 (App 포함 시)
if [[ "$SERVICES" == *"app"* || -z "$SERVICES" ]] && command -v dotnet &> /dev/null; then
    echo -e "${GREEN}🛠️ Build Check: 백엔드 컴파일 오류를 검사합니다...${NC}"
    dotnet build -c Release
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ 빌드 오류 발생으로 배포를 중단합니다.${NC}"
        exit 1
    fi
fi

# 5. 컨테이너 재가동 및 최적화 빌드
BUILD_OPTS=""
[ "$CLEAN_BUILD" = true ] && BUILD_OPTS="--no-cache"

echo -e "${GREEN}🐳 Docker: 함대 기동 중 [$SERVICES]...${NC}"
docker compose up -d --build $BUILD_OPTS $SERVICES

# [v2.4.11] Nginx 업스트림 정렬 강제 수행
echo -e "${GREEN}🔄 Nginx: 업스트림 동기화 중...${NC}"
docker compose restart nginx

# 6. 상태 모니터링 및 로그 제안
echo -e "${GREEN}🔍 Health Check: 서비스 가동 상태 확인...${NC}"
sleep 3
docker compose ps

# [v3.0.0] EDMH Phase 0: 계약 정합성 자가진단 수행
echo -e "${GREEN}⚖️ Verifier: 함대 혈관(Contracts) 정합성 검증 중...${NC}"
mkdir -p ./data/app/reports
docker compose run --rm --entrypoint "dotnet" -v ./data/app/reports:/app/verifier/reports chzzk-bot verifier/MooldangBot.Verifier.dll

echo -e "${GREEN}✅ 배포 및 자가진단이 성공적으로 완료되었습니다!${NC}"
if [ "$DEPLOY_UI" = true ] && [ "$DEPLOY_APP" = false ]; then
    echo "UI 로그 확인: docker compose logs -f studio"
else
    echo "백엔드 로그 확인: docker compose logs -f app"
fi
