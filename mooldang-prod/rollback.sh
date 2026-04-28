#!/bin/bash

# ---------------------------------------------------------
# ⏪ [MooldangBot] 긴급 롤백 자동화 스크립트 v1.0
# 배포 이력을 확인하고 이전 버전으로 즉시 복구합니다.
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

HISTORY_FILE="release_history.log"

echo -e "${RED}⚠️ 긴급 롤백 시스템을 가동합니다.${NC}"

if [ ! -f "$HISTORY_FILE" ]; then
    echo -e "${RED}❌ 오류: 배포 이력 파일이 없습니다.${NC}"
    exit 1
fi

# 1. 최근 배포 이력 표시 (최근 10개)
echo -e "${YELLOW}🕒 최근 배포 이력 (최신순):${NC}"
tail -n 10 "$HISTORY_FILE" | tac | nl -w2 -s') '

echo -e "\n${YELLOW}롤백할 버전을 선택하세요 (취소: q):${NC}"
read -p "선택: " choice

if [[ "$choice" == "q" ]]; then
    echo "취소되었습니다."
    exit 0
fi

# 2. 선택된 이력에서 버전 정보 추출
# 로그 형식: [2026-04-26 09:44:31] DEPLOYED: Target=all, APP=v0.0.8, UI=v0.0.8
SELECTED_LOG=$(tail -n 10 "$HISTORY_FILE" | tac | sed -n "${choice}p")

if [ -z "$SELECTED_LOG" ]; then
    echo -e "${RED}❌ 올바르지 않은 선택입니다.${NC}"
    exit 1
fi

OLD_APP=$(echo "$SELECTED_LOG" | grep -o "APP=[^,]*" | cut -d'=' -f2)
OLD_UI=$(echo "$SELECTED_LOG" | grep -o "UI=[^,]*" | cut -d'=' -f2)

echo -e "${YELLOW}🔄 복구 대상: API($OLD_APP) / UI($OLD_UI)${NC}"
read -p "정말로 롤백하시겠습니까? (y/n): " confirm

if [[ "$confirm" != "y" ]]; then
    echo "취소되었습니다."
    exit 0
fi

# 3. .env 업데이트
if [ -f .env ]; then
    sed -i "s/^VERSION_APP=.*/VERSION_APP=$OLD_APP/" .env
    sed -i "s/^VERSION_UI=.*/VERSION_UI=$OLD_UI/" .env
    echo -e "${GREEN}📝 .env 파일이 이전 버전으로 복구되었습니다.${NC}"
else
    echo -e "${RED}❌ 오류: .env 파일이 없습니다.${NC}"
    exit 1
fi

# 4. 함대 재기동
echo -e "${YELLOW}🚀 이전 버전으로 함대를 재기동합니다...${NC}"
./deploy.sh all

echo -e "${GREEN}✅ 롤백이 성공적으로 완료되었습니다!${NC}"
