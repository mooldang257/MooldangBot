#!/bin/bash

# ---------------------------------------------------------
# 🔄 [MooldangBot] DB 스키마 동기화 스크립트 v1.0
# 개발(dev) DB의 스키마/인덱스 정보를 추출하여 운영(bot) DB에 적용합니다.
# EF 마이그레이션이 처리하지 못하는 VECTOR 컬럼, FULLTEXT 인덱스 등을 동기화합니다.
#
# 사용법:
#   ./sync-schema.sh              # 기본: dry-run (차이점만 표시)
#   ./sync-schema.sh --apply      # 실제 운영 DB에 적용
#   ./sync-schema.sh --dump       # 개발 스키마를 파일로 덤프만
# ---------------------------------------------------------

set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
BLUE='\033[0;34m'
BOLD='\033[1m'
NC='\033[0m'

# ─────────────────── 설정 ───────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

DEV_CONTAINER="mooldang-dev-db"
PROD_CONTAINER="mooldang-bot-db"
DB_NAME="MooldangBot"
DB_USER="root"

# .env에서 비밀번호 로드
DEV_ENV="$PROJECT_ROOT/mooldang-dev/.env"
PROD_ENV="$PROJECT_ROOT/bot/.env"

load_password() {
    local env_file=$1
    if [ -f "$env_file" ]; then
        grep "^MARIADB_ROOT_PASSWORD=" "$env_file" | cut -d'=' -f2
    else
        echo ""
    fi
}

DEV_PW=$(load_password "$DEV_ENV")
PROD_PW=$(load_password "$PROD_ENV")

if [ -z "$DEV_PW" ] || [ -z "$PROD_PW" ]; then
    echo -e "${RED}❌ .env 파일에서 DB 비밀번호를 읽을 수 없습니다.${NC}"
    exit 1
fi

# 임시 디렉토리
WORK_DIR="$SCRIPT_DIR/.schema_sync_tmp"
mkdir -p "$WORK_DIR"

# ─────────────────── 유틸리티 ───────────────────

# 컨테이너에서 SQL 실행 (탭 구분 텍스트 반환)
run_sql() {
    local container=$1
    local password=$2
    local sql=$3
    docker exec "$container" mariadb -u"$DB_USER" -p"$password" "$DB_NAME" \
        --batch --skip-column-names -e "$sql" 2>/dev/null
}

# 컨테이너 상태 확인
check_container() {
    local name=$1
    if ! docker ps --format '{{.Names}}' | grep -q "^${name}$"; then
        echo -e "${RED}❌ 컨테이너 '$name'이 실행 중이 아닙니다.${NC}"
        return 1
    fi
    return 0
}

# ─────────────────── 1. 스키마 추출 ───────────────────

extract_schema() {
    local container=$1
    local password=$2
    local prefix=$3

    echo -e "${CYAN}📊 [$prefix] $container 스키마 추출 중...${NC}"

    # 1-1. 테이블 목록
    run_sql "$container" "$password" "
        SELECT TABLE_NAME 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_SCHEMA='$DB_NAME' AND TABLE_TYPE='BASE TABLE'
        ORDER BY TABLE_NAME;
    " > "$WORK_DIR/${prefix}_tables.txt"

    # 1-2. 컬럼 정보 (이름, 타입, NULL 가능 여부, 기본값, EXTRA)
    run_sql "$container" "$password" "
        SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, 
               IFNULL(COLUMN_DEFAULT, '__NULL__'), EXTRA
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA='$DB_NAME'
        ORDER BY TABLE_NAME, ORDINAL_POSITION;
    " > "$WORK_DIR/${prefix}_columns.txt"

    # 1-3. 인덱스 정보 (FULLTEXT 포함)
    run_sql "$container" "$password" "
        SELECT TABLE_NAME, INDEX_NAME, 
               GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ','),
               NON_UNIQUE, INDEX_TYPE
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA='$DB_NAME'
        GROUP BY TABLE_NAME, INDEX_NAME, NON_UNIQUE, INDEX_TYPE
        ORDER BY TABLE_NAME, INDEX_NAME;
    " > "$WORK_DIR/${prefix}_indexes.txt"

    # 1-4. CREATE TABLE 문 (상세 참조용)
    while IFS= read -r tbl; do
        run_sql "$container" "$password" "SHOW CREATE TABLE \`$tbl\`;" \
            > "$WORK_DIR/${prefix}_create_${tbl}.txt" 2>/dev/null || true
    done < "$WORK_DIR/${prefix}_tables.txt"

    echo -e "${GREEN}  ✅ [$prefix] 추출 완료 ($(wc -l < "$WORK_DIR/${prefix}_tables.txt") 테이블)${NC}"
}

# ─────────────────── 2. 스키마 비교 & SQL 생성 ───────────────────

generate_sync_sql() {
    local output_file="$WORK_DIR/sync_migration.sql"
    > "$output_file"
    
    local diff_count=0

    echo -e "\n${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BOLD}  🔍 스키마 차이 분석 (개발 → 운영)${NC}"
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"

    # --- 2-1. 테이블 차이 ---
    echo -e "${BLUE}[1/4] 테이블 비교...${NC}"
    local missing_tables=()
    while IFS= read -r dev_table; do
        if ! grep -qx "$dev_table" "$WORK_DIR/prod_tables.txt"; then
            missing_tables+=("$dev_table")
            echo -e "  ${RED}▸ 누락 테이블: $dev_table${NC}"
            
            # CREATE TABLE 문 복사
            if [ -f "$WORK_DIR/dev_create_${dev_table}.txt" ]; then
                # SHOW CREATE TABLE 출력에서 CREATE 문 추출
                local create_stmt
                create_stmt=$(cut -f2 "$WORK_DIR/dev_create_${dev_table}.txt" 2>/dev/null || true)
                if [ -n "$create_stmt" ]; then
                    echo "-- [누락 테이블] $dev_table" >> "$output_file"
                    echo "$create_stmt;" >> "$output_file"
                    echo "" >> "$output_file"
                    ((diff_count++))
                fi
            fi
        fi
    done < "$WORK_DIR/dev_tables.txt"
    
    if [ ${#missing_tables[@]} -eq 0 ]; then
        echo -e "  ${GREEN}✅ 모든 테이블 일치${NC}"
    fi

    # --- 2-2. 컬럼 차이 (타입 불일치 포함 - VECTOR 컬럼 핵심) ---
    echo -e "\n${BLUE}[2/4] 컬럼 비교 (타입 포함)...${NC}"
    local col_diff_found=false
    
    while IFS=$'\t' read -r tbl col coltype nullable default extra; do
        # 운영에 해당 테이블이 없으면 스킵 (위에서 이미 처리)
        if ! grep -qx "$tbl" "$WORK_DIR/prod_tables.txt"; then
            continue
        fi
        
        # 운영에서 같은 테이블·컬럼 검색
        local prod_line
        prod_line=$(grep -P "^${tbl}\t${col}\t" "$WORK_DIR/prod_columns.txt" 2>/dev/null || true)
        
        if [ -z "$prod_line" ]; then
            # 컬럼이 운영에 없음 → ADD COLUMN
            echo -e "  ${RED}▸ 누락 컬럼: ${tbl}.${col} (${coltype})${NC}"
            
            local null_clause=""
            if [ "$nullable" = "NO" ]; then
                null_clause="NOT NULL"
            else
                null_clause="NULL"
            fi
            
            local default_clause=""
            if [ "$default" != "__NULL__" ] && [ -n "$default" ]; then
                default_clause="DEFAULT $default"
            fi
            
            local extra_clause=""
            if [ -n "$extra" ] && [ "$extra" != "" ]; then
                extra_clause="$extra"
            fi

            echo "-- [누락 컬럼] ${tbl}.${col}" >> "$output_file"
            echo "ALTER TABLE \`$tbl\` ADD COLUMN \`$col\` $coltype $null_clause $default_clause $extra_clause;" >> "$output_file"
            echo "" >> "$output_file"
            col_diff_found=true
            ((diff_count++))
        else
            # 컬럼이 있으나 타입이 다름 → MODIFY COLUMN (특히 VECTOR 변환)
            local prod_coltype
            prod_coltype=$(echo "$prod_line" | cut -f3)
            
            if [ "$coltype" != "$prod_coltype" ]; then
                echo -e "  ${YELLOW}▸ 타입 불일치: ${tbl}.${col} [운영: ${prod_coltype}] → [개발: ${coltype}]${NC}"
                
                local null_clause=""
                if [ "$nullable" = "NO" ]; then
                    null_clause="NOT NULL"
                else
                    null_clause="NULL"
                fi

                echo "-- [타입 변경] ${tbl}.${col}: $prod_coltype → $coltype" >> "$output_file"
                echo "ALTER TABLE \`$tbl\` MODIFY COLUMN \`$col\` $coltype $null_clause;" >> "$output_file"
                echo "" >> "$output_file"
                col_diff_found=true
                ((diff_count++))
            fi
        fi
    done < "$WORK_DIR/dev_columns.txt"
    
    if [ "$col_diff_found" = false ]; then
        echo -e "  ${GREEN}✅ 모든 컬럼 일치${NC}"
    fi

    # --- 2-3. 일반 인덱스 & FULLTEXT 인덱스 차이 ---
    echo -e "\n${BLUE}[3/4] 인덱스 비교 (FULLTEXT/VECTOR 포함)...${NC}"
    local idx_diff_found=false
    
    while IFS=$'\t' read -r tbl idx_name columns non_unique idx_type; do
        # PRIMARY KEY는 스킵 (EF가 관리)
        if [ "$idx_name" = "PRIMARY" ]; then
            continue
        fi
        
        # 운영에 해당 테이블이 없으면 스킵
        if ! grep -qx "$tbl" "$WORK_DIR/prod_tables.txt"; then
            continue
        fi
        
        # 운영에서 같은 인덱스 검색
        local prod_idx
        prod_idx=$(grep -P "^${tbl}\t${idx_name}\t" "$WORK_DIR/prod_indexes.txt" 2>/dev/null || true)
        
        if [ -z "$prod_idx" ]; then
            echo -e "  ${RED}▸ 누락 인덱스: ${tbl}.${idx_name} (${idx_type}, columns: ${columns})${NC}"
            
            echo "-- [누락 인덱스] ${tbl}.${idx_name}" >> "$output_file"
            
            # 인덱스 타입별 CREATE 문 생성
            local col_list
            col_list=$(echo "$columns" | sed 's/,/`,`/g')
            
            if [ "$idx_type" = "VECTOR" ]; then
                # VECTOR 인덱스는 MariaDB 전용 문법 사용
                echo "ALTER TABLE \`$tbl\` ADD VECTOR INDEX \`$idx_name\` (\`$col_list\`);" >> "$output_file"
            elif [ "$idx_type" = "FULLTEXT" ]; then
                echo "CREATE FULLTEXT INDEX IF NOT EXISTS \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
            elif [ "$non_unique" = "0" ]; then
                echo "CREATE UNIQUE INDEX IF NOT EXISTS \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
            else
                echo "CREATE INDEX IF NOT EXISTS \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
            fi
            echo "" >> "$output_file"
            idx_diff_found=true
            ((diff_count++))
        else
            # 인덱스는 있지만 컬럼 구성이 다른지 확인
            local prod_columns
            prod_columns=$(echo "$prod_idx" | cut -f3)
            local prod_idx_type
            prod_idx_type=$(echo "$prod_idx" | cut -f5)
            
            if [ "$columns" != "$prod_columns" ] || [ "$idx_type" != "$prod_idx_type" ]; then
                echo -e "  ${YELLOW}▸ 인덱스 변경: ${tbl}.${idx_name}${NC}"
                echo -e "    운영: [${prod_idx_type}] (${prod_columns})"
                echo -e "    개발: [${idx_type}] (${columns})"
                
                local col_list
                col_list=$(echo "$columns" | sed 's/,/`,`/g')
                
                echo "-- [인덱스 변경] ${tbl}.${idx_name}: 재생성 필요" >> "$output_file"
                echo "DROP INDEX IF EXISTS \`$idx_name\` ON \`$tbl\`;" >> "$output_file"
                
                if [ "$idx_type" = "VECTOR" ]; then
                    echo "ALTER TABLE \`$tbl\` ADD VECTOR INDEX \`$idx_name\` (\`$col_list\`);" >> "$output_file"
                elif [ "$idx_type" = "FULLTEXT" ]; then
                    echo "CREATE FULLTEXT INDEX \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
                elif [ "$non_unique" = "0" ]; then
                    echo "CREATE UNIQUE INDEX \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
                else
                    echo "CREATE INDEX \`$idx_name\` ON \`$tbl\` (\`$col_list\`);" >> "$output_file"
                fi
                echo "" >> "$output_file"
                idx_diff_found=true
                ((diff_count++))
            fi
        fi
    done < "$WORK_DIR/dev_indexes.txt"
    
    if [ "$idx_diff_found" = false ]; then
        echo -e "  ${GREEN}✅ 모든 인덱스 일치${NC}"
    fi

    # --- 2-4. 운영에만 있고 개발에 없는 인덱스 (경고) ---
    echo -e "\n${BLUE}[4/4] 운영에만 존재하는 인덱스 확인...${NC}"
    local orphan_found=false
    
    while IFS=$'\t' read -r tbl idx_name columns non_unique idx_type; do
        if [ "$idx_name" = "PRIMARY" ]; then
            continue
        fi
        
        local dev_idx
        dev_idx=$(grep -P "^${tbl}\t${idx_name}\t" "$WORK_DIR/dev_indexes.txt" 2>/dev/null || true)
        
        if [ -z "$dev_idx" ]; then
            echo -e "  ${YELLOW}⚠ 운영 전용 인덱스: ${tbl}.${idx_name} (${idx_type})${NC}"
            orphan_found=true
        fi
    done < "$WORK_DIR/prod_indexes.txt"
    
    if [ "$orphan_found" = false ]; then
        echo -e "  ${GREEN}✅ 고아 인덱스 없음${NC}"
    fi

    echo ""
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    if [ "$diff_count" -eq 0 ]; then
        echo -e "${GREEN}✅ 개발과 운영 스키마가 완전히 일치합니다! 동기화 필요 없음.${NC}"
        rm -f "$output_file"
        return 1
    else
        echo -e "${YELLOW}📝 총 ${diff_count}개 차이 발견. 마이그레이션 SQL이 생성되었습니다:${NC}"
        echo -e "   ${CYAN}$output_file${NC}"
        echo ""
        echo -e "${BOLD}── 생성된 SQL ──${NC}"
        cat "$output_file"
        echo -e "${BOLD}────────────────${NC}"
        return 0
    fi
}

# ─────────────────── 3. 적용 ───────────────────

apply_migration() {
    local sql_file="$WORK_DIR/sync_migration.sql"
    
    if [ ! -f "$sql_file" ]; then
        echo -e "${RED}❌ 적용할 마이그레이션 SQL 파일이 없습니다.${NC}"
        return 1
    fi

    echo -e "\n${YELLOW}⚠️  위 SQL을 운영 DB ($PROD_CONTAINER)에 적용합니다.${NC}"
    echo -e "${RED}경고: 이 작업은 운영 DB의 스키마를 변경합니다!${NC}"
    read -p "계속하시겠습니까? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        echo -e "${YELLOW}ℹ️  적용이 취소되었습니다. SQL 파일은 보존됩니다.${NC}"
        return 0
    fi

    echo -e "${CYAN}🔧 운영 DB에 마이그레이션 적용 중...${NC}"
    
    # SQL을 컨테이너 안으로 복사 후 실행
    docker cp "$sql_file" "${PROD_CONTAINER}:/tmp/sync_migration.sql"
    
    local result
    result=$(docker exec "$PROD_CONTAINER" mariadb -u"$DB_USER" -p"$PROD_PW" "$DB_NAME" \
        --force -e "SOURCE /tmp/sync_migration.sql;" 2>&1) || true
    
    if [ -n "$result" ]; then
        echo -e "${YELLOW}실행 결과:${NC}"
        echo "$result"
    fi
    
    # 적용 후 재검증
    echo -e "\n${CYAN}🔄 적용 후 재검증 중...${NC}"
    extract_schema "$PROD_CONTAINER" "$PROD_PW" "prod_verify"
    
    # 간단 비교
    local remaining_diff=0
    while IFS=$'\t' read -r tbl col coltype _ _ _; do
        local prod_line
        prod_line=$(grep -P "^${tbl}\t${col}\t" "$WORK_DIR/prod_verify_columns.txt" 2>/dev/null || true)
        if [ -z "$prod_line" ]; then
            ((remaining_diff++))
        else
            local prod_coltype
            prod_coltype=$(echo "$prod_line" | cut -f3)
            if [ "$coltype" != "$prod_coltype" ]; then
                ((remaining_diff++))
            fi
        fi
    done < "$WORK_DIR/dev_columns.txt"
    
    if [ "$remaining_diff" -eq 0 ]; then
        echo -e "${GREEN}✅ 운영 DB 스키마 동기화 완료! 모든 차이가 해소되었습니다.${NC}"
    else
        echo -e "${YELLOW}⚠️  ${remaining_diff}개 항목이 아직 불일치합니다. 수동 확인이 필요합니다.${NC}"
    fi

    # 적용 로그 저장
    local log_file="$SCRIPT_DIR/schema_sync_$(date +%Y%m%d_%H%M%S).log"
    cp "$sql_file" "$log_file"
    echo -e "${CYAN}📄 적용된 SQL 로그: $log_file${NC}"
}

# ─────────────────── 4. 덤프 전용 모드 ───────────────────

dump_schemas() {
    local dump_dir="$PROJECT_ROOT/mooldang-dev"
    
    echo -e "${CYAN}📂 스키마 정보를 파일로 덤프합니다...${NC}"
    
    # 개발 DB 덤프
    run_sql "$DEV_CONTAINER" "$DEV_PW" "
        SELECT TABLE_NAME, COUNT(*) as col_count
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        GROUP BY TABLE_NAME ORDER BY TABLE_NAME;
    " > "$dump_dir/dev_schema.txt"
    echo -e "TABLE_NAME\tcol_count" | cat - "$dump_dir/dev_schema.txt" > /tmp/_schema && mv /tmp/_schema "$dump_dir/dev_schema.txt"
    
    run_sql "$DEV_CONTAINER" "$DEV_PW" "
        SELECT TABLE_NAME, COLUMN_NAME 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        ORDER BY TABLE_NAME, ORDINAL_POSITION;
    " > "$dump_dir/dev_columns.txt"
    echo -e "TABLE_NAME\tCOLUMN_NAME" | cat - "$dump_dir/dev_columns.txt" > /tmp/_cols && mv /tmp/_cols "$dump_dir/dev_columns.txt"

    run_sql "$DEV_CONTAINER" "$DEV_PW" "
        SELECT TABLE_NAME, INDEX_NAME, COLUMN_NAME, NON_UNIQUE
        FROM INFORMATION_SCHEMA.STATISTICS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;
    " > "$dump_dir/dev_indexes.txt"
    echo -e "TABLE_NAME\tINDEX_NAME\tCOLUMN_NAME\tNON_UNIQUE" | cat - "$dump_dir/dev_indexes.txt" > /tmp/_idx && mv /tmp/_idx "$dump_dir/dev_indexes.txt"

    # 운영 DB 덤프
    run_sql "$PROD_CONTAINER" "$PROD_PW" "
        SELECT TABLE_NAME, COUNT(*) as col_count
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        GROUP BY TABLE_NAME ORDER BY TABLE_NAME;
    " > "$dump_dir/prod_schema.txt"
    echo -e "TABLE_NAME\tcol_count" | cat - "$dump_dir/prod_schema.txt" > /tmp/_schema && mv /tmp/_schema "$dump_dir/prod_schema.txt"

    run_sql "$PROD_CONTAINER" "$PROD_PW" "
        SELECT TABLE_NAME, COLUMN_NAME 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        ORDER BY TABLE_NAME, ORDINAL_POSITION;
    " > "$dump_dir/prod_columns.txt"
    echo -e "TABLE_NAME\tCOLUMN_NAME" | cat - "$dump_dir/prod_columns.txt" > /tmp/_cols && mv /tmp/_cols "$dump_dir/prod_columns.txt"

    run_sql "$PROD_CONTAINER" "$PROD_PW" "
        SELECT TABLE_NAME, INDEX_NAME, COLUMN_NAME, NON_UNIQUE
        FROM INFORMATION_SCHEMA.STATISTICS 
        WHERE TABLE_SCHEMA='$DB_NAME'
        ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;
    " > "$dump_dir/prod_indexes.txt"
    echo -e "TABLE_NAME\tINDEX_NAME\tCOLUMN_NAME\tNON_UNIQUE" | cat - "$dump_dir/prod_indexes.txt" > /tmp/_idx && mv /tmp/_idx "$dump_dir/prod_indexes.txt"

    echo -e "${GREEN}✅ 스키마 덤프 완료:${NC}"
    echo -e "   $dump_dir/dev_schema.txt, dev_columns.txt, dev_indexes.txt"
    echo -e "   $dump_dir/prod_schema.txt, prod_columns.txt, prod_indexes.txt"
}

# ─────────────────── 메인 ───────────────────

main() {
    local mode="${1:---dry-run}"

    echo -e "${BOLD}🔄 [MooldangBot] DB 스키마 동기화 도구 v1.0${NC}"
    echo -e "   개발: $DEV_CONTAINER → 운영: $PROD_CONTAINER"
    echo -e "   모드: ${CYAN}${mode}${NC}"
    echo ""

    # 컨테이너 확인
    check_container "$DEV_CONTAINER" || exit 1
    check_container "$PROD_CONTAINER" || exit 1

    case "$mode" in
        --dump)
            dump_schemas
            ;;
        --dry-run)
            extract_schema "$DEV_CONTAINER" "$DEV_PW" "dev"
            extract_schema "$PROD_CONTAINER" "$PROD_PW" "prod"
            generate_sync_sql || true
            ;;
        --apply)
            extract_schema "$DEV_CONTAINER" "$DEV_PW" "dev"
            extract_schema "$PROD_CONTAINER" "$PROD_PW" "prod"
            if generate_sync_sql; then
                apply_migration
            fi
            ;;
        *)
            echo -e "${RED}사용법: $0 [--dry-run | --apply | --dump]${NC}"
            echo -e "  --dry-run  : 차이점만 표시 (기본값, 운영 DB 미변경)"
            echo -e "  --apply    : 차이점을 운영 DB에 적용"
            echo -e "  --dump     : 양쪽 스키마를 파일로 덤프"
            exit 1
            ;;
    esac

    # 정리
    rm -rf "$WORK_DIR"
}

main "$@"
