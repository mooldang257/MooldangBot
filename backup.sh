#!/bin/bash

# ---------------------------------------------------------
# 💾 [물댕봇] MariaDB 자동 백업 스크립트 (backup.sh)
# ---------------------------------------------------------

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

# 1. .env 파일의 환경 변수 로드
if [ ! -f .env ]; then
    echo -e "${RED}❌ [오류] 백업에 필요한 .env 파일이 존재하지 않습니다.${NC}"
    exit 1
fi

set -a
source .env
set +a

# 2. 백업 파일이 저장될 경로 준비 (기본값: 로컬 DB_DATA_PATH 안의 backups 폴더)
BACKUP_DIR="${DB_DATA_PATH:-./data/mariadb}/backups"
mkdir -p "$BACKUP_DIR"

# 3. 날짜 형식 및 파일 이름 지정
CURRENT_DATE=$(date +%Y-%m-%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/${MARIADB_DATABASE}_$CURRENT_DATE.sql"

echo -e "${GREEN}📦 [${MARIADB_DATABASE}] 데이터베이스 백업을 시작합니다...${NC}"

# 4. 백업 명령어 실행 (비밀번호 노출 경고 방지를 위해 컨테이너 내 환경변수 전달방식 사용)
docker-compose exec -T -e MYSQL_PWD="${MARIADB_ROOT_PASSWORD}" db mariadb-dump -u root "${MARIADB_DATABASE}" > "$BACKUP_FILE"

# 5. 결과 검증
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ 백업이 성공적으로 완료되었습니다!${NC}"
    echo "경로: $BACKUP_FILE"
    
    # 선택 사항: 오래된 백업(7일 초과) 자동 삭제 기능
    # find "$BACKUP_DIR" -type f -name "*.sql" -mtime +7 -exec rm {} \;
else
    echo -e "${RED}❌ 백업 과정에서 오류가 발생했습니다.${NC}"
    # 실패한 빈 파일은 삭제
    rm -f "$BACKUP_FILE"
    exit 1
fi
