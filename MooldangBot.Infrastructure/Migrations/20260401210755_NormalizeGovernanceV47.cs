using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeGovernanceV47 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.7.11] Universal Clean Restoration: Physical conversion removed (handled by global policy)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. [streameromakases] 신규 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE streameromakases ADD StreamerProfileId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 전역 Collation이 일치하므로 표준 JOIN 수행
                UPDATE streameromakases o JOIN streamerprofiles p ON o.ChzzkUid = p.ChzzkUid SET o.StreamerProfileId = p.Id;
                DELETE FROM streameromakases WHERE StreamerProfileId IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streameromakases", nullable: false);

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND COLUMN_NAME = 'ChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE streameromakases DROP COLUMN ChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND CONSTRAINT_NAME = 'FK_streameromakases_streamerprofiles_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE streameromakases DROP FOREIGN KEY FK_streameromakases_streamerprofiles_StreamerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // [v4.7.9] Resilient Cleaning
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @has_col = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND COLUMN_NAME = 'StreamerProfileId');
                SET @has_data = (SELECT COUNT(*) FROM streamerprofiles);

                IF @has_col > 0 AND @has_data > 0 THEN
                   SET @min_streamer = (SELECT MIN(Id) FROM streamerprofiles);
                   UPDATE streameromakases SET StreamerProfileId = @min_streamer 
                   WHERE StreamerProfileId NOT IN (SELECT Id FROM streamerprofiles) OR StreamerProfileId IS NULL OR StreamerProfileId = 0;
                END IF;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_streameromakases_streamerprofiles_StreamerProfileId",
                table: "streameromakases",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 2. [streamermanagers] 테이블 정규화
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE streamermanagers ADD StreamerProfileId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'GlobalViewerId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE streamermanagers ADD GlobalViewerId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                INSERT IGNORE INTO globalviewers (ViewerUidHash, ViewerUid)
                SELECT UPPER(HEX(SHA2(CONCAT(LOWER(ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256))), 'MIGRATED_MANAGER'
                FROM streamermanagers;

                UPDATE streamermanagers m JOIN streamerprofiles p ON m.StreamerChzzkUid = p.ChzzkUid SET m.StreamerProfileId = p.Id;
                UPDATE streamermanagers m JOIN globalviewers g ON g.ViewerUidHash = UPPER(HEX(SHA2(CONCAT(LOWER(m.ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256))) SET m.GlobalViewerId = g.Id;

                DELETE FROM streamermanagers WHERE StreamerProfileId IS NULL OR GlobalViewerId IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamermanagers", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "GlobalViewerId", table: "streamermanagers", nullable: false);

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'StreamerChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE streamermanagers DROP COLUMN StreamerChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'ManagerChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE streamermanagers DROP COLUMN ManagerChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND INDEX_NAME = 'IX_streamermanagers_StreamerProfileId_GlobalViewerId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_streamermanagers_StreamerProfileId_GlobalViewerId ON streamermanagers', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_StreamerProfileId_GlobalViewerId",
                table: "streamermanagers",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            // [v4.7.9] Resilient Cleaning
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @has_col = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'StreamerProfileId');
                SET @has_data = (SELECT COUNT(*) FROM streamerprofiles);
                SET @has_gv = (SELECT COUNT(*) FROM globalviewers);

                IF @has_col > 0 AND @has_data > 0 THEN
                    SET @min_streamer = (SELECT MIN(Id) FROM streamerprofiles);
                    UPDATE streamermanagers SET StreamerProfileId = @min_streamer 
                    WHERE StreamerProfileId NOT IN (SELECT Id FROM streamerprofiles) OR StreamerProfileId IS NULL OR StreamerProfileId = 0;

                    IF @has_gv > 0 THEN
                        UPDATE streamermanagers SET GlobalViewerId = (SELECT MIN(Id) FROM globalviewers)
                        WHERE GlobalViewerId NOT IN (SELECT Id FROM globalviewers) OR GlobalViewerId IS NULL OR GlobalViewerId = 0;
                    END IF;
                END IF;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_streamermanagers_streamerprofiles_StreamerProfileId",
                table: "streamermanagers",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamermanagers_globalviewers_GlobalViewerId",
                table: "streamermanagers",
                column: "GlobalViewerId",
                principalTable: "globalviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
