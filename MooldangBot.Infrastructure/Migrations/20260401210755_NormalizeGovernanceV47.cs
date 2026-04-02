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
            // [v4.7.13] Universal Physical Unification: Case-Insensitive Hardening
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 정렬 규칙 통일을 위해 대상 테이블 물리적 변환 (대소문자 방어)
                -- streameromakases
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streameromakases' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- streamermanagers
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamermanagers' LIMIT 1);
                IF @target IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- streamerprofiles
                SET @profiles = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @profiles IS NOT NULL THEN
                    SET @sql = CONCAT('ALTER TABLE ', @profiles, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
                    PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                END IF;

                -- 1. 데이터 매핑 (물리적 일치 상태)
                UPDATE streameromakases o JOIN streamerprofiles p ON LOWER(o.ChzzkUid) = LOWER(p.ChzzkUid) SET o.StreamerProfileId = p.Id;
            ");

            // 나머지 로직 유지...
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streameromakases", nullable: false);
            migrationBuilder.AddForeignKey(
                name: "FK_streameromakases_streamerprofiles_StreamerProfileId",
                table: "streameromakases",
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
