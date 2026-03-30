#!/bin/bash

# ---------------------------------------------------------
# 🌊 [물댕봇] 서버 배포 고도화 스크립트 (deploy.sh)
# ---------------------------------------------------------

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 물댕봇 서버 배포 및 최적화를 시작합니다...${NC}"

# 1. 환경 변수 체크
if [ ! -f .env ]; then
    echo -e "${RED}❌ 오류: .env 파일이 없습니다. .env.sample을 복사하여 먼저 생성해주세요.${NC}"
    exit 1
fi

# 2. 소스 코드 동기화
echo -e "${GREEN}📥 Git: 최신 소스 코드를 동기화 중...${NC}"
git pull

# 3. 사전 빌드 검사 (로컬 dotnet 설치 시)
if command -v dotnet &> /dev/null; then
    echo -e "${GREEN}🛠️ Build Check: 컴파일 오류 여부를 사전에 확인합니다...${NC}"
    dotnet build -c Release
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ 빌드 오류가 발생하여 배포를 중단합니다.${NC}"
        exit 1
    fi
fi

# 4. 컨테이너 재가동
echo -e "${GREEN}🐳 Docker: 컨테이너 빌드 및 백그라운드 실행 중...${NC}"
# --remove-orphans를 추가하여 설정에서 제거된 이전 컨테이너들을 확실히 청소합니다.
docker-compose down --remove-orphans
docker-compose up -d --build

# 5. 상태 모니터링
echo -e "${GREEN}🔍 Health Check: 서비스 가동 상태를 확인합니다...${NC}"
sleep 5
docker-compose ps

echo -e "${GREEN}✅ 배포가 성공적으로 완료되었습니다!${NC}"
echo "로그 확인: docker-compose logs -f app"
