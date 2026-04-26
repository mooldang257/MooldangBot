#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 통합 빌드 지휘부 v5.5
# 인프라, 백엔드, 프론트엔드 이미지를 선택적으로 빌드합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 변수 로드
[ -f .env ] && source .env

# 1. 인자 분석
TARGET=$1
IMAGE_VERSION=$2
CLEAN_BUILD=false

# 인자가 없을 경우 메뉴 표시
if [ -z "$TARGET" ]; then
    echo -e "${YELLOW}🛠️  빌드 대상을 선택해주세요:${NC}"
    echo "1) 전체 빌드 (All)"
    echo "2) 백엔드 (Backend: API, Bot)"
    echo "3) 프론트엔드 (Frontend: Studio, Admin, Overlay)"
    echo "4) 인프라 (Infra: Images Pull Only)"
    read -p "선택 (번호): " choice
    case $choice in
        1) TARGET="all" ;;
        2) TARGET="backend" ;;
        3) TARGET="frontend" ;;
        4) TARGET="infra" ;;
        *) echo "취소되었습니다."; exit 0 ;;
    esac
fi

# 버전 자동 생성 (인자가 없을 때)
if [ -z "$IMAGE_VERSION" ] && [ "$TARGET" != "infra" ]; then
    IMAGE_VERSION="v$(date +%Y%m%d-%H%M)"
    echo -e "${YELLOW}🔔 버전이 지정되지 않아 자동으로 생성합니다: $IMAGE_VERSION${NC}"
fi

# 2. 빌드 대상별 컴포즈 파일 및 서비스 설정
COMPOSE_INFRA="-f docker-compose.infra.yml"
COMPOSE_BACKEND="-f docker-compose.backend.yml"
COMPOSE_FRONTEND="-f docker-compose.frontend.yml"

case $TARGET in
    all)
        FILES="$COMPOSE_INFRA $COMPOSE_BACKEND $COMPOSE_FRONTEND"
        SERVICES=""
        SERVICES_TO_TAG=("app" "chzzk-bot" "studio" "overlay" "admin")
        ;;
    backend)
        FILES="$COMPOSE_BACKEND"
        SERVICES="app chzzk-bot"
        SERVICES_TO_TAG=("app" "chzzk-bot")
        ;;
    frontend)
        FILES="$COMPOSE_FRONTEND"
        SERVICES="studio admin overlay"
        SERVICES_TO_TAG=("studio" "admin" "overlay")
        ;;
    infra)
        echo -e "${YELLOW}📥 인프라 이미지(MariaDB, Redis 등)를 가져오는 중...${NC}"
        docker compose $COMPOSE_INFRA pull
        echo -e "${GREEN}✅ 인프라 이미지 준비 완료!${NC}"
        exit 0
        ;;
    *)
        echo -e "${RED}❌ 잘못된 대상입니다.${NC}"
        exit 1
        ;;
esac

# 3. 빌드 실행
echo -e "${GREEN}🚀 빌드 시작 ($TARGET, Version: $IMAGE_VERSION)...${NC}"
# [v5.6] 빌드 시에는 강제로 latest 태그를 사용하도록 환경 변수 주입
VERSION_APP=latest VERSION_UI=latest docker compose $FILES --env-file .env build $SERVICES

# 4. 버전 태깅
if [ "$IMAGE_VERSION" != "latest" ]; then
    echo -e "${YELLOW}🏷️  버전 태깅 중: latest -> $IMAGE_VERSION...${NC}"
    for svc in "${SERVICES_TO_TAG[@]}"; do
        IMG_NAME="mooldang-$svc"
        if docker images -q "$IMG_NAME:latest" > /dev/null; then
            docker tag "$IMG_NAME:latest" "$IMG_NAME:$IMAGE_VERSION"
            echo -e "  - $IMG_NAME:$IMAGE_VERSION ${GREEN}완료${NC}"
        fi
    done
fi

echo -e "${GREEN}✅ 빌드가 완료되었습니다!${NC}"
