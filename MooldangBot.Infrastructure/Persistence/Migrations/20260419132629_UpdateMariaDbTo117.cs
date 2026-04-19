using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMariaDbTo117 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v11.7.2]: 벡터 인덱스 생성을 위해 강제로 NOT NULL 제약 조건 적용
            migrationBuilder.Sql("ALTER TABLE `func_song_master_library` ADD COLUMN IF NOT EXISTS `title_vector` VECTOR(768) NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE `func_song_master_library` MODIFY COLUMN `title_vector` VECTOR(768) NOT NULL;");
            
            // RouletteSpin 데이터 초기화 (GUID -> bigint 변환 충돌 방지)
            migrationBuilder.Sql("TRUNCATE TABLE `func_roulette_spins`;");
            
            // RouletteSpin ID 변경 (이미 bigint인 경우 무시됨)
            migrationBuilder.Sql("ALTER TABLE `func_roulette_spins` MODIFY COLUMN `id` bigint NOT NULL AUTO_INCREMENT;");

            // 벡터 인덱스 추가 (이미 존재하는 경우를 대비해 예외 처리 또는 존재 여부 확인 로직 권장되나 MariaDB 11.7 규격에 맞춰 실행)
            migrationBuilder.Sql("ALTER TABLE func_song_master_library ADD VECTOR INDEX IF NOT EXISTS (title_vector) M=8 DISTANCE=cosine;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "func_song_master_library");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "func_roulette_spins",
                type: "varchar(255)",
                nullable: false,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
