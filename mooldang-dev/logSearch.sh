#!/bin/bash

# ---------------------------------------------------------
# 🔍 [MooldangBot] 로그 통합 검색 스크립트 v1.2
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 0. 환경 선택
echo -e "${YELLOW}🌐 로그를 추적할 환경을 선택해주세요:${NC}"
echo "1) 개발 (Development: mooldang-dev-app)"
echo "2) 운영 (Production: mooldang-bot-app)"
read -p "선택 (번호, 기본값 1): " env_choice
env_choice=${env_choice:-1}

if [ "$env_choice" == "1" ]; then
    CONTAINER_NAME="mooldang-dev-app"
else
    CONTAINER_NAME="mooldang-bot-app"
fi

# 1. 대상 선택
echo -e "${YELLOW}🚀 검색할 로그 카테고리를 선택해주세요:${NC}"
echo "1) 썸네일 검색 ([SongBookThumbnail])"
echo "2) 전체 앱 로그 (Full Logs)"
echo "3) AI/LLM 추론 로그 (LLM/AI/Gemini)"
echo "4) 데이터베이스/쿼리 로그 (SQL)"
echo "5) 에러 로그 (Error/Exception)"
echo "6) 특정 키워드 직접 입력"
echo "7) 종료"

read -p "선택 (번호): " choice

case $choice in
    1)
        SEARCH_TERM="\[SongBookThumbnail\]"
        ;;
    2)
        SEARCH_TERM=""
        ;;
    3)
        SEARCH_TERM="LLM|AI|Generative|VertexAI|Gemini"
        ;;
    4)
        SEARCH_TERM="Microsoft.EntityFrameworkCore|SQL|Query|Npgsql"
        ;;
    5)
        SEARCH_TERM="Error|Exception|Fail|Critical"
        ;;
    6)
        read -p "검색할 키워드를 입력하세요: " custom_term
        SEARCH_TERM=$custom_term
        ;;
    7)
        echo -e "${GREEN}👋 종료합니다.${NC}"
        exit 0
        ;;
    *)
        echo -e "${RED}❌ 올바르지 않은 번호입니다.${NC}"
        exit 1
        ;;
esac

echo "=========================================================="
if [ -z "$SEARCH_TERM" ]; then
    echo -e " 🔍 ${GREEN}전체 로그${NC}를 추적 중..."
else
    echo -e " 🔍 ${GREEN}'$SEARCH_TERM'${NC} 관련 로그를 추적 중..."
fi
echo -e " 📦 컨테이너: ${YELLOW}$CONTAINER_NAME${NC}"
echo " 💡 중단하려면 Ctrl+C를 누르세요."
echo "=========================================================="

# 실시간 로그 스트리밍
if [ -z "$SEARCH_TERM" ]; then
    docker logs -f --tail 100 "$CONTAINER_NAME"
else
    docker logs -f --tail 100 "$CONTAINER_NAME" 2>&1 | grep --line-buffered --color=always -Ei "$SEARCH_TERM"
fi
