#!/bin/bash

# ---------------------------------------------------------
# 💾 [Project Osiris] 통합 데이터 백업 스크립트 (backup.sh v2.1)
# ---------------------------------------------------------

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 1. .env 파일 안전 로드 (Windows CRLF 호환 처리)
if [ ! -f .env ]; then
    echo -e "${RED}❌ [오류] 백업에 필요한 .env 파일이 존재하지 않습니다.${NC}"
    exit 1
fi

# [물멍의 팁]: \r 문자를 제거하여 윈도우 환경에서 생성된 .env 호환성을 보장합니다.
set -a
eval "$(grep -v '^#' .env | sed 's/\r$//' | xargs -d '\n' -n 1 echo export)"
set +a

# 2. 백업 디렉토리 설정
BACKUP_DIR="${DB_DATA_PATH:-./data/mariadb}/backups"
mkdir -p "$BACKUP_DIR"
CURRENT_DATE=$(date +%Y-%m-%d_%H%M%S)

echo -e "${GREEN}📦 [오시리스의 기록관] 함선 데이터 백업을 시작합니다...${NC}"

# 3. MariaDB 백업 (DB 사수)
DB_FILE="$BACKUP_DIR/${MARIADB_DATABASE}_$CURRENT_DATE.sql"
docker-compose exec -T -e MYSQL_PWD="${MARIADB_ROOT_PASSWORD}" db mariadb-dump -u root "${MARIADB_DATABASE}" > "$DB_FILE"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ DB 백업 완료: $(basename $DB_FILE)${NC}"
else
    echo -e "${RED}❌ DB 백업 실패!${NC}"
    rm -f "$DB_FILE"
    exit 1
fi

# 4. [Phase 18] 익산 보험 파일 백업 (Point Durability 사수)
# [v18.1] 이제 전용 볼륨 경로(./data/app_data/)에서 직접 파일을 수집합니다.
QUEUE_SOURCE="./data/app_data/temp_point_queue.json"
QUEUE_DEST="$BACKUP_DIR/temp_point_queue_$CURRENT_DATE.json"

echo -e "${YELLOW}🛡️ [익산 보험] 포인트 유실 방지 큐 확인 중...${NC}"

if [ -f "$QUEUE_SOURCE" ]; then
    cp "$QUEUE_SOURCE" "$QUEUE_DEST"
    echo -e "${GREEN}✅ 포인트 복구 큐 백업 완료! (익산 보험 적용됨)${NC}"
else
    echo -e "   (현재 대피 중인 포인트 데이터가 없습니다.)"
fi

# 5. 오래된 백업 정리 (7일 초과분)
find "$BACKUP_DIR" -type f \( -name "*.sql" -o -name "*.json" \) -mtime +7 -exec rm {} \;
echo -e "${GREEN}✨ 모든 백업 절차가 완료되었습니다!${NC}"
