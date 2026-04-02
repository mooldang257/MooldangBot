using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhilosophyNormalization_v4_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.8.14] Universal Physical Unification: Split Execution for Schema Recognition
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 물리적 변환 (Collation 통일 및 대소문자 방어)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'broadcastsessions' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'iamf_vibration_logs' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'iamf_streamer_settings' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerknowledges' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @profiles = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @profiles IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @profiles, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
            ");

            // 0. [테이블 이름 변경]
            migrationBuilder.RenameTable(
                name: "StreamerKnowledges",
                newName: "streamerknowledges");

            // 1. [컬럼 추가]
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", type: "int", nullable: true);

            // 2. [데이터 이관]
            // AddColumn은 EF Core 명령어로 별도 배치로 전달되므로, 이후 Sql()에서 안전하게 인식됩니다.
            migrationBuilder.Sql(@"
                -- MariaDB 대소문자 방어 및 데이터 매핑
                UPDATE broadcastsessions s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
                UPDATE iamf_vibration_logs s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
                UPDATE iamf_streamer_settings s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
                UPDATE streamerknowledges s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
            ");

            // 3. [정화]
            migrationBuilder.Sql("DELETE FROM broadcastsessions WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM streamerknowledges WHERE StreamerProfileId IS NULL;");

            // 4. [속성 업데이트 및 제약 조건]
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", nullable: false);
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "broadcastsessions");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "streamerknowledges");

            migrationBuilder.AddForeignKey(
                name: "FK_broadcastsessions_streamerprofiles_StreamerProfileId",
                table: "broadcastsessions",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamerknowledges_streamerprofiles_StreamerProfileId",
                table: "streamerknowledges",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
