#!/bin/bash

이것은 더이상 사용하지 않는다.

# ---------------------------------------------------------
# 🔱 [MooldangBot] 운영 환경 통합 배포 스크립트 v5.0
# .env의 버전 정보를 기반으로 함대를 기동하고 시스템을 최적화합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}⚓ 물댕 함대 운영 제어 시스템에 접속했습니다.${NC}"

# 1. 환경 설정 로드
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다.${NC}"
    exit 1
fi

# [v5.2] 외부 네트워크 체크 및 생성
NETWORK_NAME="mooldang_prod_net"
if ! docker network ls | grep -q "$NETWORK_NAME"; then
    echo -e "${YELLOW}🌐 외부 네트워크($NETWORK_NAME)가 없습니다. 생성을 시도합니다...${NC}"
    docker network create "$NETWORK_NAME"
fi


VERSION_APP=$(grep "^VERSION_APP=" .env | cut -d'=' -f2)
VERSION_UI=$(grep "^VERSION_UI=" .env | cut -d'=' -f2)

echo -e "${YELLOW}📍 배포 대상 버전: API($VERSION_APP) / UI($VERSION_UI)${NC}"

# 2. 배포 대상 결정
TARGET=$1
if [ -z "$TARGET" ]; then
    echo -e "${YELLOW}🎯 배포 대상을 선택해주세요:${NC}"
    echo "1) 전체 배포 (All)"
    echo "2) 백엔드만 (App: API, Bot)"
    echo "3) 프론트엔드만 (UI: Studio, Admin, Overlay)"
    echo "4) 인프라만 (Infra: MariaDB, Redis, RabbitMQ)"
    read -p "선택 (번호): " choice
    case $choice in
        1) TARGET="all" ;;
        2) TARGET="app" ;;
        3) TARGET="ui" ;;
        4) TARGET="infra" ;;
        *) echo "취소되었습니다."; exit 0 ;;
    esac
fi

# 3. 컴포즈 파일 매핑
INFRA_FILE="docker-compose.infra.yml"
APP_FILE="docker-compose.app.yml"
UI_FILE="docker-compose.ui.yml"
GW_FILE="docker-compose.gateway.yml"

deploy_infra() {
    echo -e "${YELLOW}🏗️  인프라 계층(DB/Cache) 가동 중...${NC}"
    docker compose -f $INFRA_FILE up -d
}

deploy_app() {
    echo -e "${YELLOW}⚙️  백엔드 계층(API/Bot) v$VERSION_APP 가동 중...${NC}"
    docker compose -f $APP_FILE up -d
}

deploy_ui() {
    echo -e "${YELLOW}🎨 프론트엔드 계층(Studio/Admin) v$VERSION_UI 가동 중...${NC}"
    docker compose -f $UI_FILE up -d
}

deploy_gateway() {
    echo -e "${YELLOW}🛡️  게이트웨이(Traefik/Tunnel) 가동 중...${NC}"
    docker compose -f $GW_FILE up -d
}

# 4. 순차적 배포 실행 (의존성 고려)
case $TARGET in
    all)
        deploy_infra
        deploy_app
        deploy_ui
        deploy_gateway
        ;;
    infra)
        deploy_infra
        ;;
    app)
        deploy_app
        ;;
    ui)
        deploy_ui
        ;;
    gw)
        deploy_gateway
        ;;
    *)
        echo -e "${RED}❌ 올바르지 않은 대상입니다: $TARGET${NC}"
        exit 1
        ;;
esac

# 5. 시스템 최적화 (이미지 자동 정리)
echo -e "\n${YELLOW}🧹 시스템 최적화: 사용하지 않는 이미지 정리 중...${NC}"
# 최근 7일(168h) 동안 사용되지 않은 미사용 이미지 정리
docker image prune -a --filter "until=168h" -f

# 6. 마무리 및 이력 기록
HISTORY_FILE="release_history.log"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] DEPLOYED: Target=$TARGET, APP=$VERSION_APP, UI=$VERSION_UI" >> "$HISTORY_FILE"

echo -e "\n${GREEN}✅ 모든 배포 절차가 완료되었습니다!${NC}"
docker compose -f $APP_FILE -f $UI_FILE ps
