#!/bin/bash

# 색상 정의
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BLUE='\033[0;34m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

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
        
        # 컨테이너 정보 조회 (Image, Status)
        local info=$(docker ps --filter "name=^/${container_name}$" --format "{{.Image}}\t{{.Status}}")
        
        if [ -z "$info" ]; then
            printf "  %-12s | %-35s | %b\n" "$label" "$container_name" "${RED}Stopped / Not Found${NC}"
        else
            local image=$(echo "$info" | cut -f1)
            local status=$(echo "$info" | cut -f2)
            
            # 상태에 따른 색상 처리
            local status_color=$GREEN
            if [[ $status == *"unhealthy"* ]] || [[ $status == *"Exited"* ]]; then
                status_color=$RED
            elif [[ $status == *"starting"* ]]; then
                status_color=$YELLOW
            fi
            
            printf "  %-12s | %-35s | %b%-20s%b\n" "$label" "$image" "$status_color" "$status" "$NC"
        fi
    }

    echo -e "${CYAN}[2] Infra (인프라)${NC}"
    print_container "DB" "db"
    print_container "Redis" "redis"
    print_container "MQ" "rabbitmq"

    echo -e "\n${CYAN}[3] Gateway (게이트웨이)${NC}"
    print_container "Traefik" "traefik"
    if [ "$env_name" == "prod" ]; then
        print_container "Tunnel" "tunnel"
    fi

    echo -e "\n${CYAN}[4] Bot (치지직 봇)${NC}"
    print_container "Bot" "chzzk-bot"

    echo -e "\n${CYAN}[5] Backend (백엔드)${NC}"
    print_container "API" "app"

    echo -e "\n${CYAN}[6] Frontend (프론트엔드)${NC}"
    print_container "Studio" "studio"
    print_container "Admin" "admin"
    print_container "Overlay" "overlay"
}

echo -e "${BLUE}======================================================================${NC}"
echo -e "   🚀 MooldangBot 통합 배포 버전 리포트 (Dev & Prod)"
echo -e "${BLUE}======================================================================${NC}"

# 개발 환경 출력
print_environment "dev" "DEVELOPMENT (개발)" "$MAGENTA"

# 운영 환경 출력
print_environment "prod" "PRODUCTION (운영)" "$BLUE"

echo -e "\n${BLUE}======================================================================${NC}"
