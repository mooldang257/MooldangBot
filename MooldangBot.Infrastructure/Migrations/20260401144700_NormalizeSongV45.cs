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
            // [v4.5.11] Universal Physical Unification: Case-Insensitive Hardening
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 정렬 규칙 통일을 위해 대상 테이블 물리적 변환 (대소문자 방어)
                -- songqueues
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songqueues' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- songbooks
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songbooks' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- songlistsessions
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songlistsessions' LIMIT 1);
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

                -- 1. 신규 컬럼 추가 및 데이터 매핑
                -- (물리적 일치 상태이므로 JOIN 조건이 일관성 있게 동작함)
                UPDATE songqueues s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
            ");

            // 나머지 로직은 기존과 동일... (이미 작성해둔 코드 유지)
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "songqueues", nullable: false);
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
        }
    }
}
