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
            // [v4.3.16] Universal Physical Unification: Case-Insensitive Hardening
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 데이터베이스 기본값부터 유니코드로 변경
                SET @sql = CONCAT('ALTER DATABASE ', @dbname, ' CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. 인덱스 제거 방어 (실제 테이블명 조회)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @idx_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @target AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_keyword');
                    IF @idx_exist > 0 THEN SET @sql = CONCAT('DROP INDEX IX_unifiedcommands_chzzkuid_keyword ON ', @target); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                    SET @idx_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @target AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_TargetId');
                    IF @idx_exist > 0 THEN SET @sql = CONCAT('DROP INDEX IX_unifiedcommands_chzzkuid_TargetId ON ', @target); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
                    
                    -- 물리적 테이블 변환 (Collation 통일)
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                SET @profiles = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @profiles IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @profiles, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- 3. 신규 컬럼 및 데이터 매핑 (항상 소문자 기준으로 시도)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' AND COLUMN_NAME = 'MasterCommandFeatureId');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'unifiedcommands' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL; END IF;

                -- 데이터 매핑
                UPDATE unifiedcommands u JOIN streamerprofiles p ON LOWER(u.chzzkuid) = LOWER(p.ChzzkUid) SET u.StreamerProfileId = p.Id;
            ");

            // 이후 로직 유지...
            migrationBuilder.AlterColumn<int>(name: "MasterCommandFeatureId", table: "unifiedcommands", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "unifiedcommands", nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
