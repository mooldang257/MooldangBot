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
            // [v4.3.12] Linux Case-Sensitivity Fix: Ensure all column names match physical schema (lowercase)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                
                -- 1. 데이터베이스 및 모든 테이블의 정렬 규칙을 강제로 통일
                SET @sql = CONCAT('ALTER DATABASE ', @dbname, ' CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 기존 테이블 변환 (리눅스 대소문자 고려: InitialCreate와 동일하게 소문자 사용 가능성 높음)
                ALTER TABLE streamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE unifiedcommands CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                
                -- 2. 신규 컬럼 추가 방어 (컬럼명은 C# 속성명에 맞춰 PascalCase로 생성됨)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'MasterCommandFeatureId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 3. 데이터 매핑 (chzzkuid는 InitialCreate 기준 소문자)
                SET @has_old = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                IF @has_old > 0 THEN
                    -- InitialCreate에서 생성된 컬럼명은 소문자 'chzzkuid'임
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

                -- 삭제 대상 컬럼명 소문자 체크 (InitialCreate 기준)
                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'chzzkuid');
                IF @col_exist > 0 THEN
                    ALTER TABLE unifiedcommands DROP COLUMN chzzkuid;
                END IF;
                
                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                IF @col_exist > 0 THEN
                    ALTER TABLE unifiedcommands DROP COLUMN FeatureType;
                END IF;

                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'Category');
                IF @col_exist > 0 THEN
                    ALTER TABLE unifiedcommands DROP COLUMN Category;
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
                -- keyword 또한 소문자임
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
