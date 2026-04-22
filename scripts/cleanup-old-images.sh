#!/bin/bash
# MooldangBot 이미지 로테이션 및 정리 스크립트 (v1.0)
# 정책: 각 앱 레포지토리별로 최신 4개(현재+백업3)만 남기고 나머지 v* 태그 이미지 삭제

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

KEEP_COUNT=4 # 현재 버전 + 이전 백업 3개

echo -e "${GREEN}🧹 [MooldangBot] 구버전 이미지 정리 로직을 가동합니다... (정책: 최근 ${KEEP_COUNT}개 유지)${NC}"

# 1. 우리 앱 레포지토리 목록 추출 (mooldang- 시작하는 것들)
REPOS=$(docker images --format "{{.Repository}}" | grep "^mooldang-" | sort -u)

for REPO in $REPOS; do
    # 인프라(DB, Redis 등)는 제외 필터링 (빌드 결과물만 대상)
    # 현재 빌드 결과물: app, chzzk-bot, studio, admin, overlay, migration
    if [[ "$REPO" =~ "db"|"redis"|"rabbitmq"|"traefik"|"loki"|"grafana"|"prometheus"|"adminer" ]]; then
        continue
    fi

    echo -e "${YELLOW}🔍 리포지토리 확인: $REPO${NC}"

    # v* 태그들을 버전 순으로 정렬하여 삭제 대상 추출 (v0.0.1 형태)
    # tail -n +$((KEEP_COUNT + 1)) 은 상위 4개를 제외한 나머지를 의미
    OLD_TAGS=$(docker images --format "{{.Tag}}" "$REPO" | grep -E "^v[0-9.]+" | sort -V -r | tail -n +$((KEEP_COUNT + 1)) || true)

    if [ -z "$OLD_TAGS" ]; then
        echo "   - 삭제할 구버전 이미지가 없습니다."
        continue
    fi

    for TAG in $OLD_TAGS; do
        IMAGE_FULL="$REPO:$TAG"
        echo -e "${RED}   - 삭제 대상 발견: $IMAGE_FULL${NC}"
        
        # 실제 삭제 수행 (오류가 나도 중단하지 않고 계속 진행)
        docker rmi "$IMAGE_FULL" || echo -e "${RED}     ⚠️ 삭제 실패 (컨테이너에서 사용 중일 수 있음)${NC}"
    done
done

echo -e "${GREEN}✨ 이미지 정리가 완료되었습니다!${NC}"
