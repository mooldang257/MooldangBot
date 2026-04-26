#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 개발 환경 배포 실행파일 v1.0
# 인프라, 백엔드, 프론트엔드를 선택적으로 개발 환경에 올립니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 1. 대상 선택
TARGET=$1
if [ -z "$TARGET" ]; then
    echo -e "${YELLOW}🚀 개발 환경 배포 대상을 선택해주세요:${NC}"
    echo "1) 전체 배포 (All)"
    echo "2) 인프라 (Infra: DB, Redis, MQ)"
    echo "3) 백엔드 (Backend: API, Bot)"
    echo "4) 프론트엔드 (Frontend: Studio, Admin, Overlay)"
    read -p "선택 (번호): " choice
    case $choice in
        1) TARGET="all" ;;
        2) TARGET="infra" ;;
        3) TARGET="backend" ;;
        4) TARGET="frontend" ;;
        *) echo "취소되었습니다."; exit 0 ;;
    esac
fi

# 2. 컴포즈 파일 정의
INFRA="-f docker-compose.infra.yml"
BACKEND="-f docker-compose.backend.yml"
FRONTEND="-f docker-compose.frontend.yml"

deploy_infra() {
    echo -e "${YELLOW}🏗️  [Dev] 인프라 가동 중...${NC}"
    docker network create mooldang_dev_net 2>/dev/null
    docker compose $INFRA up -d
}

deploy_backend() {
    echo -e "${YELLOW}⚙️  [Dev] 백엔드 가동 중...${NC}"
    docker compose $BACKEND up -d
}

deploy_frontend() {
    echo -e "${YELLOW}🎨 [Dev] 프론트엔드 가동 중...${NC}"
    docker compose $FRONTEND up -d
}

# 3. 실행
case $TARGET in
    all)
        deploy_infra
        deploy_backend
        deploy_frontend
        ;;
    infra)
        deploy_infra
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

echo -e "${GREEN}✅ 개발 환경 배포가 완료되었습니다!${NC}"
docker ps --filter "name=mooldang-dev"
