using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSongRequiredPointsAndAccumulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stats_roulette_audit_func_roulette_main_roulette_id",
                table: "log_roulette_stats");

            migrationBuilder.AlterColumn<string>(
                name: "pitch",
                table: "func_song_list_queues",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<int>(
                name: "required_points",
                table: "func_song_books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "viewer_uid",
                table: "core_global_viewers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_song_accumulations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    song_book_id = table.Column<int>(type: "int", nullable: true),
                    song_title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    current_points = table.Column<int>(type: "int", nullable: false),
                    last_donator_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_song_accumulations", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_song_accumulations_core_streamer_profiles_streamer_prof",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_func_song_accumulations_func_song_books_song_book_id",
                        column: x => x.song_book_id,
                        principalTable: "func_song_books",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalViewer_ViewerUid",
                table: "core_global_viewers",
                column: "viewer_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_func_song_accumulations_song_book_id",
                table: "func_song_accumulations",
                column: "song_book_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_song_accumulations_streamer_profile_id_song_book_id",
                table: "func_song_accumulations",
                columns: new[] { "streamer_profile_id", "song_book_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_log_roulette_stats_func_roulettes_roulette_id",
                table: "log_roulette_stats",
                column: "roulette_id",
                principalTable: "func_roulette_main",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_log_roulette_stats_func_roulettes_roulette_id",
                table: "log_roulette_stats");

            migrationBuilder.DropTable(
                name: "func_song_accumulations");

            migrationBuilder.DropIndex(
                name: "IX_GlobalViewer_ViewerUid",
                table: "core_global_viewers");

            migrationBuilder.DropColumn(
                name: "required_points",
                table: "func_song_books");

            migrationBuilder.AlterColumn<string>(
                name: "pitch",
                table: "func_song_list_queues",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AlterColumn<string>(
                name: "viewer_uid",
                table: "core_global_viewers",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddForeignKey(
                name: "fk_stats_roulette_audit_func_roulette_main_roulette_id",
                table: "log_roulette_stats",
                column: "roulette_id",
                principalTable: "func_roulette_main",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
