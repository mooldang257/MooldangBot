#!/bin/bash

# ---------------------------------------------------------
# 🔱 [MooldangBot] 운영(Bot) 환경 버전 승격 및 배포 지휘부 v3.0
# 설계 v5.2 준수: 개발(dev) 이미지를 운영(bot) 버전으로 승격 배포
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 체크
[ ! -f .env ] && { echo -e "${RED}❌ 운영(bot) .env 파일이 없습니다.${NC}"; exit 1; }
[ ! -f ../mooldang-dev/.env ] && { echo -e "${RED}❌ 개발(dev) .env 파일을 찾을 수 없습니다.${NC}"; exit 1; }

# 버전 승격 함수
promote_version() {
    local current_version=$1
    if [[ $current_version =~ v([0-9]+)\.([0-9]+)\.([0-9]+) ]]; then
        local major=${BASH_REMATCH[1]}
        local minor=${BASH_REMATCH[2]}
        local new_minor=$((minor + 1))
        echo "v${major}.${new_minor}.0"
    else
        echo "$current_version"
    fi
}

# 1. 버전 정보 로드
DEV_VER_APP=$(grep VERSION_APP ../mooldang-dev/.env | cut -d'=' -f2)
BOT_VER_CUR=$(grep VERSION_APP .env | cut -d'=' -f2)
BOT_VER_NEW=$(promote_version $BOT_VER_CUR)

echo -e "${GREEN}⚓ 운영(bot) 버전 승격 시스템 가동${NC}"
echo -e "  - 개발 서버 검증 버전: ${CYAN}${DEV_VER_APP}${NC}"
echo -e "  - 현재 운영 서버 버전: ${YELLOW}${BOT_VER_CUR}${NC}"
echo -e "  - 승격 예정 운영 버전: ${GREEN}${BOT_VER_NEW}${NC}"
echo -e "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# 2. 이미지 재태깅 (Re-tagging)
tag_image() {
    local name=$1
    local dev_ver=$2
    local bot_ver=$3
    
    echo -ne "  🏷️  $name 승격 중... "
    docker tag "mooldang-$name:$dev_ver" "mooldang-$name:$bot_ver" 2>/dev/null
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}완료${NC}"
    else
        echo -e "${RED}실패 (이미지 없음: mooldang-$name:$dev_ver)${NC}"
    fi
}

# 배포 전 승격 확인
read -p "🚀 운영(bot) 버전으로 승격하시겠습니까? (y/N): " confirm
if [[ "$confirm" == "y" || "$confirm" == "Y" ]]; then
    # 모든 이미지 승격 실행
    tag_image "app" "$DEV_VER_APP" "$BOT_VER_NEW"
    tag_image "chzzk-bot" "$DEV_VER_APP" "$BOT_VER_NEW" # Bot은 App과 버전 공유
    tag_image "studio" "$(grep VERSION_UI ../mooldang-dev/.env | cut -d'=' -f2)" "$BOT_VER_NEW"
    tag_image "admin" "$(grep VERSION_UI ../mooldang-dev/.env | cut -d'=' -f2)" "$BOT_VER_NEW"
    tag_image "overlay" "$(grep VERSION_OVERLAY ../mooldang-dev/.env | cut -d'=' -f2)" "$BOT_VER_NEW"
    tag_image "fonts" "$(grep VERSION_FONTS ../mooldang-dev/.env | cut -d'=' -f2)" "$BOT_VER_NEW"

    # .env 업데이트
    sed -i "s/VERSION_APP=.*/VERSION_APP=$BOT_VER_NEW/" .env
    sed -i "s/VERSION_BOT=.*/VERSION_BOT=$BOT_VER_NEW/" .env
    sed -i "s/VERSION_UI=.*/VERSION_UI=$BOT_VER_NEW/" .env
    sed -i "s/VERSION_OVERLAY=.*/VERSION_OVERLAY=$BOT_VER_NEW/" .env
    sed -i "s/VERSION_FONTS=.*/VERSION_FONTS=$BOT_VER_NEW/" .env
    echo -e "${GREEN}✅ .env 파일이 $BOT_VER_NEW 버전으로 업데이트되었습니다.${NC}"
else
    echo -e "${YELLOW}ℹ️  승격을 건너뛰고 현재 버전($BOT_VER_CUR)으로 배포를 진행합니다.${NC}"
fi

# 3. 배포 실행
# 네트워크 체크 및 생성
NETWORK_NAME="mooldang_prod_net"
docker network ls | grep -q "$NETWORK_NAME" || docker network create "$NETWORK_NAME"

# 컴포즈 파일 정의
INFRA="-f docker-compose.infra.yml"
GATEWAY="-f docker-compose.gateway.yml"
APP="-f docker-compose.app.yml"
BOT="-f docker-compose.bot.yml"
UI="-f docker-compose.ui.yml"
OVERLAY="-f docker-compose.overlay.yml"
SHARED="-f docker-compose.shared.yml"

echo -e "${YELLOW}🎯 배포 대상을 선택해주세요:${NC}"
echo "1) [FE+API] 서비스 전역 배포 (UI + Overlay + API)"
echo "2) [UI] 스튜디오 및 어드민 배포"
echo "3) [Overlay] 방송용 오버레이 배포"
echo "4) [API] 백엔드 서버 및 마이그레이션"
echo "5) [Bot] 치지직 봇 독립 배포"
echo "6) [All] 전체 시스템 풀 배포 (인프라 포함)"
echo "7) [Infra] 인프라 레이어 (DB, Redis, MQ, Monitoring)"
echo "8) [Gateway] 게이트웨이 레이어 (Traefik, Tunnel)"
echo "9) [Fonts] 공용 폰트 서버 배포"
read -p "선택 (1-9): " choice

case $choice in
    1) docker compose $APP $UI $OVERLAY up -d ;;
    2) docker compose $UI up -d ;;
    3) docker compose $OVERLAY up -d ;;
    4) docker compose $APP up -d ;;
    5) docker compose $BOT up -d ;;
    6) docker compose $INFRA $GATEWAY $APP $BOT $UI $OVERLAY up -d ;;
    7) docker compose $INFRA up -d ;;
    8) docker compose $GATEWAY up -d ;;
    9) docker compose $SHARED up -d ;;
    *) echo "잘못된 선택입니다."; exit 1 ;;
esac

# 정리
docker image prune -f
echo -e "${GREEN}✅ 운영(Bot) 환경 배포 완료!${NC}"
./version.sh
