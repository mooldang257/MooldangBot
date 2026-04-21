#!/bin/bash

# ---------------------------------------------------------
# 🌊 [오시리스의 함대] 물댕봇 무중단(Zero-Downtime) 배포 스크립트 v2.5
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 물댕봇 무중단 배포 시스템을 가동합니다...${NC}"

# 1. 환경 변수 체크
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다. .env.sample을 복사하여 생성해주세요.${NC}"
    exit 1
fi

# 2. 소스 코드 동기화
echo -e "${GREEN}📥 Git: 파로스의 새로운 진동(최신 코드)을 동기화 중...${NC}"
# git fetch --all
# git reset --hard origin/main # 충돌 방지를 위한 강제 동기화 권장

# 3. 파라미터 분석
DEPLOY_APP=false
DEPLOY_UI=false
DEPLOY_DB=false
DEPLOY_MONITOR=false
CLEAN_BUILD=false
SERVICES=""

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --app) DEPLOY_APP=true ;;
        --ui) DEPLOY_UI=true ;;
        --db) DEPLOY_DB=true; DEPLOY_APP=true ;; # DB 변경 시 App 배포 필수
        --monitor) DEPLOY_MONITOR=true ;;
        --clean) CLEAN_BUILD=true ;;
        *) echo "알 수 없는 옵션: $1"; exit 1 ;;
    esac
    shift
done

# 배포 대상 서비스 리스트 구성
if [ "$DEPLOY_APP" = false ] && [ "$DEPLOY_UI" = false ] && [ "$DEPLOY_DB" = false ] && [ "$DEPLOY_MONITOR" = false ]; then
    echo -e "${YELLOW}ℹ️ 옵션이 지정되지 않아 전체 함대를 배포합니다.${NC}"
    SERVICES=""
else
    [ "$DEPLOY_DB" = true ] && SERVICES="$SERVICES db migration"
    [ "$DEPLOY_APP" = true ] && SERVICES="$SERVICES app chzzk-bot"
    [ "$DEPLOY_UI" = true ] && SERVICES="$SERVICES studio admin overlay"
    [ "$DEPLOY_MONITOR" = true ] && SERVICES="$SERVICES grafana loki prometheus"
    SERVICES="$SERVICES nginx redis rabbitmq" # 핵심 인프라는 상태 유지 명시
fi

# 4. [중요] 사전 검증 (Shift-Left Verification)
if [[ "$SERVICES" == *"app"* || -z "$SERVICES" ]]; then
    echo -e "${YELLOW}⚖️ Verifier: 배포 전 오시리스의 저울로 Contract 정합성을 검증합니다...${NC}"
    
    # Verifier 실행을 위해 코드만 먼저 빌드 (컨테이너 교체는 아직 안 함)
    docker compose build chzzk-bot
    
    mkdir -p ./data/app/reports
    # --no-deps 옵션으로 다른 서비스 건드리지 않고 격리된 상태에서 검증 수행
    docker compose run --rm --no-deps --entrypoint "dotnet" -v ./data/app/reports:/app/verifier/reports chzzk-bot verifier/MooldangBot.Verifier.dll
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ 치명적 오류: Contract 정합성 검증 실패! 파로스의 진동이 왜곡되어 배포를 전면 취소합니다.${NC}"
        echo -e "${YELLOW}💡 ./data/app/reports 의 로그를 확인하여 코드를 수정하세요.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ 정합성 검증 통과. 진동이 완벽히 정렬되었습니다.${NC}"
fi

# 5. 컨테이너 교체 및 무중단 적용
BUILD_OPTS=""
[ "$CLEAN_BUILD" = true ] && BUILD_OPTS="--no-cache"

echo -e "${GREEN}🐳 Docker: 함대 기동 중 [$SERVICES]...${NC}"
docker compose up -d --build $BUILD_OPTS $SERVICES

# [핵심] Nginx 무중단 리로드 (Hard Restart 방지)
if docker ps | grep -q "mooldang-nginx"; then
    echo -e "${GREEN}🔄 Nginx: 10k TPS 유실 없이 업스트림 동기화(Graceful Reload) 중...${NC}"
    docker exec mooldang-nginx nginx -s reload
else
    echo -e "${YELLOW}⚠️ Nginx 컨테이너가 없어 새로 시작합니다...${NC}"
    docker compose start nginx
fi

# 6. 시스템 최적화 및 마무리
echo -e "${GREEN}🧹 System: 불필요한 이미지 잔해(Dangling)를 청소하여 NVMe 수명을 보호합니다...${NC}"
docker image prune -f

echo -e "${GREEN}✅ 배포가 성공적으로 완료되었습니다! (무중단 적용 완료)${NC}"

if [ "$DEPLOY_UI" = true ] && [ "$DEPLOY_APP" = false ]; then
    echo "UI 로그 확인: docker compose logs -f studio"
else
    echo "백엔드 로그 확인: docker compose logs -f chzzk-bot app"
fi

