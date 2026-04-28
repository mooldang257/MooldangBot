#!/bin/bash

# ---------------------------------------------------------
# 🔱 [MooldangBot] 운영 환경 배포 실행파일 v1.0
# 인프라, 게이트웨이, 백엔드, 프론트엔드를 개별 배포합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 1. 대상 선택
TARGET=$1
if [ -z "$TARGET" ]; then
    echo -e "${GREEN}⚓ 운영 환경 배포 지휘부에 접속했습니다.${NC}"
    echo -e "${YELLOW}🎯 배포 대상을 선택해주세요:${NC}"
    echo "1) 전체 배포 (All: Infra -> Gateway -> Backend -> Frontend)"
    echo "2) 인프라 (Infra: DB, Redis, MQ)"
    echo "3) 게이트웨이 (Gateway: Traefik, Tunnel)"
    echo "4) 백엔드 (Backend: API, Bot)"
    echo "5) 프론트엔드 (Frontend: Studio, Admin, Overlay)"
    read -p "선택 (번호): " choice
    case $choice in
        1) TARGET="all" ;;
        2) TARGET="infra" ;;
        3) TARGET="gateway" ;;
        4) TARGET="backend" ;;
        5) TARGET="frontend" ;;
        *) echo "취소되었습니다."; exit 0 ;;
    esac
fi

# 2. 컴포즈 파일 매핑
INFRA="-f docker-compose.infra.yml"
GATEWAY="-f docker-compose.gateway.yml"
BACKEND="-f docker-compose.app.yml"
FRONTEND="-f docker-compose.ui.yml"

# 환경 체크
[ ! -f .env ] && { echo -e "${RED}❌ .env 파일이 없습니다.${NC}"; exit 1; }
NETWORK_NAME="mooldang_prod_net"
docker network ls | grep -q "$NETWORK_NAME" || docker network create "$NETWORK_NAME"

deploy_infra() {
    echo -e "${YELLOW}🏗️  [Prod] 인프라 가동 중...${NC}"
    docker compose $INFRA up -d
}

deploy_gateway() {
    echo -e "${YELLOW}🛡️  [Prod] 게이트웨이 가동 중...${NC}"
    docker compose $GATEWAY up -d
}

deploy_backend() {
    echo -e "${YELLOW}⚙️  [Prod] 백엔드 가동 중...${NC}"
    docker compose $BACKEND up -d
}

deploy_frontend() {
    echo -e "${YELLOW}🎨 [Prod] 프론트엔드 가동 중...${NC}"
    docker compose $FRONTEND up -d
}

# 3. 실행
case $TARGET in
    all)
        deploy_infra
        deploy_gateway
        deploy_backend
        deploy_frontend
        ;;
    infra)
        deploy_infra
        ;;
    gateway)
        deploy_gateway
        ;;
    backend)
        deploy_backend
        ;;
    frontend)
        deploy_frontend
        ;;
    *)
        echo -e "${RED}❌ 잘못된 대상입니다.${NC}"
        exit 1
        ;;
esac

# 4. 정리 (7일 이상 된 미사용 이미지)
docker image prune -a --filter "until=168h" -f

echo -e "${GREEN}✅ 운영 환경 배포가 완료되었습니다!${NC}"
docker ps --filter "name=mooldang-prod"
