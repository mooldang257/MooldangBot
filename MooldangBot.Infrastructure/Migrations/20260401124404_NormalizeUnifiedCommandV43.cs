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
            // [v4.3.15] Physical Unification Engine: Safe heavy conversion
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 데이터베이스 기본값부터 유니코드로 변경 (향후 생성될 컬럼 대비)
                SET @sql = CONCAT('ALTER DATABASE ', @dbname, ' CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. 인덱스 충돌 방지를 위해 기존 인덱스 먼저 제거 (chzzkuid 관련)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_keyword');
                IF @exist > 0 THEN DROP INDEX IX_unifiedcommands_chzzkuid_keyword ON unifiedcommands; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_TargetId');
                IF @exist > 0 THEN DROP INDEX IX_unifiedcommands_chzzkuid_TargetId ON unifiedcommands; END IF;

                -- 3. 물리적 테이블 변환 (Collation 통일의 핵심)
                ALTER TABLE streamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE unifiedcommands CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

                -- 4. 신규 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'MasterCommandFeatureId');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL; END IF;

                -- 5. 데이터 매핑 (물리적 형식이 통일되었으므로 안전함)
                SET @has_old = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'chzzkuid');
                IF @has_old > 0 THEN
                    UPDATE unifiedcommands u JOIN streamerprofiles p ON u.chzzkuid = p.ChzzkUid SET u.StreamerProfileId = p.Id;
                    
                    SET @has_ft = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                    IF @has_ft > 0 THEN
                        UPDATE unifiedcommands SET MasterCommandFeatureId = 1 WHERE LOWER(FeatureType) = 'reply';
                        -- ... (나머지 매핑 생략 가능하나 안정성을 위해 유지)
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
                END IF;

                -- 6. 구 컬럼 삭제
                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'chzzkuid');
                IF @col_exist > 0 THEN ALTER TABLE unifiedcommands DROP COLUMN chzzkuid; END IF;
                
                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                IF @col_exist > 0 THEN ALTER TABLE unifiedcommands DROP COLUMN FeatureType; END IF;

                SET @col_exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'Category');
                IF @col_exist > 0 THEN ALTER TABLE unifiedcommands DROP COLUMN Category; END IF;

                ALTER TABLE unifiedcommands MODIFY MasterCommandFeatureId int NOT NULL;
                ALTER TABLE unifiedcommands MODIFY StreamerProfileId int NOT NULL;

                -- 7. 신규 인덱스 생성
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_MasterCommandFeatureId');
                IF @exist > 0 THEN DROP INDEX IX_unifiedcommands_MasterCommandFeatureId ON unifiedcommands; END IF;
                CREATE INDEX IX_unifiedcommands_MasterCommandFeatureId ON unifiedcommands (MasterCommandFeatureId);

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_StreamerProfileId_keyword');
                IF @exist > 0 THEN DROP INDEX IX_unifiedcommands_StreamerProfileId_keyword ON unifiedcommands; END IF;
                CREATE UNIQUE INDEX IX_unifiedcommands_StreamerProfileId_keyword ON unifiedcommands (StreamerProfileId, keyword);

                -- FK 생성
                ALTER TABLE unifiedcommands ADD CONSTRAINT FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId FOREIGN KEY (MasterCommandFeatureId) REFERENCES master_commandfeatures (Id) ON DELETE RESTRICT;
                ALTER TABLE unifiedcommands ADD CONSTRAINT FK_unifiedcommands_streamerprofiles_StreamerProfileId FOREIGN KEY (StreamerProfileId) REFERENCES streamerprofiles (Id) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
