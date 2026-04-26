#!/bin/bash

# ---------------------------------------------------------
# 🚀 [MooldangBot] 운영 이관용 이미지 버전 태깅 및 추출 스크립트 v1.0
# 개발(dev)에서 빌드된 latest 이미지를 특정 버전으로 태깅하고 추출합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}📦 운영 환경 이관을 위한 이미지 버전 관리를 시작합니다...${NC}"

# 0. 버전 자동 증가 함수
increment_version() {
    local v=$1
    local clean_v=${v#v}
    IFS='.' read -r major minor patch <<< "$clean_v"
    patch=$((patch + 1))
    echo "v${major:-0}.${minor:-0}.${patch:-1}"
}

# 1. 환경 변수에서 현재 버전 읽기
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다.${NC}"
    exit 1
fi

CUR_APP_VER=$(grep "^VERSION_APP=" .env | cut -d'=' -f2)
CUR_UI_VER=$(grep "^VERSION_UI=" .env | cut -d'=' -f2)

# 1.2 인자 분석
IMAGE_VERSION=""
RELEASE_TARGETS=()
AUTO_INCREMENT=true
TARGET_TYPE="" # app or ui

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --all) RELEASE_TARGETS+=("app" "chzzk-bot" "studio" "admin" "overlay"); TARGET_TYPE="all" ;;
        --app) RELEASE_TARGETS+=("app" "chzzk-bot"); TARGET_TYPE="app" ;;
        --ui) RELEASE_TARGETS+=("studio" "admin" "overlay"); TARGET_TYPE="ui" ;;
        --version|-v) shift; IMAGE_VERSION=$1; AUTO_INCREMENT=false ;;
        *) 
            if [[ "$1" == v* ]]; then
                IMAGE_VERSION=$1; AUTO_INCREMENT=false
            else
                RELEASE_TARGETS+=("$1")
            fi
            ;;
    esac
    shift
done

if [ ${#RELEASE_TARGETS[@]} -eq 0 ]; then
    echo -e "${RED}❌ 오류: 배포 대상(--app 또는 --ui)을 지정해야 합니다.${NC}"
    exit 1
fi

# 1.3 버전 결정
if [ "$AUTO_INCREMENT" = true ]; then
    if [ "$TARGET_TYPE" == "app" ]; then
        IMAGE_VERSION=$(increment_version ${CUR_APP_VER:-v0.0.0})
        echo -e "${YELLOW}🔄 백엔드 버전 자동 증가: $CUR_APP_VER -> $IMAGE_VERSION${NC}"
        sed -i "s/^VERSION_APP=.*/VERSION_APP=$IMAGE_VERSION/" .env
    elif [ "$TARGET_TYPE" == "ui" ]; then
        IMAGE_VERSION=$(increment_version ${CUR_UI_VER:-v0.0.0})
        echo -e "${YELLOW}🔄 프론트엔드 버전 자동 증가: $CUR_UI_VER -> $IMAGE_VERSION${NC}"
        sed -i "s/^VERSION_UI=.*/VERSION_UI=$IMAGE_VERSION/" .env
    else
        # 혼합 배포의 경우 (둘 다 지정 또는 개별 서비스 지정)
        echo -e "${YELLOW}⚠️ 여러 대상이 섞여 있어 자동 증가를 위해 버전을 직접 입력받아야 합니다.${NC}"
        read -p "배포할 버전명을 입력하세요 (예: v1.0.0): " IMAGE_VERSION
    fi
fi

if [ -z "$IMAGE_VERSION" ]; then
    read -p "배포할 버전명을 입력하세요 (예: v1.0.0): " IMAGE_VERSION
fi

# 2. 운영 환경 설정 업데이트 및 태깅
PROD_ENV="../mooldang-prod/.env"
SERVICES_TO_RELEASE=("${RELEASE_TARGETS[@]}")

echo -e "${YELLOW}🏷️ 대상 서비스([${RELEASE_TARGETS[*]}])에 버전($IMAGE_VERSION) 적용을 시작합니다...${NC}"

for svc in "${SERVICES_TO_RELEASE[@]}"; do
    IMG_NAME="mooldang-$svc"
    
    # 로컬에 latest 이미지가 있는지 확인
    if docker images -q "$IMG_NAME:latest" > /dev/null; then
        echo -e "  - $IMG_NAME: ${GREEN}latest -> $IMAGE_VERSION${NC} 태깅 중..."
        docker tag "$IMG_NAME:latest" "$IMG_NAME:$IMAGE_VERSION"
    else
        echo -e "  - $IMG_NAME: ${RED}latest 이미지를 찾을 수 없습니다.${NC}"
    fi
done

# 3. 운영 환경 .env 파일 업데이트
if [ -f "$PROD_ENV" ]; then
    echo -e "${YELLOW}📝 운영 환경($PROD_ENV) 버전을 업데이트합니다...${NC}"
    if [ "$TARGET_TYPE" == "app" ]; then
        sed -i "s/^VERSION_APP=.*/VERSION_APP=$IMAGE_VERSION/" "$PROD_ENV"
        echo -e "  - VERSION_APP -> ${GREEN}$IMAGE_VERSION${NC}"
    elif [ "$TARGET_TYPE" == "ui" ]; then
        sed -i "s/^VERSION_UI=.*/VERSION_UI=$IMAGE_VERSION/" "$PROD_ENV"
        echo -e "  - VERSION_UI -> ${GREEN}$IMAGE_VERSION${NC}"
    else
        # 개별 또는 혼합 배포 시 둘 다 업데이트 시도 (안전책)
        sed -i "s/^VERSION_APP=.*/VERSION_APP=$IMAGE_VERSION/" "$PROD_ENV"
        sed -i "s/^VERSION_UI=.*/VERSION_UI=$IMAGE_VERSION/" "$PROD_ENV"
        echo -e "  - VERSION_APP/UI -> ${GREEN}$IMAGE_VERSION${NC}"
    fi
else
    echo -e "${RED}❌ 오류: 운영 환경 .env 파일을 찾을 수 없습니다 ($PROD_ENV).${NC}"
    exit 1
fi

echo -e "${GREEN}✅ 운영 이관 준비가 완료되었습니다! (이미지 태깅 및 .env 업데이트 완료)${NC}"
echo -e "\n${YELLOW}🚢 [다음 단계] 운영 서버에서:${NC}"
echo -e "1. cd ../mooldang-prod"
echo -e "2. ./deploy.sh ${TARGET_TYPE:-all}"

echo -e "\n${YELLOW}⏪ 롤백 방법:${NC}"
echo -e "./rollback.sh 명령어를 사용하여 이전 버전으로 즉시 복구할 수 있습니다."

