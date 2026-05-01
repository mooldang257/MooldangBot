#!/bin/bash

# ---------------------------------------------------------
# 💾 [MooldangBot] MariaDB 자동 백업 스크립트 v1.0
# ---------------------------------------------------------

BACKUP_DIR="./data/backups/db"
KEEP_DAYS=7
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}💾 DB 백업 작업을 시작합니다...${NC}"

# 1. 백업 폴더 생성
mkdir -p "$BACKUP_DIR"

# 2. .env에서 설정 읽기
if [ -f .env ]; then
    # 필요한 변수만 추출하여 설정
    DB_ROOT_PW=$(grep "^MARIADB_ROOT_PASSWORD=" .env | cut -d'=' -f2)
fi

if [ -z "$DB_ROOT_PW" ]; then
    echo -e "${RED}❌ 오류: .env에서 MARIADB_ROOT_PASSWORD를 찾을 수 없습니다.${NC}"
    exit 1
fi

# 3. 덤프 실행 (컨테이너 내부의 mariadb-dump 활용)
FILENAME="mooldang_db_$TIMESTAMP.sql.gz"
echo -e "${YELLOW}📥 데이터 추출 중: $FILENAME${NC}"

docker exec mooldang-prod-db mariadb-dump -u root -p"$DB_ROOT_PW" --all-databases 2>/dev/null | gzip > "$BACKUP_DIR/$FILENAME"

if [ $? -eq 0 ] && [ -s "$BACKUP_DIR/$FILENAME" ]; then
    echo -e "${GREEN}✅ 백업 성공: $BACKUP_DIR/$FILENAME${NC}"
else
    echo -e "${RED}❌ 백업 실패 또는 파일이 비어있습니다!${NC}"
    rm -f "$BACKUP_DIR/$FILENAME"
    exit 1
fi

# 4. 오래된 백업 삭제 (KEEP_DAYS 기준)
echo -e "${YELLOW}🧹 $KEEP_DAYS일 지난 오래된 백업 정리 중...${NC}"
find "$BACKUP_DIR" -name "*.sql.gz" -type f -mtime +$KEEP_DAYS -delete

echo -e "${GREEN}✨ 모든 작업이 완료되었습니다.${NC}"
