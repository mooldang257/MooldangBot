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
    echo -e "${YELLOW}ℹ️ 옵션이 지정되지 않아 전체 서비스를 배포합니다.${NC}"
    DEPLOY_APP=true
    DEPLOY_UI=true
    DEPLOY_DB=true
fi

# 배포 대상 서비스 리스트 구성
[ "$DEPLOY_DB" = true ] && SERVICES="$SERVICES db migration"
[ "$DEPLOY_APP" = true ] && SERVICES="$SERVICES app"
[ "$DEPLOY_UI" = true ] && SERVICES="$SERVICES studio"
SERVICES="$SERVICES nginx redis rabbitmq" # 핵심 인프라는 항상 상태 유지 확인

# 4. 사전 빌드 검사 (App 포함 시)
if [ "$DEPLOY_APP" = true ] && command -v dotnet &> /dev/null; then
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

echo -e "${GREEN}🐳 Docker: 선택된 서비스 [$SERVICES ] 빌드 및 실행 중...${NC}"
docker compose up -d --build $BUILD_OPTS $SERVICES

# 6. 상태 모니터링 및 로그 제안
echo -e "${GREEN}🔍 Health Check: 서비스 가동 상태 확인...${NC}"
sleep 3
docker compose ps

echo -e "${GREEN}✅ 배포가 성공적으로 완료되었습니다!${NC}"
if [ "$DEPLOY_UI" = true ] && [ "$DEPLOY_APP" = false ]; then
    echo "UI 로그 확인: docker-compose logs -f studio"
else
    echo "백엔드 로그 확인: docker-compose logs -f app"
fi
