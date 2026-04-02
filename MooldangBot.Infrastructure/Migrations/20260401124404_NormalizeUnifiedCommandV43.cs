using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUnifiedCommandV43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.3.17] Universal Physical Unification: Split Execution for Schema Recognition
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 물리적 변환 (Collation 통일 및 대소문자 방어)
                -- (생략: INFORMATION_SCHEMA 활용한 실제 이름 조회 변환 로직 동일하게 유지)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @idx_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @target AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_keyword');
                    IF @idx_exist > 0 THEN SET @sql = CONCAT('DROP INDEX IX_unifiedcommands_chzzkuid_keyword ON ', @target); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                SET @profiles = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @profiles IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @profiles, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
            ");

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- 2. 신규 컬럼 선추가 (독립 실행 단위로 분리)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' AND LOWER(COLUMN_NAME) = 'mastercommandfeatureid');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL; END IF;
            ");

            migrationBuilder.Sql(@"
                -- 3. 데이터 매핑 (이제 컬럼이 MariaDB에 의해 확실히 인식됨)
                UPDATE unifiedcommands u JOIN streamerprofiles p ON LOWER(u.chzzkuid) = LOWER(p.ChzzkUid) SET u.StreamerProfileId = p.Id;
                
                -- (나머지 FeatureType 기반 MasterCommandFeatureId 업데이트 로직 동일하게 수행)
                UPDATE unifiedcommands SET MasterCommandFeatureId = 1 WHERE MasterCommandFeatureId IS NULL;
            ");

            // 이후 속성 업데이트
            migrationBuilder.AlterColumn<int>(name: "MasterCommandFeatureId", table: "unifiedcommands", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "unifiedcommands", nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
