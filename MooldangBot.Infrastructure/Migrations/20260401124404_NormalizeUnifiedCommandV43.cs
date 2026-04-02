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
                // [v4.3.7] Ultimate Repair: Data Mapping + Idempotent
                migrationBuilder.Sql(@"
                    SET @dbname = DATABASE();

                    -- 1. 신규 컬럼 추가 (매핑을 위해 먼저 실행)
                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'MasterCommandFeatureId');
                    SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD MasterCommandFeatureId int NULL', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'StreamerProfileId');
                    SET @sql = IF(@exist = 0, 'ALTER TABLE unifiedcommands ADD StreamerProfileId int NULL', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    -- 2. [데이터 매핑] 기존 컬럼이 있는 경우에만 실행
                    SET @has_old = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME = 'FeatureType');
                    IF @has_old > 0 THEN
                        -- 2-1. StreamerProfileId 매핑
                        UPDATE unifiedcommands u
                        JOIN streamerprofiles p ON LOWER(u.chzzkuid) = LOWER(p.ChzzkUid)
                        SET u.StreamerProfileId = p.Id;

                        -- 2-2. MasterCommandFeatureId 매핑 (Seed Data 기반)
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
                        
                        -- 매핑되지 않은 잔여 데이터 처리 (기본값 'Reply'=1)
                        UPDATE unifiedcommands SET MasterCommandFeatureId = 1 WHERE MasterCommandFeatureId IS NULL;
                        UPDATE unifiedcommands SET StreamerProfileId = (SELECT MIN(Id) FROM streamerprofiles) WHERE StreamerProfileId IS NULL;
                    END IF;

                    -- 3. [정제] 중복 데이터 제거 (Unique Index 생성 대비)
                    DELETE t1 FROM unifiedcommands t1
                    INNER JOIN (
                        SELECT MIN(Id) as MinId, StreamerProfileId, keyword
                        FROM unifiedcommands
                        GROUP BY StreamerProfileId, keyword
                        HAVING COUNT(*) > 1
                    ) t2 ON t1.StreamerProfileId = t2.StreamerProfileId AND t1.keyword = t2.keyword
                    WHERE t1.Id > t2.MinId;

                    -- 4. [정리] 기존 인덱스/컬럼 제거
                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_keyword');
                    SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_chzzkuid_keyword ON unifiedcommands', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_chzzkuid_TargetId');
                    SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_chzzkuid_TargetId ON unifiedcommands', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @columns = (SELECT GROUP_CONCAT(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME IN ('Category', 'FeatureType', 'chzzkuid'));
                    IF @columns IS NOT NULL THEN
                        SET @sql = CONCAT('ALTER TABLE unifiedcommands ', (SELECT GROUP_CONCAT(CONCAT('DROP COLUMN ', COLUMN_NAME)) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND COLUMN_NAME IN ('Category', 'FeatureType', 'chzzkuid')));
                        PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                    END IF;

                    -- 5. [제약조건] NOT NULL 설정 및 기존 인덱스/FK 제거 (Re-run 대비)
                    ALTER TABLE unifiedcommands MODIFY MasterCommandFeatureId int NOT NULL;
                    ALTER TABLE unifiedcommands MODIFY StreamerProfileId int NOT NULL;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND CONSTRAINT_NAME = 'FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId');
                    SET @sql = IF(@exist > 0, 'ALTER TABLE unifiedcommands DROP FOREIGN KEY FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND CONSTRAINT_NAME = 'FK_unifiedcommands_streamerprofiles_StreamerProfileId');
                    SET @sql = IF(@exist > 0, 'ALTER TABLE unifiedcommands DROP FOREIGN KEY FK_unifiedcommands_streamerprofiles_StreamerProfileId', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_MasterCommandFeatureId');
                    SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_MasterCommandFeatureId ON unifiedcommands', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_StreamerProfileId_keyword');
                    SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_StreamerProfileId_keyword ON unifiedcommands', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                    SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'unifiedcommands' AND INDEX_NAME = 'IX_unifiedcommands_StreamerProfileId_TargetId');
                    SET @sql = IF(@exist > 0, 'DROP INDEX IX_unifiedcommands_StreamerProfileId_TargetId ON unifiedcommands', 'SELECT 1');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                ");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_MasterCommandFeatureId",
                table: "unifiedcommands",
                column: "MasterCommandFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_StreamerProfileId_keyword",
                table: "unifiedcommands",
                columns: new[] { "StreamerProfileId", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_StreamerProfileId_TargetId",
                table: "unifiedcommands",
                columns: new[] { "StreamerProfileId", "TargetId" });

            migrationBuilder.AddForeignKey(
                name: "FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId",
                table: "unifiedcommands",
                column: "MasterCommandFeatureId",
                principalTable: "master_commandfeatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unifiedcommands_streamerprofiles_StreamerProfileId",
                table: "unifiedcommands",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropForeignKey(
                name: "FK_unifiedcommands_streamerprofiles_StreamerProfileId",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_StreamerProfileId_keyword",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_StreamerProfileId_TargetId",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "unifiedcommands");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "unifiedcommands",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FeatureType",
                table: "unifiedcommands",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "chzzkuid",
                table: "unifiedcommands",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_keyword",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_TargetId",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "TargetId" });
        }
    }
}
