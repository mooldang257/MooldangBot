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
            // [v4.5.12] Universal Physical Unification: Split Execution for Schema Recognition
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();

                -- 1. 물리적 변환 (Collation 통일)
                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songqueues' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songbooks' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songlistsessions' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;

                SET @target = (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'streamerprofiles' LIMIT 1);
                IF @target IS NOT NULL THEN SET @sql = CONCAT('ALTER TABLE ', @target, ' CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'); PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt; END IF;
            ");

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                -- 2. 신규 컬럼 선추가 (독립 실행하여 MariaDB가 인식하게 함)
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songqueues' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE songqueues ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songlistsessions' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE songlistsessions ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND LOWER(TABLE_NAME) = 'songbooks' AND LOWER(COLUMN_NAME) = 'streamerprofileid');
                IF @exist = 0 THEN ALTER TABLE songbooks ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;
            ");

            migrationBuilder.Sql(@"
                -- 3. 데이터 매핑 (이미 컬럼이 존재하므로 안전함)
                UPDATE songqueues s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
                UPDATE songlistsessions s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
                UPDATE songbooks s JOIN streamerprofiles p ON LOWER(s.ChzzkUid) = LOWER(p.ChzzkUid) SET s.StreamerProfileId = p.Id;
            ");

            // 나머지 로직 유지...
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
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
