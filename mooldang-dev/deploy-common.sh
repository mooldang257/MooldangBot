#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 공통 자산(Global Assets) 배포 스크립트 v1.0
# 임베딩 서버 및 폰트 등 개발/운영 공용 리소스 관리
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 변수 로드
[ -f .env ] && source .env

echo -e "${BLUE}======================================================================${NC}"
echo -e "   🚀 MooldangBot 공통 자산(Global Assets) 관리"
echo -e "${BLUE}======================================================================${NC}"

echo -e "${YELLOW}📦 배포 대상을 선택해주세요:${NC}"
echo "1) 임베딩 서버 (Shared AI Engine: BGE-M3)"
echo "2) 공용 폰트 (Shared Fonts Assets)"
echo "3) 전체 배포 (All Common Assets)"
read -p "선택 (번호): " choice

# 컴포즈 파일 설정
INFRA="-f docker-compose.infra.yml"
SHARED="-f docker-compose.shared.yml"

# 네트워크 확인 및 생성
ensure_networks() {
    for net in "mooldang_dev_net" "mooldang_prod_net"; do
        if ! docker network ls | grep -q "$net"; then
            echo -e "${YELLOW}🌐 네트워크($net) 생성 중...${NC}"
            docker network create $net
        fi
    done
}

deploy() {
    local name=$1
    local files=$2
    local services=$3
    echo -e "${YELLOW}🏗️  [Global] $name 가동 중...${NC}"
    ensure_networks
    docker compose $files up -d $services
}

case $choice in
    1) # Embedding
        deploy "Embedding Engine" "$INFRA" "embedding-server" ;;
    2) # Fonts
        deploy "Shared Fonts" "$SHARED" "fonts" ;;
    3) # All
        deploy "All Common Assets" "$INFRA $SHARED" "embedding-server fonts" ;;
    *)
        echo -e "${RED}❌ 잘못된 선택입니다.${NC}"
        exit 1
        ;;
esac

echo -e "${GREEN}✅ 공통 자산 배포 프로세스가 완료되었습니다!${NC}"
docker ps --filter "label=com.docker.compose.service=embedding-server" --filter "label=com.docker.compose.service=fonts" --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"
