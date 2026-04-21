# 1. 최신 보안 패치 반영
git pull origin main
# 2. 컨테이너 재빌드 및 리스타트
docker compose up -d --build
# 3. 실시간 세션 진단 모니터링
docker logs -f mooldang-app | grep "[인증]"
