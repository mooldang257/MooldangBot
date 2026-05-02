#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 개발 환경 전용 빌드 스크립트 v2.1
# 설계 v5.2 준수: 모듈화된 레이어별 빌드 및 버전 관리
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
    echo -e "${YELLOW}🚀 빌드 대상을 선택해주세요 (개발 전용):${NC}"
    echo "1) 프론트엔드(UI) & API (Web App 통합)"
    echo "2) 프론트엔드(UI Only)"
    echo "3) 오버레이 (Overlay: 방송 송출)"
    echo "4) 백엔드 (API: API Server)"
    echo "5) 봇 (Bot: Chzzk-Bot)"
    echo "6) 전체 (All Services)"
    echo "7) 인프라 (Infra: DB, Redis, MQ - Pull Only)"
    echo "8) 게이트웨이 (Gateway: Traefik)"
    echo "9) 폰트 (Fonts: Shared Assets)"
    read -p "선택 (번호): " choice
    TARGET_IDX=$choice
fi

# 개발(dev) 버전 규칙(Patch Up) 상시 적용
echo -e "${GREEN}🍃 개발(dev) 버전 규칙(Patch Up)을 적용합니다.${NC}"
IS_BOT_ENV=false

# 2. 빌드 대상 및 컴포즈 파일 매핑
COMPOSE_INFRA="-f docker-compose.infra.yml"
COMPOSE_APP="-f docker-compose.app.yml"
COMPOSE_BOT="-f docker-compose.bot.yml"
COMPOSE_UI="-f docker-compose.ui.yml"
COMPOSE_OVERLAY="-f docker-compose.overlay.yml"
COMPOSE_SHARED="-f docker-compose.shared.yml"

case $TARGET_IDX in
    1) # UI & API
        FILES="$COMPOSE_UI $COMPOSE_APP"
        SERVICES="studio admin app migration"
        SERVICES_TO_TAG=("studio" "admin" "app")
        VERSION_KEYS=("VERSION_UI" "VERSION_APP")
        ;;
    2) # UI Only
        FILES="$COMPOSE_UI"
        SERVICES="studio admin"
        SERVICES_TO_TAG=("studio" "admin")
        VERSION_KEYS=("VERSION_UI")
        ;;
    3) # Overlay
        FILES="$COMPOSE_OVERLAY"
        SERVICES="overlay"
        SERVICES_TO_TAG=("overlay")
        VERSION_KEYS=("VERSION_OVERLAY")
        ;;
    4) # Backend API
        FILES="$COMPOSE_APP"
        SERVICES="app migration"
        SERVICES_TO_TAG=("app")
        VERSION_KEYS=("VERSION_APP")
        ;;
    5) # Bot
        FILES="$COMPOSE_BOT"
        SERVICES="chzzk-bot"
        SERVICES_TO_TAG=("chzzk-bot")
        VERSION_KEYS=("VERSION_BOT")
        ;;
    6) # All
        FILES="$COMPOSE_UI $COMPOSE_APP $COMPOSE_BOT $COMPOSE_OVERLAY $COMPOSE_SHARED"
        SERVICES="studio admin app chzzk-bot overlay fonts"
        SERVICES_TO_TAG=("studio" "admin" "app" "chzzk-bot" "overlay" "fonts")
        VERSION_KEYS=("VERSION_APP" "VERSION_BOT" "VERSION_UI" "VERSION_OVERLAY" "VERSION_FONTS")
        ;;
    7) # Infra
        echo -e "${YELLOW}📦 인프라 이미지를 가져오는 중...${NC}"
        docker compose $COMPOSE_INFRA pull
        echo -e "${GREEN}✅ 인프라 이미지 준비 완료!${NC}"
        exit 0
        ;;
    8) # Gateway
        FILES="-f ../bot/docker-compose.gateway.yml"
        SERVICES="traefik"
        SERVICES_TO_TAG=()
        VERSION_KEYS=()
        ;;
    9) # Fonts
        FILES="$COMPOSE_SHARED"
        SERVICES="fonts"
        SERVICES_TO_TAG=("fonts")
        VERSION_KEYS=("VERSION_FONTS")
        ;;
    *)
        echo -e "${RED}❌ 올바르지 않은 번호입니다.${NC}"
        exit 1
        ;;
esac

# 3. 버전 결정 (v0.x.x 형태)
IMAGE_VERSION="latest"
if [ ${#VERSION_KEYS[@]} -gt 0 ]; then
    # 첫 번째 키를 기준으로 버전 추출
    KEY=${VERSION_KEYS[0]}
    CURRENT_VERSION=$(grep "^$KEY=" .env | cut -d'=' -f2)
    
    if [[ $CURRENT_VERSION =~ ^v([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
        major=${BASH_REMATCH[1]}
        minor=${BASH_REMATCH[2]}
        patch=${BASH_REMATCH[3]}
        
        if [ "$IS_BOT_ENV" = true ]; then
            IMAGE_VERSION="v$major.$((minor + 1)).0"
        else
            IMAGE_VERSION="v$major.$minor.$((patch + 1))"
        fi
    else
        IMAGE_VERSION="v0.0.1"
    fi
    echo -e "${YELLOW}🔔 버전 자동 승격: $CURRENT_VERSION -> $IMAGE_VERSION${NC}"
fi

# 4. 빌드 실행
echo -e "${GREEN}🚀 빌드 시작 ($IMAGE_VERSION)...${NC}"
# 빌드 시에는 latest로 빌드하여 캐시 활용 후 나중에 태깅
VERSION_APP=latest VERSION_BOT=latest VERSION_UI=latest VERSION_OVERLAY=latest VERSION_FONTS=latest \
docker compose $FILES build --no-cache $SERVICES

# 5. 버전 태깅 및 .env 업데이트
if [ "$IMAGE_VERSION" != "latest" ]; then
    echo -e "${YELLOW}🏷️  버전 태깅 및 .env 동기화 중...${NC}"
    for svc in "${SERVICES_TO_TAG[@]}"; do
        IMG_NAME="mooldang-$svc"
        if docker images -q "$IMG_NAME:latest" > /dev/null; then
            docker tag "$IMG_NAME:latest" "$IMG_NAME:$IMAGE_VERSION"
            echo -e "  - $IMG_NAME:$IMAGE_VERSION ${GREEN}완료${NC}"
        fi
    done
    
    # .env 파일 업데이트
    for key in "${VERSION_KEYS[@]}"; do
        sed -i "s/^$key=.*/$key=$IMAGE_VERSION/" .env
        echo -e "  - $key -> ${GREEN}$IMAGE_VERSION${NC}"
    done
fi

echo -e "${GREEN}✅ 모든 빌드 및 태깅 프로세스가 완료되었습니다!${NC}"
