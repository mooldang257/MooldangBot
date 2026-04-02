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
            // [v4.8.13] Universal Physical Unification: Case-Insensitive Hardening
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 정렬 규칙 통일을 위해 대상 테이블 물리적 변환
                -- 리눅스 대소문자 구분을 방어하기 위해 존재 여부와 실제 이름을 체크하여 변환합니다.
                
                -- broadcastsessions
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'broadcastsessions' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- iamf_vibration_logs
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'iamf_vibration_logs' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- iamf_streamer_settings
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'iamf_streamer_settings' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- StreamerKnowledges (특히 대소문자가 꼬일 확률이 높음)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerknowledges' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- streamerprofiles
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;
            ");

            // 0. [테이블 이름 변경]
            // RenameTable은 내부적으로 실제 이름을 찾아서 바꿔주지만, 안전을 위해 순서를 지킵니다.
            migrationBuilder.RenameTable(
                name: "StreamerKnowledges",
                newName: "streamerknowledges");

            // 1. [컬럼 추가]
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", type: "int", nullable: true);

            // 2. [데이터 이관] 물리적 형식이 통일되었으므로 표준 JOIN 수행
            migrationBuilder.Sql(@"
                -- ChzzkUid 컬럼의 대소문자가 다를 수 있으므로 JOIN 조건을 LOWER()로 감싸거나, 위에서 변환된 형식을 신뢰합니다.
                UPDATE broadcastsessions s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE iamf_vibration_logs s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE iamf_streamer_settings s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE streamerknowledges s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");

            // 3. [정화 및 속성 업데이트]
            migrationBuilder.Sql("DELETE FROM broadcastsessions WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM streamerknowledges WHERE StreamerProfileId IS NULL;");

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
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
