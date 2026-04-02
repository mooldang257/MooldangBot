using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSongV45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.5.5] Emergency Repair: Idempotent migration for Songs
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 기존 인덱스 제거 방어
                SET @indexes = 'IX_streamermanagers_ManagerChzzkUid,IX_songqueues_ChzzkUid,IX_songqueues_ChzzkUid_Id,IX_songqueues_ChzzkUid_Status_CreatedAt,IX_songlistsessions_ChzzkUid_IsActive,IX_songbooks_ChzzkUid_Id,IX_overlaypresets_ChzzkUid';
                -- (간결함을 위해 개별 체크 대신 동적 SQL 생략하고 개별 체크 SQL 반복)
                
                -- IX_streamermanagers_ManagerChzzkUid
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND INDEX_NAME = 'IX_streamermanagers_StreamerProfileId_GlobalViewerId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_streamermanagers_StreamerProfileId_GlobalViewerId ON streamermanagers', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- IX_songqueues_ChzzkUid
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_ChzzkUid');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_ChzzkUid ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- ... 기타 인덱스들도 동일 패턴으로 처리
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_ChzzkUid_Id');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_ChzzkUid_Id ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_StreamerProfileId ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_StreamerProfileId_Id');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_StreamerProfileId_Id ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_StreamerProfileId_Status_CreatedAt');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_StreamerProfileId_Status_CreatedAt ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songlistsessions' AND INDEX_NAME = 'IX_songlistsessions_StreamerProfileId_IsActive');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songlistsessions_StreamerProfileId_IsActive ON songlistsessions', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songbooks' AND INDEX_NAME = 'IX_songbooks_StreamerProfileId_Id');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songbooks_StreamerProfileId_Id ON songbooks', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. 기존 컬럼 제거 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'ChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE songqueues DROP COLUMN ChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songlistsessions' AND COLUMN_NAME = 'ChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE songlistsessions DROP COLUMN ChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songbooks' AND COLUMN_NAME = 'ChzzkUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE songbooks DROP COLUMN ChzzkUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 3. 새 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'GlobalViewerId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE songqueues ADD GlobalViewerId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE songqueues ADD StreamerProfileId int NOT NULL DEFAULT 0', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songlistsessions' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE songlistsessions ADD StreamerProfileId int NOT NULL DEFAULT 0', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songbooks' AND COLUMN_NAME = 'StreamerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE songbooks ADD StreamerProfileId int NOT NULL DEFAULT 0', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // 이후 인덱스 생성 및 FK 설정은 표준 메서드로 진행 (이미 존재하는 경우를 대비해 SQL에서 먼저 DROP 함)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- 인덱스 재생성을 위한 선제거
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_GlobalViewerId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_songqueues_GlobalViewerId ON songqueues', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                -- (기타 인덱스들도 필요시 추가)
            ");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_GlobalViewerId",
                table: "songqueues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId_Id",
                table: "songqueues",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId_Status_CreatedAt",
                table: "songqueues",
                columns: new[] { "StreamerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_songlistsessions_StreamerProfileId_IsActive",
                table: "songlistsessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_songbooks_StreamerProfileId_Id",
                table: "songbooks",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_songbooks_streamerprofiles_StreamerProfileId",
                table: "songbooks",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_songlistsessions_streamerprofiles_StreamerProfileId",
                table: "songlistsessions",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_songqueues_globalviewers_GlobalViewerId",
                table: "songqueues",
                column: "GlobalViewerId",
                principalTable: "globalviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_songqueues_streamerprofiles_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_songbooks_streamerprofiles_StreamerProfileId",
                table: "songbooks");

            migrationBuilder.DropForeignKey(
                name: "FK_songlistsessions_streamerprofiles_StreamerProfileId",
                table: "songlistsessions");

            migrationBuilder.DropForeignKey(
                name: "FK_songqueues_globalviewers_GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropForeignKey(
                name: "FK_songqueues_streamerprofiles_StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId_Id",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId_Status_CreatedAt",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songlistsessions_StreamerProfileId_IsActive",
                table: "songlistsessions");

            migrationBuilder.DropIndex(
                name: "IX_songbooks_StreamerProfileId_Id",
                table: "songbooks");

            migrationBuilder.DropColumn(
                name: "GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songlistsessions");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songbooks");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songqueues",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songlistsessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songbooks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_ManagerChzzkUid",
                table: "streamermanagers",
                column: "ManagerChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Id",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Status_CreatedAt",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_songlistsessions_ChzzkUid_IsActive",
                table: "songlistsessions",
                columns: new[] { "ChzzkUid", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_songbooks_ChzzkUid_Id",
                table: "songbooks",
                columns: new[] { "ChzzkUid", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_overlaypresets_ChzzkUid",
                table: "overlaypresets",
                column: "ChzzkUid");
        }
    }
}
