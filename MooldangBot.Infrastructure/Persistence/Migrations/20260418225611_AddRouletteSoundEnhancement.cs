using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRouletteSoundEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [오시리스의 회복]: 기존 컬럼 존재 여부를 확인하며 안전하게 추가 (마리아DB 10.2.2+ 지원)
            migrationBuilder.Sql("ALTER TABLE `func_roulette_items` ADD COLUMN IF NOT EXISTS `sound_url` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL;");
            migrationBuilder.Sql("ALTER TABLE `func_roulette_items` ADD COLUMN IF NOT EXISTS `template` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE `func_roulette_items` ADD COLUMN IF NOT EXISTS `use_default_sound` tinyint(1) NOT NULL DEFAULT 0;");

            // [오시리스의 영속]: 사운드 에셋 테이블 생성 (IF NOT EXISTS 사용)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `sound_assets` (
                    `id` int NOT NULL AUTO_INCREMENT,
                    `streamer_profile_id` int NOT NULL,
                    `name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                    `sound_url` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                    `asset_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                    `created_at` datetime(6) NOT NULL,
                    `updated_at` datetime(6) NULL,
                    CONSTRAINT `pk_sound_assets` PRIMARY KEY (`id`),
                    CONSTRAINT `fk_sound_assets_streamer_profiles_streamer_profile_id` FOREIGN KEY (`streamer_profile_id`) REFERENCES `core_streamer_profiles` (`id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");

            // 인덱스는 수동으로 존재 여부 확인 후 생성 (MariaDB는 인덱스에 IF NOT EXISTS가 없으므로 익명 블록 활용)
            migrationBuilder.Sql(@"
                SET @index_count = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sound_assets' AND INDEX_NAME = 'ix_sound_assets_streamer_profile_id');
                SET @sql = IF(@index_count = 0, 'CREATE INDEX `ix_sound_assets_streamer_profile_id` ON `sound_assets` (`streamer_profile_id`)', 'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sound_assets");

            migrationBuilder.DropColumn(
                name: "sound_url",
                table: "func_roulette_items");

            migrationBuilder.DropColumn(
                name: "template",
                table: "func_roulette_items");

            migrationBuilder.DropColumn(
                name: "use_default_sound",
                table: "func_roulette_items");
        }
    }
}
