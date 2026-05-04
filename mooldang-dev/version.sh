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

print_environment() {
    local env_name=$1
    local env_label=$2
    local env_color=$3

    echo -e "\n${env_color}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "  ${env_color}🌐 $env_label 환경 컨테이너 현황${NC}"
    echo -e "${env_color}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    print_container() {
        local label=$1
        local service_name=$2
        local container_name="mooldang-${env_name}-${service_name}"
        
        # 공용 자산(fonts) 및 게이트웨이 예외 처리
        if [ "$service_name" == "fonts" ]; then
            if [ "$env_name" == "dev" ]; then
                container_name="mooldang-dev-fonts"
            else
                container_name="mooldang-fonts"
            fi
        fi

        # 컨테이너 정보 조회 (Image, Status)
        local info=$(docker ps -a --filter "name=${container_name}" --format "{{.Image}}\t{{.Status}}" | head -n 1)
        
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

    echo -e "${CYAN}[1,2] Frontend UI (프론트엔드)${NC}"
    print_container "Studio" "studio"
    print_container "Admin" "admin"

    echo -e "\n${CYAN}[3] Overlay (오버레이)${NC}"
    print_container "Overlay" "overlay"

    echo -e "\n${CYAN}[4] Backend API (백엔드)${NC}"
    print_container "API" "app"

    echo -e "\n${CYAN}[5] Bot (치지직 봇)${NC}"
    print_container "Bot" "chzzk-bot"

    echo -e "\n${CYAN}[7] Infrastructure (인프라)${NC}"
    print_container "DB" "db"
    print_container "Redis" "redis"
    print_container "MQ" "rabbitmq"
    print_container "Grafana" "grafana"

    echo -e "\n${CYAN}[8] Gateway (게이트웨이)${NC}"
    # 게이트웨이는 bot 환경(운영)에만 존재하므로 조건 처리
    if [ "$env_name" == "bot" ]; then
        print_container "Traefik" "traefik"
        print_container "Tunnel" "tunnel"
    else
        echo -e "  Shared with Bot Environment"
    fi

    echo -e "\n${CYAN}[9] Global Assets (공용 자산: Shared)${NC}"
    print_container "Embedding" "embeddings"
    print_container "Fonts" "fonts"
}

echo -e "${BLUE}======================================================================${NC}"
echo -e "   🚀 MooldangBot 통합 배포 버전 리포트 (Dev & Bot)"
echo -e "${BLUE}======================================================================${NC}"

# 개발 환경 출력 (dev)
print_environment "dev" "DEVELOPMENT (개발)" "$MAGENTA"

# 운영 환경 출력 (bot)
print_environment "bot" "PRODUCTION (운영/BOT)" "$BLUE"

echo -e "\n${BLUE}======================================================================${NC}"
