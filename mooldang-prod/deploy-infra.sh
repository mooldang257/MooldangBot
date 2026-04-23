#!/bin/bash

# ---------------------------------------------------------
# 🏗️ [MooldangBot] 인프라 전용 배포 스크립트 v1.0
# DB, Redis, RabbitMQ, Traefik, Monitoring 등 핵심 인프라 관리
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🏗️ 물댕 함대 핵심 인프라 기동 시스템을 시작합니다...${NC}"

# 1. 인프라 가동
echo -e "${YELLOW}🐳 Docker: 인프라 계층(docker-compose.infra.yml) 적용 중...${NC}"
docker compose -f docker-compose.infra.yml up -d

# 2. 결과 확인
echo -e "${GREEN}✅ 인프라 배포가 완료되었습니다!${NC}"
docker compose -f docker-compose.infra.yml ps
