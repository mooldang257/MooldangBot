#!/bin/bash

# 🌊 [MooldangBot] 통합 개발 배포 스크립트 (v1.0)
# 사용법: ./deploy-dev.sh [버전] (예: ./deploy-dev.sh v0.0.9)

# 1. 버전 설정 (입력값이 없으면 현재 시간 기준 자동 생성)
VERSION=$1
if [ -z "$VERSION" ]; then
    VERSION="dev-$(date +%Y%m%d-%H%M)"
fi

echo "🚀 [Deploy] 개발 환경 배포를 시작합니다. (Target Version: $VERSION)"

# 2. 빌드 실행
echo "📦 [1/4] 소스 코드 빌드 중..."
./build.sh --app
if [ $? -ne 0 ]; then
    echo "❌ 빌드 실패! 배포를 중단합니다."
    exit 1
fi

# 3. 이미지 추출 및 로드
echo "📂 [2/4] 도커 이미지 추출 및 로컬 로드 중..."
./release.sh --app --version $VERSION
if [ $? -ne 0 ]; then
    echo "❌ 이미지 추출 실패! 배포를 중단합니다."
    exit 1
fi

# 4. .env 버전 업데이트
echo "📝 [3/4] .env 파일 버전 정보 업데이트 중 ($VERSION)..."
sed -i "s/^VERSION_APP=.*/VERSION_APP=$VERSION/" .env

# 5. 컨테이너 재시작
echo "🔄 [4/4] 인프라 확인 및 최신 이미지로 백엔드 컨테이너 재시작 중..."
docker compose -f docker-compose.infra.yml up -d
docker compose -f docker-compose.backend.yml up -d

echo "✅ [SUCCESS] 배포가 완료되었습니다!"
echo "📡 현재 버전: $VERSION"
echo "🔍 로그 확인: docker compose -f docker-compose.backend.yml logs -f"
