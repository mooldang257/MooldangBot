#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 통합 빌드 지휘부 v6.0 (Ultimate Edition)
# 설계 v5.2 기반: 모듈별 독립 빌드 및 지능형 버전 관리
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 변수 로드
[ -f .env ] && source .env

# 1. 인자 분석
TARGET_IDX=$1
IMAGE_VERSION=$2
IS_BOT_ENV=false # 기본은 dev 버전업

# 인자가 없을 경우 메뉴 표시
if [ -z "$TARGET_IDX" ]; then
    echo -e "${YELLOW}🛠️  빌드 대상을 선택해주세요 (설계 v5.2 준수):${NC}"
    echo "1) 프론트엔드(UI) & API (Web App 통합)"
    echo "2) 프론트엔드(UI: Studio, Admin)"
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

# 운영(bot) 버전 여부 확인 (인프라/게이트웨이 제외)
if [[ "$TARGET_IDX" =~ ^[1-6,9]$ ]]; then
    read -p "📢 운영(bot)용 버전으로 빌드하시겠습니까? (y/N): " is_prod
    if [[ "$is_prod" == "y" || "$is_prod" == "Y" ]]; then
        IS_BOT_ENV=true
        echo -e "${RED}⚠️  운영(bot) 버전 규칙(Minor Up)을 적용합니다.${NC}"
    else
        echo -e "${GREEN}🍃 개발(dev) 버전 규칙(Patch Up)을 적용합니다.${NC}"
    fi
fi

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
    4) # API Only
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
        FILES="$COMPOSE_INFRA $COMPOSE_APP $COMPOSE_BOT $COMPOSE_UI $COMPOSE_OVERLAY $COMPOSE_SHARED"
        SERVICES="db redis rabbitmq adminer grafana loki prometheus app migration chzzk-bot studio admin overlay fonts"
        SERVICES_TO_TAG=("app" "chzzk-bot" "studio" "admin" "overlay" "fonts")
        VERSION_KEYS=("VERSION_APP" "VERSION_BOT" "VERSION_UI" "VERSION_OVERLAY" "VERSION_FONTS")
        ;;
    7) # Infra Pull
        echo -e "${YELLOW}📥 인프라 이미지를 가져오는 중...${NC}"
        docker compose $COMPOSE_INFRA pull
        exit 0 ;;
    8) # Gateway Pull
        echo -e "${YELLOW}📥 게이트웨이 이미지를 가져오는 중...${NC}"
        docker compose $COMPOSE_INFRA pull traefik
        exit 0 ;;
    9) # Fonts
        FILES="$COMPOSE_SHARED"
        SERVICES="fonts"
        SERVICES_TO_TAG=("fonts")
        VERSION_KEYS=("VERSION_FONTS")
        ;;
    *) echo -e "${RED}❌ 잘못된 선택입니다.${NC}"; exit 1 ;;
esac

# 3. 버전 자동 생성 (설계 v5.2 준수)
if [ -z "$IMAGE_VERSION" ]; then
    # 첫 번째 대표 키를 기준으로 버전 계산
    V_KEY=${VERSION_KEYS[0]}
    CURRENT_VERSION=$(grep "^${V_KEY}=" .env | cut -d'=' -f2)
    
    if [[ $CURRENT_VERSION =~ ^v([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
        MAJOR=${BASH_REMATCH[1]}
        MINOR=${BASH_REMATCH[2]}
        PATCH=${BASH_REMATCH[3]}
        
        if [ "$IS_BOT_ENV" == "true" ]; then
            NEXT_MINOR=$((MINOR + 1))
            IMAGE_VERSION="v$MAJOR.$NEXT_MINOR.0"
        else
            NEXT_PATCH=$((PATCH + 1))
            IMAGE_VERSION="v$MAJOR.$MINOR.$NEXT_PATCH"
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
