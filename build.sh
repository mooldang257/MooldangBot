#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 통합 배포 스크립트 v4.0 (Unified)
# 이 스크립트는 통합된 docker-compose.yml을 사용하여 빌드 및 가동을 수행합니다.
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

# 3. 파라미터 분석 및 대화형 메뉴
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
        shift
    done
fi

# 4. Traefik 지휘부 상태 확인
if ! docker ps | grep -q "mooldang-traefik"; then
    echo -e "${YELLOW}⚠️ Traefik 지휘부가 가동 중이지 않습니다. 기동을 시도합니다...${NC}"
fi

# 6. 컨테이너 교체 및 무중단 적용
BUILD_OPTS=""
[ "$CLEAN_BUILD" = true ] && BUILD_OPTS="--no-cache"

echo -e "${GREEN}🐳 Docker: 통합 함대 기동 중 [${DEPLOY_TARGETS[*]}]...${NC}"

# 이제 -dev 접미사 없이 통합된 docker-compose.yml을 사용합니다.
docker compose -f docker-compose.yml --env-file .env up -d --build $BUILD_OPTS ${DEPLOY_TARGETS[@]}

# 7. 마무리
echo -e "${GREEN}🧹 System: 불필요한 이미지 잔해 청소...${NC}"
docker image prune -f

echo -e "${GREEN}✅ 통합 배포가 성공적으로 완료되었습니다!${NC}"
echo -e "${YELLOW}📊 Traefik DashBoard: http://localhost:8080 (내부망)${NC}"
