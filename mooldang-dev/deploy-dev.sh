#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 개발 환경 배포 실행파일 v2.0
# 설계 v5.2 준수: 모듈화된 레이어별 정밀 배포 수행
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 변수 로드
[ -f .env ] && source .env

# 1. 대상 선택
TARGET_IDX=$1
if [ -z "$TARGET_IDX" ]; then
    echo -e "${YELLOW}🚀 개발 환경 배포 대상을 선택해주세요 (설계 v5.2 준수):${NC}"
    echo "1) 프론트엔드(UI) & API (Web App 통합)"
    echo "2) 프론트엔드(UI: Studio, Admin)"
    echo "3) 오버레이 (Overlay: 방송 송출)"
    echo "4) 백엔드 (API: API Server)"
    echo "5) 봇 (Bot: Chzzk-Bot)"
    echo "6) 전체 배포 (All Services)"
    echo "7) 인프라 (Infra: DB, Redis, MQ)"
    echo "8) 게이트웨이 (Gateway: 운영 Traefik 연동 확인)"
    echo "9) 폰트 (Fonts: Shared Assets)"
    read -p "선택 (번호): " choice
    TARGET_IDX=$choice
fi

# 2. 컴포즈 파일 매핑
INFRA="-f docker-compose.infra.yml"
APP="-f docker-compose.app.yml"
BOT="-f docker-compose.bot.yml"
UI="-f docker-compose.ui.yml"
OVERLAY="-f docker-compose.overlay.yml"
SHARED="-f docker-compose.shared.yml"

# 네트워크 보장 함수
ensure_network() {
    if ! docker network ls | grep -q "mooldang_dev_net"; then
        echo -e "${YELLOW}🌐 개발 네트워크(mooldang_dev_net) 생성 중...${NC}"
        docker network create mooldang_dev_net
    fi
}

deploy_target() {
    local name=$1
    local files=$2
    local services=$3
    echo -e "${YELLOW}🏗️  [Dev] $name 가동 중...${NC}"
    ensure_network
    docker compose $files up -d $services
}

# 3. 실행 프로세스
case $TARGET_IDX in
    1) # UI & API
        deploy_target "UI & API" "$UI $APP" "" ;;
    2) # UI Only
        deploy_target "Frontend UI" "$UI" "" ;;
    3) # Overlay
        deploy_target "Overlay" "$OVERLAY" "" ;;
    4) # API Only
        deploy_target "Backend API" "$APP" "" ;;
    5) # Bot
        deploy_target "Chzzk Bot" "$BOT" "" ;;
    6) # All
        echo -e "${YELLOW}🌟 [Dev] 전체 시스템 가동 중...${NC}"
        ensure_network
        docker compose $INFRA $APP $BOT $UI $OVERLAY $SHARED up -d
        ;;
    7) # Infra
        deploy_target "Infrastructure" "$INFRA" "" ;;
    8) # Gateway
        echo -e "${GREEN}ℹ️  개발 환경은 운영 게이트웨이(bot.mooldang.com)를 공유합니다.${NC}"
        echo -e "   - dev.mooldang.com 트래픽이 정상적으로 유입되는지 확인하십시오."
        ;;
    9) # Fonts
        deploy_target "Shared Fonts" "$SHARED" "" ;;
    *)
        echo -e "${RED}❌ 잘못된 선택입니다.${NC}"
        exit 1
        ;;
esac

echo -e "${GREEN}✅ 개발 환경 배포 프로세스가 완료되었습니다!${NC}"
echo -e "${YELLOW}🔍 가동 중인 컨테이너 상태:${NC}"
docker ps --filter "name=mooldang" --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"
