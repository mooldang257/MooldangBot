#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 통합 버전 리포트 v3.0
# 설계 v5.2 준수: 1~9번 순서에 따른 환경별 서비스 현황 출력
# ---------------------------------------------------------

# 색상 정의
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BLUE='\033[0;34m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
NC='\033[0m'

print_container() {
    local label=$1
    local container_name=$2
    
    # 컨테이너 정보 조회 (Image, Status)
    local info=$(docker ps -a --filter "name=^/${container_name}$" --format "{{.Image}}\t{{.Status}}")
    
    if [ -z "$info" ]; then
        printf "  %-12s | %-35s | %b\n" "$label" "$container_name" "${RED}Stopped / Not Found${NC}"
    else
        local image=$(echo "$info" | cut -f1)
        local status=$(echo "$info" | cut -f2)
        
        local status_color=$GREEN
        if [[ $status == *"unhealthy"* ]] || [[ $status == *"Exited"* ]]; then
            status_color=$RED
        elif [[ $status == *"Up"* ]]; then
            status_color=$GREEN
        else
            status_color=$YELLOW
        fi
        
        printf "  %-12s | %-35s | %b%-20s%b\n" "$label" "$image" "$status_color" "$status" "$NC"
    fi
}

print_environment() {
    local env_name=$1
    local env_label=$2
    local env_color=$3

    echo -e "\n${env_color}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "  ${env_color}🌐 $env_label 환경 컨테이너 현황${NC}"
    echo -e "${env_color}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    echo -e "${CYAN}[1,2,3] Apps & Overlay (응용 서비스)${NC}"
    print_container "Studio" "mooldang-${env_name}-studio"
    print_container "Admin" "mooldang-${env_name}-admin"
    print_container "Overlay" "mooldang-${env_name}-overlay"

    echo -e "\n${CYAN}[4,5] Backend & Bot (백엔드 및 봇)${NC}"
    print_container "API" "mooldang-${env_name}-app"
    print_container "Bot" "mooldang-${env_name}-chzzk-bot"

    echo -e "\n${CYAN}[7] Infrastructure (전용 인프라)${NC}"
    print_container "DB" "mooldang-${env_name}-db"
    print_container "Redis" "mooldang-${env_name}-redis"
    print_container "MQ" "mooldang-${env_name}-rabbitmq"
    print_container "Grafana" "mooldang-${env_name}-grafana"
}

print_common_assets() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "  ${BLUE}⚓ COMMON ASSETS (공용 관문 및 자산)${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    echo -e "${CYAN}[8] Gateway (공용 관문/터널)${NC}"
    print_container "Traefik" "mooldang-bot-traefik"
    print_container "Tunnel" "mooldang-bot-tunnel"

    echo -e "\n${CYAN}[9] Shared Resources (공용 자산)${NC}"
    # [오시리스의 지혜]: 임베딩과 폰트는 개발/운영이 공유하는 단일 인스턴스입니다.
    print_container "Embedding" "mooldang-dev-embeddings"
    print_container "Fonts" "mooldang-common-fonts"
}

echo -e "${BLUE}======================================================================${NC}"
echo -e "   🚀 MooldangBot 통합 배포 버전 리포트 (Fleet Status)"
echo -e "${BLUE}======================================================================${NC}"

# 1. 개발 환경 출력 (dev)
print_environment "dev" "DEVELOPMENT (개발)" "$MAGENTA"

# 2. 운영 환경 출력 (bot)
print_environment "bot" "PRODUCTION (운영/BOT)" "$BLUE"

# 3. 공용 자산 출력 (Common)
print_common_assets

echo -e "\n${BLUE}======================================================================${NC}"
