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
            // [v4.2.5] Universal Physical Unification: Initialization Hardening
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                
                -- FK 제거 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND CONSTRAINT_NAME = 'FK_roulettespins_viewerprofiles_ViewerProfileId');
                IF @exist > 0 THEN ALTER TABLE roulettespins DROP FOREIGN KEY FK_roulettespins_viewerprofiles_ViewerProfileId; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND CONSTRAINT_NAME = 'FK_roulettelogs_viewerprofiles_ViewerProfileId');
                IF @exist > 0 THEN ALTER TABLE roulettelogs DROP FOREIGN KEY FK_roulettelogs_viewerprofiles_ViewerProfileId; END IF;

                DROP TABLE IF EXISTS roulettespins;
                DROP TABLE IF EXISTS roulettelogs;
                DROP TABLE IF EXISTS viewerprofiles;
            ");

            // 1. Create GlobalViewer table with EXPLICIT Unicode Collation
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS globalviewers (
                    Id INT NOT NULL AUTO_INCREMENT,
                    ViewerUid LONGTEXT NOT NULL,
                    ViewerUidHash VARCHAR(64) NOT NULL,
                    PRIMARY KEY (Id),
                    UNIQUE INDEX IX_globalviewers_ViewerUidHash (ViewerUidHash)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");

            // 2. Create target tables
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

            // 6. Handle Roulette Logs/Spins
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

            // EF Core Metadata
            migrationBuilder.CreateIndex(name: "IX_viewerprofiles_GlobalViewerId", table: "viewerprofiles", column: "GlobalViewerId");
            migrationBuilder.AddForeignKey(name: "FK_viewerprofiles_globalviewers_GlobalViewerId", table: "viewerprofiles", column: "GlobalViewerId", principalTable: "globalviewers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
