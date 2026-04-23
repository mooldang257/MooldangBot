#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 통합 배포 및 이미지 자동 추출 스크립트 v4.1
# 이 스크립트는 빌드 후 이미지를 mooldang-images 폴더로 자동 저장합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 물댕봇 통합 관제 시스템 가동을 시작합니다...${NC}"

# 1. 환경 변수 체크
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다. 설정을 확인해주세요.${NC}"
    exit 1
fi

DEPLOY_TARGETS=()
CLEAN_BUILD=false

if [[ "$#" -eq 0 ]]; then
    echo -e "${YELLOW}💬 배포 대상을 선택해주세요 (스페이스로 다중 선택 가능):${NC}"
    echo "1) 전체 배포 (함대 총출격)"
    echo "2) 백엔드만 (app, chzzk-bot)"
    echo "3) 프론트엔드 전체 (studio, admin, overlay)"
    echo "4) 개별 서비스 선택"
    read -p "선택 (예: 2 3): " choices

    for choice in $choices; do
        case $choice in
            1) DEPLOY_TARGETS=(""); break ;;
            2) DEPLOY_TARGETS+=("app" "chzzk-bot") ;;
            3) DEPLOY_TARGETS+=("studio" "admin" "overlay") ;;
            4) 
                read -p "서비스명 입력 (예: studio admin): " services
                DEPLOY_TARGETS+=($services)
                ;;
        esac
    done
else
    while [[ "$#" -gt 0 ]]; do
        case $1 in
            --all) DEPLOY_TARGETS=(""); break ;;
            --app) DEPLOY_TARGETS+=("app" "chzzk-bot") ;;
            --ui) DEPLOY_TARGETS+=("studio" "admin" "overlay") ;;
            --clean) CLEAN_BUILD=true ;;
            *) DEPLOY_TARGETS+=("$1") ;;
        esac
    done
fi

# 3. Traefik 지휘부 상태 확인
if ! docker ps | grep -q "mooldang-traefik"; then
    echo -e "${YELLOW}⚠️ Traefik 지휘부가 가동 중이지 않습니다. 기동을 시도합니다...${NC}"
fi

# 4. 컨테이너 빌드 및 가동
BUILD_OPTS=""
[ "$CLEAN_BUILD" = true ] && BUILD_OPTS="--no-cache"

# [v4.2] 계층형 아키텍처 지원: 서비스 선택에 따라 필요한 파일만 포함하거나 전체 함대 운용
COMPOSE_INFRA="-f docker-compose.infra.yml"
COMPOSE_BACKEND="-f docker-compose.backend.yml"
COMPOSE_FRONTEND="-f docker-compose.frontend.yml"

echo -e "${GREEN}🐳 Docker: 계층형 함대 기동 중 [${DEPLOY_TARGETS[*]}]...${NC}"

# 항상 인프라 레이어를 포함하여 네트워크 및 기본 공조 체계 유지
docker compose $COMPOSE_INFRA $COMPOSE_BACKEND $COMPOSE_FRONTEND --env-file .env up -d --build $BUILD_OPTS ${DEPLOY_TARGETS[@]}

# 5. 이미지 자동 추출 (명시적 관리 폴더로 저장)
IMAGE_EXPORT_DIR="./images"
mkdir -p "$IMAGE_EXPORT_DIR"

echo -e "${YELLOW}📦 빌드된 이미지를 $IMAGE_EXPORT_DIR 폴더로 자동 추출합니다...${NC}"

# [v4.3] 지능형 추출 로직: 선택된 배포 대상이 있으면 해당 서비스만, 없으면(전체) 기본 목록 추출
if [[ ${#DEPLOY_TARGETS[@]} -eq 0 || "${DEPLOY_TARGETS[0]}" == "" ]]; then
    SERVICES_TO_EXPORT=("app" "chzzk-bot" "studio" "overlay" "admin")
else
    # 사용자가 개별 선택한 서비스들만 추출 대상으로 선정
    SERVICES_TO_EXPORT=("${DEPLOY_TARGETS[@]}")
fi

for svc in "${SERVICES_TO_EXPORT[@]}"; do
    IMG_NAME="mooldang-$svc"
    # 예외 케이스 처리
    [ "$svc" == "admin" ] && IMG_NAME="mooldang-admin"
    
    if docker images -q "$IMG_NAME:latest" > /dev/null; then
        echo -e "  - $IMG_NAME:latest 추출 중..."
        docker save -o "$IMAGE_EXPORT_DIR/$IMG_NAME-latest.tar" "$IMG_NAME:latest"
    fi
done

# 6. 마무리
echo -e "${GREEN}🧹 System: 불필요한 이미지 잔해 청소...${NC}"
docker image prune -f

echo -e "${GREEN}✅ 통합 배포 및 이미지 자동 추출이 완료되었습니다!${NC}"
echo -e "${YELLOW}📊 추출된 파일 확인: $IMAGE_EXPORT_DIR${NC}"
echo -e "${YELLOW}📊 Traefik DashBoard: http://localhost:8080 (내부망)${NC}"
