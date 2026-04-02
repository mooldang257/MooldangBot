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
            // [v4.7.14] Universal Physical Unification: Split Execution for Schema Recognition
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 물리적 변환 (Collation 통일 및 대소문자 방어)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streameromakases' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamermanagers' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @profiles = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @profiles IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @profiles, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
            ");

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- 2. 신규 컬럼 선추가 (독립 실행 단위 분리)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streameromakases' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE streameromakases ADD StreamerProfileId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamermanagers' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE streamermanagers ADD StreamerProfileId int NULL; END IF;
            ");

            migrationBuilder.Sql(@"
                -- 3. 데이터 매핑 (이미 컬럼이 존재하므로 안전하게 수행)
                UPDATE streameromakases o JOIN streamerprofiles p ON LOWER(o.ChzzkUid) = LOWER(p.ChzzkUid) SET o.StreamerProfileId = p.Id;
                UPDATE streamermanagers m JOIN streamerprofiles p ON LOWER(m.Streamerschzzkuid) = LOWER(p.ChzzkUid) SET m.StreamerProfileId = p.Id;
            ");

            // 이후 속성 및 외래키 설정...
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
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
