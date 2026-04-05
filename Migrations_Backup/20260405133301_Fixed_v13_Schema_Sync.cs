using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fixed_v13_Schema_Sync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_book_main");

            migrationBuilder.AddColumn<long>(
                name: "master_song_library_id",
                table: "song_list_queues",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "func_song_master_library",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "metadata_key",
                table: "func_song_master_library",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "func_song_streamer_library",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    master_song_library_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    youtube_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    youtube_title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lyrics = table.Column<string>(type: "TEXT", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_song_streamer_library", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_song_streamer_library_func_song_master_library_master_s",
                        column: x => x.master_song_library_id,
                        principalTable: "func_song_master_library",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_func_song_streamer_library_streamer_profiles_streamer_profil",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_master_song_library_id",
                table: "song_list_queues",
                column: "master_song_library_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_book_main",
                columns: new[] { "streamer_profile_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_func_song_master_library_metadata_key",
                table: "func_song_master_library",
                column: "metadata_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_func_song_streamer_library_master_song_library_id",
                table: "func_song_streamer_library",
                column: "master_song_library_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_song_streamer_library_streamer_profile_id_master_song_l",
                table: "func_song_streamer_library",
                columns: new[] { "streamer_profile_id", "master_song_library_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_master_song_libraries_master_song_library_id",
                table: "song_list_queues",
                column: "master_song_library_id",
                principalTable: "func_song_master_library",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_master_song_libraries_master_song_library_id",
                table: "song_list_queues");

            migrationBuilder.DropTable(
                name: "func_song_streamer_library");

            migrationBuilder.DropIndex(
                name: "ix_song_list_queues_master_song_library_id",
                table: "song_list_queues");

            migrationBuilder.DropIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_book_main");

            migrationBuilder.DropIndex(
                name: "ix_func_song_master_library_metadata_key",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "master_song_library_id",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "metadata_key",
                table: "func_song_master_library");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "func_song_master_library",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.CreateIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_book_main",
                columns: new[] { "streamer_profile_id", "id" },
                descending: new[] { false, true });
        }
    }
}
