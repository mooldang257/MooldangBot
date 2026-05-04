#!/bin/bash

# ---------------------------------------------------------
# 🌊 [MooldangBot] 공용 폰트 자산 빌드 스크립트 v1.0
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 환경 변수 로드
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

# 버전 설정 (기본값은 v0.0.1)
VERSION=${VERSION_APP:-v0.0.1}

echo -e "${YELLOW}🏗️  폰트 자산 빌드 시작 (버전: $VERSION)...${NC}"

# 빌드 컨텍스트: ../fonts
cd ../fonts

# 도커 빌드 실행
docker build -t mooldang-fonts:$VERSION .
docker tag mooldang-fonts:$VERSION mooldang-fonts:latest

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ 폰트 빌드 및 태깅 완료: mooldang-fonts:$VERSION${NC}"
else
    echo -e "${RED}❌ 폰트 빌드 실패!${NC}"
    exit 1
fi
