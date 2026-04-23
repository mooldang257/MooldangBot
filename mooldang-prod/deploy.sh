#!/bin/bash

# ---------------------------------------------------------
# 🔱 [MooldangBot] 운영 환경 지능형 선택 기동 스크립트 v1.0
# images/ 폴더의 버전 목록을 보여주고 선택하여 배포합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}⚓ 물댕 함대 운영 제어 시스템에 접속했습니다.${NC}"

# 1. 현재 가동 버전 확인 및 이력 관리
HISTORY_FILE="release_history.log"
[ ! -f "$HISTORY_FILE" ] && touch "$HISTORY_FILE"

CURRENT_APP_VERSION=$(grep "^VERSION_APP=" .env 2>/dev/null | cut -d'=' -f2)
CURRENT_UI_VERSION=$(grep "^VERSION_UI=" .env 2>/dev/null | cut -d'=' -f2)

echo -e "${YELLOW}📍 현재 가동 버전: API(${CURRENT_APP_VERSION:-N/A}) / UI(${CURRENT_UI_VERSION:-N/A})${NC}"

# 1.2 사용 가능한 버전 스캔 (images 폴더에서 mooldang-app-*.tar 파일 기반)
if [ ! -d "images" ]; then
    echo -e "${RED}❌ 오류: images 폴더가 없습니다.${NC}"
    exit 1
fi

# mooldang-app-*.tar 파일들로부터 버전 추출
VERSIONS=($(ls images/mooldang-app-*.tar 2>/dev/null | sed -E 's/.*mooldang-app-(.*)\.tar/\1/' | sort -r))

if [ ${#VERSIONS[@]} -eq 0 ]; then
    echo -e "${YELLOW}⚠️ 사용 가능한 이미지 버전이 없습니다. release.sh를 먼저 실행해주세요.${NC}"
    exit 1
fi

# 2. 버전 선택 메뉴
echo -e "\n${YELLOW}🚢 가동할 버전을 선택해주세요 (${GREEN}*표시${NC}는 현재 가동 버전):${NC}"
for i in "${!VERSIONS[@]}"; do
    MARK=""
    if [[ "${VERSIONS[$i]}" == "$CURRENT_APP_VERSION" ]] || [[ "${VERSIONS[$i]}" == "$CURRENT_UI_VERSION" ]]; then
        MARK=" (★현재 가동 중)"
    fi
    echo -e "$((i+1))) ${VERSIONS[$i]}${GREEN}$MARK${NC}"
done
echo "q) 종료"

read -p "선택 (번호 입력): " choice

if [[ "$choice" == "q" ]]; then
    echo "취소되었습니다."
    exit 0
fi

if [[ -z "$choice" || ! "$choice" =~ ^[0-9]+$ || "$choice" -lt 1 || "$choice" -gt ${#VERSIONS[@]} ]]; then
    echo -e "${RED}❌ 올바르지 않은 선택입니다.${NC}"
    exit 1
fi

SELECTED_VERSION=${VERSIONS[$((choice-1))]}
echo -e "${GREEN}▶️ 선택된 버전: $SELECTED_VERSION${NC}"

# 2.5 배포 대상 선택 (안정성을 위해 전체 배포 옵션 제거)
echo -e "\n${YELLOW}🎯 배포 대상을 선택해주세요 (실수 방지를 위해 개별 그룹 선택만 가능):${NC}"
echo "1) 백엔드만 (app, chzzk-bot)"
echo "2) 프론트엔드만 (studio, admin, overlay)"
read -p "선택 (번호 입력): " target_choice

DEPLOY_APPS=false
DEPLOY_UI=false
DEPLOY_SERVICES=()

case $target_choice in
    1) DEPLOY_APPS=true; DEPLOY_SERVICES=("app" "chzzk-bot") ;;
    2) DEPLOY_UI=true; DEPLOY_SERVICES=("studio" "admin" "overlay") ;;
    *) echo -e "${RED}❌ 올바른 대상을 선택해야 합니다.${NC}"; exit 1 ;;
esac

# 3. 이미지 로드
echo -e "${YELLOW}📥 선택한 서비스의 이미지를 로드합니다...${NC}"
for svc in "${DEPLOY_SERVICES[@]}"; do
    FILE="images/mooldang-$svc-$SELECTED_VERSION.tar"
    if [ -f "$FILE" ]; then
        echo "  - $svc 로딩 중..."
        docker load -i "$FILE"
    else
        echo -e "${RED}  - $svc ($FILE): 파일을 찾을 수 없습니다. (로드 건너뜀)${NC}"
    fi
done

# 4. .env 파일 업데이트 (선택된 대상만 업데이트)
if [ -f .env ]; then
    if [ "$DEPLOY_APPS" = true ]; then
        if grep -q "^VERSION_APP=" .env; then
            sed -i "s/^VERSION_APP=.*/VERSION_APP=$SELECTED_VERSION/" .env
        else
            echo "VERSION_APP=$SELECTED_VERSION" >> .env
        fi
        echo -e "${GREEN}📝 [API] 버전을 $SELECTED_VERSION 으로 업데이트했습니다.${NC}"
    fi

    if [ "$DEPLOY_UI" = true ]; then
        if grep -q "^VERSION_UI=" .env; then
            sed -i "s/^VERSION_UI=.*/VERSION_UI=$SELECTED_VERSION/" .env
        else
            echo "VERSION_UI=$SELECTED_VERSION" >> .env
        fi
        echo -e "${GREEN}📝 [UI] 버전을 $SELECTED_VERSION 으로 업데이트했습니다.${NC}"
    fi
else
    echo -e "${YELLOW}⚠️ .env 파일이 없어 직접 업데이트하지 못했습니다.${NC}"
fi

# 5. 함대 기동
echo -e "${GREEN}🚀 선택한 대상에게 버전($SELECTED_VERSION)을 적용합니다...${NC}"
docker compose -f docker-compose.app.yml up -d ${DEPLOY_SERVICES[@]}

echo -e "${GREEN}✅ 배포가 완료되었습니다!${NC}"

# 6. 배포 이력 기록
DEPLOY_DESC="Unknown"
[ "$target_choice" == "1" ] && DEPLOY_DESC="Backend Only"
[ "$target_choice" == "2" ] && DEPLOY_DESC="Frontend Only"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] DEPLOYED: Version=$SELECTED_VERSION, Target=$DEPLOY_DESC" >> "$HISTORY_FILE"

docker compose ps
