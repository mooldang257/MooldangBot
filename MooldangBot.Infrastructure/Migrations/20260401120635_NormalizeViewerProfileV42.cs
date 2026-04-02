using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeViewerProfileV42 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.2.4] Emergency Repair: Drop corrupted empty tables before recreation
            // [v4.9.1] 외래 키 제약 조건 선제거 (삭제 블로킹 방지)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                
                -- 1. roulettespins FK 제거 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND CONSTRAINT_NAME = 'FK_roulettespins_viewerprofiles_ViewerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettespins DROP FOREIGN KEY FK_roulettespins_viewerprofiles_ViewerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. roulettelogs FK 제거 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND CONSTRAINT_NAME = 'FK_roulettelogs_viewerprofiles_ViewerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettelogs DROP FOREIGN KEY FK_roulettelogs_viewerprofiles_ViewerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 3. 이제 안전하게 테이블 삭제
                DROP TABLE IF EXISTS roulettespins;
                DROP TABLE IF EXISTS roulettelogs;
                DROP TABLE IF EXISTS viewerprofiles;
            ");

            // 1. Create GlobalViewer table if not exists
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS globalviewers (
                    Id INT NOT NULL AUTO_INCREMENT,
                    ViewerUid LONGTEXT NOT NULL,
                    ViewerUidHash VARCHAR(64) NOT NULL,
                    PRIMARY KEY (Id),
                    UNIQUE INDEX IX_globalviewers_ViewerUidHash (ViewerUidHash)
                ) CHARACTER SET utf8mb4;");

            // 2. Create target tables with NEW schema directly (Initialization)
            migrationBuilder.Sql(@"
                CREATE TABLE viewerprofiles (
                    Id INT NOT NULL AUTO_INCREMENT,
                    StreamerProfileId INT NOT NULL,
                    GlobalViewerId INT NOT NULL,
                    Nickname VARCHAR(100) NOT NULL,
                    Points INT NOT NULL DEFAULT 0,
                    AttendanceCount INT NOT NULL DEFAULT 0,
                    ConsecutiveAttendanceCount INT NOT NULL DEFAULT 0,
                    LastAttendanceAt DATETIME(6) NULL,
                    PRIMARY KEY (Id)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");

            // 3. Skip data migration as tables were empty

            // 6. Handle Roulette Logs/Spins (Recreate due to tablespace corruption)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS roulettelogs (
                    Id BIGINT NOT NULL AUTO_INCREMENT,
                    ChzzkUid VARCHAR(100) NOT NULL,
                    RouletteId INT NOT NULL,
                    RouletteName VARCHAR(100) NOT NULL,
                    ViewerNickname VARCHAR(100) NOT NULL,
                    ItemName VARCHAR(200) NOT NULL,
                    IsMission TINYINT(1) NOT NULL,
                    Status INT NOT NULL,
                    ProcessedAt DATETIME(6) NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    ViewerUid LONGTEXT NULL,
                    ViewerUidHash VARCHAR(64) NULL,
                    PRIMARY KEY (Id),
                    INDEX IX_roulettelogs_RouletteId (RouletteId),
                    INDEX IX_roulettelogs_ChzzkUid_ViewerUidHash (ChzzkUid, ViewerUidHash)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS roulettespins (
                    Id VARCHAR(255) NOT NULL,
                    ChzzkUid VARCHAR(50) NOT NULL,
                    RouletteId INT NOT NULL,
                    ViewerUid LONGTEXT NOT NULL,
                    ViewerNickname VARCHAR(100) NOT NULL,
                    ResultsJson LONGTEXT NOT NULL,
                    Summary LONGTEXT NOT NULL,
                    IsCompleted TINYINT(1) NOT NULL,
                    ScheduledTime DATETIME(6) NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    PRIMARY KEY (Id),
                    INDEX IX_roulettespins_IsCompleted_ScheduledTime (IsCompleted, ScheduledTime)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");

            // 6-1. Copy data from ViewerProfileId if applicable (Skipped as tables were recreated empty)
            /* Data mapping skipped for recreated tables */

            migrationBuilder.Sql("SET innodb_lock_wait_timeout = 600;");

            // 8. EF Core Metadata (Indexes/FKs)
            migrationBuilder.CreateIndex(
                name: "IX_viewerprofiles_GlobalViewerId",
                table: "viewerprofiles",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_viewerprofiles_StreamerProfileId_GlobalViewerId",
                table: "viewerprofiles",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_viewerprofiles_StreamerProfileId_Points",
                table: "viewerprofiles",
                columns: new[] { "StreamerProfileId", "Points" });

            migrationBuilder.AddForeignKey(
                name: "FK_viewerprofiles_globalviewers_GlobalViewerId",
                table: "viewerprofiles",
                column: "GlobalViewerId",
                principalTable: "globalviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_viewerprofiles_streamerprofiles_StreamerProfileId",
                table: "viewerprofiles",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 9. Master Data Update
            migrationBuilder.UpdateData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 1,
                column: "QueryString",
                value: "SELECT CAST(vp.Points AS CHAR) FROM viewerprofiles vp JOIN streamerprofiles sp ON vp.StreamerProfileId = sp.Id JOIN globalviewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 2,
                column: "QueryString",
                value: "SELECT vp.Nickname FROM viewerprofiles vp JOIN streamerprofiles sp ON vp.StreamerProfileId = sp.Id JOIN globalviewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 6,
                column: "QueryString",
                value: "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM viewerprofiles vp JOIN streamerprofiles sp ON vp.StreamerProfileId = sp.Id JOIN globalviewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 7,
                column: "QueryString",
                value: "SELECT CAST(vp.AttendanceCount AS CHAR) FROM viewerprofiles vp JOIN streamerprofiles sp ON vp.StreamerProfileId = sp.Id JOIN globalviewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 8,
                column: "QueryString",
                value: "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewerprofiles vp JOIN streamerprofiles sp ON vp.StreamerProfileId = sp.Id JOIN globalviewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback is complex, usually not recommended for normalization migrations
        }
    }
}
