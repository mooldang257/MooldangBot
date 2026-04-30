using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeVectorTo3072 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 컬럼 확장 (768 -> 3072)
            migrationBuilder.Sql("ALTER TABLE `func_song_master_library` MODIFY COLUMN `title_vector` VECTOR(3072) NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE `func_song_master_staging` MODIFY COLUMN `title_vector` VECTOR(3072) NULL;");
            migrationBuilder.Sql("ALTER TABLE `func_song_books` MODIFY COLUMN `title_vector` VECTOR(3072) NULL;");
            migrationBuilder.Sql("ALTER TABLE `func_song_streamer_library` MODIFY COLUMN `title_vector` VECTOR(3072) NULL;");

            // 2. 데이터 초기화 (백필 트리거)
            migrationBuilder.Sql("UPDATE `func_song_master_library` SET `title_vector` = NULL;");
            migrationBuilder.Sql("UPDATE `func_song_master_staging` SET `title_vector` = NULL;");
            migrationBuilder.Sql("UPDATE `func_song_books` SET `title_vector` = NULL;");
            migrationBuilder.Sql("UPDATE `func_song_streamer_library` SET `title_vector` = NULL;");

            // 3. 인덱스 생성 (기존에 없던 경우를 위해)
            migrationBuilder.CreateIndex(
                name: "ix_func_song_books_streamer_profile_id_song_no",
                table: "func_song_books",
                columns: new[] { "streamer_profile_id", "song_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_func_song_books_streamer_profile_id_song_no",
                table: "func_song_books");

            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "func_song_streamer_library");

            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "func_song_master_staging");

            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "func_song_books");
        }
    }
}
