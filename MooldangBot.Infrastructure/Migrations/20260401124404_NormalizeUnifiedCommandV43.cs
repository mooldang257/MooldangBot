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
            // [v4.3.11] Universal String Unification: Proactively convert all tables to unicode_ci
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                
                -- 1. 데이터베이스 및 모든 테이블의 정렬 규칙을 강제로 통일 (utf8mb4_unicode_ci)
                SET @sql = CONCAT('ALTER DATABASE ', @dbname, ' CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 기존의 주요 테이블들을 사전에 통합 (충돌 방지)
                ALTER TABLE streamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE unifiedcommands CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                
                -- 2. 신규 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'MasterCommandFeatureId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 3. 데이터 매핑 (물리적 형식이 통일되었으므로 COLLATE 없이 순수 JOIN)
                SET @has_old = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                IF @has_old > 0 THEN
                    UPDATE unifiedcommands u JOIN streamerprofiles p ON u.chzzkuid = p.ChzzkUid SET u.StreamerProfileId = p.Id;
                    
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 1 WHERE LOWER(FeatureType) = 'reply';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 2 WHERE LOWER(FeatureType) = 'notice';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 3 WHERE LOWER(FeatureType) = 'title';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 4 WHERE LOWER(FeatureType) = 'category';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 5 WHERE LOWER(FeatureType) = 'songlisttoggle';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 6 WHERE LOWER(FeatureType) = 'songrequest';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 7 WHERE LOWER(FeatureType) = 'omakase';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 8 WHERE LOWER(FeatureType) = 'roulette';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 9 WHERE LOWER(FeatureType) = 'chatpoint';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 10 WHERE LOWER(FeatureType) = 'systemresponse';
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 11 WHERE LOWER(FeatureType) = 'ai';
                END IF;

                -- 4. 정량화 및 컬럼 정리
                SET @has_data = (SELECT COUNT(*) FROM streamerprofiles);
                IF @has_data > 0 THEN
                    UPDATE unifiedcommands SET StreamerProfileId = (SELECT MIN(Id) FROM streamerprofiles) WHERE StreamerProfileId IS NULL;
                    UPDATE unifiedcommands SET MasterCommandFeatureId = 1 WHERE MasterCommandFeatureId IS NULL;
                END IF;

                SET @columns = (SELECT GROUP_CONCAT(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME IN ('Category', 'FeatureType', 'chzzkuid'));
                IF @columns IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE unifiedcommands ', (SELECT GROUP_CONCAT(CONCAT('DROP COLUMN ', COLUMN_NAME)) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME IN ('Category', 'FeatureType', 'chzzkuid')));
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                ALTER TABLE unifiedcommands MODIFY MasterCommandFeatureId int NOT NULL;
                ALTER TABLE unifiedcommands MODIFY StreamerProfileId int NOT NULL;

                -- 5. 인덱스/FK 최종 정리
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_MasterCommandFeatureId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_MasterCommandFeatureId ON unifiedcommands', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                CREATE INDEX IX_unifiedcommands_MasterCommandFeatureId ON unifiedcommands (MasterCommandFeatureId);

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_StreamerProfileId_keyword');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_StreamerProfileId_keyword ON unifiedcommands', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                CREATE UNIQUE INDEX IX_unifiedcommands_StreamerProfileId_keyword ON unifiedcommands (StreamerProfileId, keyword);

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_StreamerProfileId_TargetId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_StreamerProfileId_TargetId ON unifiedcommands', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                CREATE INDEX IX_unifiedcommands_StreamerProfileId_TargetId ON unifiedcommands (StreamerProfileId, TargetId);

                -- FK 설정
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND CONSTRAINT_NAME = 'FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE unifiedcommands DROP FOREIGN KEY FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                ALTER TABLE unifiedcommands ADD CONSTRAINT FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId FOREIGN KEY (MasterCommandFeatureId) REFERENCES master_commandfeatures (Id) ON DELETE RESTRICT;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND CONSTRAINT_NAME = 'FK_unifiedcommands_streamerprofiles_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE unifiedcommands DROP FOREIGN KEY FK_unifiedcommands_streamerprofiles_StreamerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                ALTER TABLE unifiedcommands ADD CONSTRAINT FK_unifiedcommands_streamerprofiles_StreamerProfileId FOREIGN KEY (StreamerProfileId) REFERENCES streamerprofiles (Id) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
