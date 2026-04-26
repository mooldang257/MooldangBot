#!/bin/bash

# 🎨 [MooldangBot] 프론트엔드 통합 개발 배포 스크립트 (v1.0)
# 사용법: ./uirun.sh [버전] (예: ./uirun.sh v0.0.9)

# 1. 버전 설정 (입력값이 없으면 현재 시간 기준 자동 생성)
VERSION=$1
if [ -z "$VERSION" ]; then
    VERSION="ui-$(date +%Y%m%d-%H%M)"
fi

echo "🚀 [Deploy] 프론트엔드(UI) 환경 배포를 시작합니다. (Target Version: $VERSION)"

# 2. UI 빌드 실행
echo "📦 [1/4] 프론트엔드 소스 코드 빌드 중 (Studio/Admin/Overlay)..."
./build.sh --ui
if [ $? -ne 0 ]; then
    echo "❌ 빌드 실패! 배포를 중단합니다."
    exit 1
fi

# 3. 이미지 추출 및 로컬 로드
echo "📂 [2/4] UI 도커 이미지 추출 및 로컬 로드 중..."
./release.sh --ui --version $VERSION
if [ $? -ne 0 ]; then
    echo "❌ 이미지 추출 실패! 배포를 중단합니다."
    exit 1
fi

# 4. .env 버전 업데이트
echo "📝 [3/4] .env 파일 UI 버전 정보 업데이트 중 ($VERSION)..."
sed -i "s/^VERSION_UI=.*/VERSION_UI=$VERSION/" .env

# 5. 컨테이너 재시작
echo "🔄 [4/4] 인프라 확인 및 최신 이미지로 프론트엔드 컨테이너 재시작 중..."
docker compose -f docker-compose.infra.yml up -d
docker compose -f docker-compose.frontend.yml up -d

echo "✅ [SUCCESS] 프론트엔드 배포가 완료되었습니다!"
echo "📡 현재 UI 버전: $VERSION"
echo "🔍 로그 확인: docker compose -f docker-compose.frontend.yml logs -f studio"
