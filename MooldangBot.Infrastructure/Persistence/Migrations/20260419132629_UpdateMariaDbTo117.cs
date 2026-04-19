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
            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "viewer_relations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "title_vector",
                table: "func_song_master_library",
                type: "VECTOR(768)",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "func_roulette_spins",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            // [v11.7.2]: MariaDB 11.7 전용 HNSW 벡터 인덱스 추가
            migrationBuilder.Sql("ALTER TABLE func_song_master_library ADD VECTOR INDEX (title_vector) M=8 DISTANCE=cosine;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nickname",
                table: "viewer_relations");

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
