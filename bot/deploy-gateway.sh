#!/bin/bash

# ---------------------------------------------------------
# 🛡️ [MooldangBot] 게이트웨이 전용 배포 스크립트 v1.0
# Traefik, Cloudflare Tunnel 등 핵심 관문 관리
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🛡️ 물댕 함대 게이트웨이(관문) 제어 시스템을 시작합니다...${NC}"

# 0. 필수 네트워크 자동 생성
docker network ls | grep -q "mooldang_prod_net" || docker network create mooldang_prod_net

# 1. 게이트웨이 가동
echo -e "${YELLOW}🐳 Docker: 게이트웨이 계층(docker-compose.gateway.yml) 적용 중...${NC}"
docker compose -f docker-compose.gateway.yml up -d

# 2. 결과 확인
echo -e "${GREEN}✅ 게이트웨이 배포가 완료되었습니다!${NC}"
echo -e "${YELLOW}⚠️ 게이트웨이는 전체 서비스의 통로이므로 신중히 관리해주세요.${NC}"
docker compose -f docker-compose.gateway.yml ps
