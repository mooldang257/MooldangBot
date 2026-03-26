#!/bin/bash

# ---------------------------------------------------------
# 🌊 [물댕봇] 리눅스 자동 배포 스크립트 (deploy.sh)
# ---------------------------------------------------------

# 색상 정의
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 물댕봇 서버 배포를 시작합니다...${NC}"

# 1. 최신 소스 코드 다운로드
echo -e "${GREEN}📥 Git Pull: 최신 소스를 가져오는 중...${NC}"
git pull origin main

# 2. .env 파일 존재 여부 확인 (보안 체크)
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 존재하지 않습니다.${NC}"
    echo "appsettings.json을 참고하여 .env 파일을 먼저 생성해주세요."
    exit 1
fi

# 3. 기존 컨테이너 중지 및 최신 이미지 빌드/실행
echo -e "${GREEN}🛠️ Docker: 빌드 및 컨테이너 재가동 중...${NC}"
docker-compose down
docker-compose up -d --build

# 4. DB 마이그레이션 결과 확인
echo -e "${GREEN}🔍 DB Migration: 마이그레이션 상태 확인 중...${NC}"
docker-compose logs -f migration | grep -m 1 "Done"

# 5. 최종 상태 보고
echo -e "${GREEN}✅ 배포가 완료되었습니다!${NC}"
echo "현재 가동 중인 컨테이너 목록:"
docker-compose ps
