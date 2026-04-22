#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 개발 환경 빌드 스크립트 v3.0 (Development Only)
# 이 스크립트는 개발(dev) 환경 구동 및 이미지 빌드에만 사용됩니다.
# 운영 배포는 scripts/promote-to-prod.sh를 사용하세요.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 물댕봇 Traefik 관제 시스템 기반 배포를 가동합니다...${NC}"

# 1. 환경 변수 체크
if [ ! -f .env.dev ]; then
    echo -e "${RED}❌ 오류: .env.dev 파일이 없습니다. .env를 복사하여 생성해주세요.${NC}"
    exit 1
fi

# 2. 소스 코드 동기화 (필요시 주석 해제)
# echo -e "${GREEN}📥 Git: 최신 코드를 동기화 중...${NC}"
# git fetch --all && git reset --hard origin/main

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
    echo -e "${YELLOW}⚠️ Traefik 지휘부가 가동 중이지 않습니다. 운영용 docker-compose.yml에서 먼저 가동해야 합니다.${NC}"
fi

# 5. [중요] 사전 검증 (Shift-Left Verification)
if [[ " ${DEPLOY_TARGETS[@]} " =~ " app " || -z "${DEPLOY_TARGETS[*]}" ]]; then
    echo -e "${YELLOW}⚖️ Verifier: 배포 전 오시리스의 저울로 Contract 정합성을 검증합니다...${NC}"
    docker compose -f docker-compose.dev.yml --env-file .env.dev build chzzk-bot-dev
    docker compose -f docker-compose.dev.yml --env-file .env.dev run --rm --no-deps --entrypoint "dotnet" chzzk-bot-dev verifier/MooldangBot.Verifier.dll
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ 치명적 오류: 정합성 검증 실패! 배포를 취소합니다.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ 정합성 검증 통과.${NC}"
fi

# 6. 컨테이너 교체 및 무중단 적용
BUILD_OPTS=""
[ "$CLEAN_BUILD" = true ] && BUILD_OPTS="--no-cache"

echo -e "${GREEN}🐳 Docker: 개발 함대 기동 중 [${DEPLOY_TARGETS[*]}]...${NC}"
# 개발팀은 접미사 -dev가 붙은 서비스를 대상으로 합니다.
DEV_TARGETS=()
for target in "${DEPLOY_TARGETS[@]}"; do
    if [ -n "$target" ]; then
        DEV_TARGETS+=("${target}-dev")
    fi
done

docker compose -f docker-compose.dev.yml --env-file .env.dev up -d --build $BUILD_OPTS ${DEV_TARGETS[@]}

# 7. 마무리
echo -e "${GREEN}🧹 System: 불필요한 이미지 잔해 청소...${NC}"
docker image prune -f

echo -e "${GREEN}✅ 배포가 성공적으로 완료되었습니다!${NC}"
echo -e "${YELLOW}📊 Traefik DashBoard: http://localhost:8080 (내부망)${NC}"
