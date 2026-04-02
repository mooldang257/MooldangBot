using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRouletteViewerData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [긴급 보정]: MariaDB DDL은 비트랜잭션이므로, 이미 실행된 컬럼 삭제/추가 작업을 방어적으로 처리합니다.
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- 1. roulettespins.ViewerUid 삭제 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND COLUMN_NAME = 'ViewerUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettespins DROP COLUMN ViewerUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. roulettelogs.ViewerUid 삭제 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND COLUMN_NAME = 'ViewerUid');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettelogs DROP COLUMN ViewerUid', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 3. roulettespins.ViewerProfileId 추가 및 정제 (Nullable로 시작)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND COLUMN_NAME = 'ViewerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE roulettespins ADD ViewerProfileId int NULL', 'ALTER TABLE roulettespins MODIFY ViewerProfileId int NULL');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 4. roulettelogs.ViewerProfileId 추가 및 정제
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND COLUMN_NAME = 'ViewerProfileId');
                SET @sql = IF(@exist = 0, 'ALTER TABLE roulettelogs ADD ViewerProfileId int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 5. 데이터 클린업: 존재하지 않는 프로필 참조 행 삭제
                DELETE FROM roulettespins WHERE ViewerProfileId = 0 OR ViewerProfileId IS NULL OR ViewerProfileId NOT IN (SELECT Id FROM viewerprofiles);
                DELETE FROM roulettelogs WHERE ViewerProfileId = 0 OR (ViewerProfileId IS NOT NULL AND ViewerProfileId NOT IN (SELECT Id FROM viewerprofiles));

                -- 6. 컬럼 속성 최종 확정 (Not Null)
                ALTER TABLE roulettespins MODIFY ViewerProfileId int NOT NULL DEFAULT 0;
            ");

            // 7. 인덱스 생성 방어 (EF Core는 인덱스 중복 생성을 기본적으로 막지 않으므로 SQL 처리 고려 가능하나, 
            // 여기서는 이미 존재하는 인덱스 DROP 후 다시 생성하도록 하거나 INDEX IF NOT EXISTS가 없으므로 무시 시도)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND INDEX_NAME = 'IX_roulettespins_ViewerProfileId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_roulettespins_ViewerProfileId ON roulettespins', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND INDEX_NAME = 'IX_roulettelogs_ViewerProfileId');
                SET @sql = IF(@exist > 0, 'DROP INDEX IX_roulettelogs_ViewerProfileId ON roulettelogs', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_roulettespins_ViewerProfileId",
                table: "roulettespins",
                column: "ViewerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_ViewerProfileId",
                table: "roulettelogs",
                column: "ViewerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_roulettelogs_viewerprofiles_ViewerProfileId",
                table: "roulettelogs",
                column: "ViewerProfileId",
                principalTable: "viewerprofiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_roulettespins_viewerprofiles_ViewerProfileId",
                table: "roulettespins",
                column: "ViewerProfileId",
                principalTable: "viewerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roulettelogs_viewerprofiles_ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_roulettespins_viewerprofiles_ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropIndex(
                name: "IX_roulettespins_ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropIndex(
                name: "IX_roulettelogs_ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropColumn(
                name: "ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUid",
                table: "roulettespins",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUid",
                table: "roulettelogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
