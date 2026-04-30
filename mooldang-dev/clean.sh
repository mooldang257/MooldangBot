#!/bin/bash

# 🔱 [오시리스의 정화]: 사용하지 않는 이미지 자산을 정리하는 스크립트 (v19.0)

# 1. 환경 변수 로드
if [ -f .env ]; then
    # 주석 제외하고 환경 변수 익스포트
    export $(grep -v '^#' .env | xargs)
else
    echo "❌ .env 파일을 찾을 수 없습니다."
    exit 1
fi

# 2. DB 연결 정보 및 경로 설정
DB_HOST="127.0.0.1"
UPLOADS_DIR="./data/dev/uploads" # 개발 환경 업로드 경로

# DB 접속 명령어 (사용자 환경에 따라 mysql 또는 mariadb 사용)
MYSQL_CMD="mysql -h $DB_HOST -u $MARIADB_USER -p$MARIADB_PASSWORD"

echo "🔍 데이터베이스에서 사용 중인 이미지 목록을 수집하는 중..."

# [v19.0] 모든 테이블에서 참조 중인 파일 경로 수집
# /api/storage/ 접두어를 제거하여 실제 파일 경로와 대조합니다.
USED_FILES=$( (
    $MYSQL_CMD $MARIADB_DATABASE -N -e "
        SELECT DISTINCT thumbnail_url FROM func_song_books WHERE thumbnail_url IS NOT NULL AND thumbnail_url != '';
        SELECT DISTINCT thumbnail_url FROM func_song_master_library WHERE thumbnail_url IS NOT NULL AND thumbnail_url != '';
        SELECT DISTINCT thumbnail_url FROM func_song_master_staging WHERE thumbnail_url IS NOT NULL AND thumbnail_url != '';
    " 2>/dev/null
    
    $MYSQL_CMD MooldangBot_Common -N -e "
        SELECT DISTINCT local_path FROM thumbnails WHERE local_path IS NOT NULL AND local_path != '';
    " 2>/dev/null
) | sed 's|^/api/storage/||' | sort -u)

if [ -z "$USED_FILES" ]; then
    echo "⚠️ 사용 중인 파일 목록이 비어있거나 DB 연결에 실패했습니다."
    echo "연결 정보: Host=$DB_HOST, User=$MARIADB_USER, DB=$MARIADB_DATABASE"
    read -p "계속 진행하시겠습니까? (파일이 모두 삭제될 수 있습니다) [y/N]: " CONFIRM
    if [[ ! $CONFIRM =~ ^[Yy]$ ]]; then
        echo "중단되었습니다."
        exit 1
    fi
fi

if [ ! -d "$UPLOADS_DIR" ]; then
    echo "❌ 업로드 디렉토리를 찾을 수 없습니다: $UPLOADS_DIR"
    exit 1
fi

echo "🚀 사용 중이지 않은 이미지 정리 시작..."
echo "------------------------------------------------"

DELETE_COUNT=0
KEEP_COUNT=0

# 실제 파일 시스템 순회
while IFS= read -r -d '' FILE; do
    # 상대 경로 추출 (예: library/songs/abc.webp)
    REL_PATH=${FILE#$UPLOADS_DIR/}
    
    # 목록에 있는지 확인 (정확히 일치하는 행 찾기)
    if echo "$USED_FILES" | grep -qFx "$REL_PATH"; then
        KEEP_COUNT=$((KEEP_COUNT + 1))
    else
        echo "🗑️  삭제: $REL_PATH"
        rm "$FILE"
        DELETE_COUNT=$((DELETE_COUNT + 1))
    fi
done < <(find "$UPLOADS_DIR" -type f -print0)

echo "------------------------------------------------"
echo "✅ 정리 완료!"
echo "📦 보존: $KEEP_COUNT 개"
echo "🗑️  삭제: $DELETE_COUNT 개"
