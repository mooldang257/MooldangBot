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
            migrationBuilder.AddColumn<string>(
                name: "sound_url",
                table: "func_roulette_items",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "template",
                table: "func_roulette_items",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "use_default_sound",
                table: "func_roulette_items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "sound_assets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sound_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    asset_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sound_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_sound_assets_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "ix_sound_assets_streamer_profile_id",
                table: "sound_assets",
                column: "streamer_profile_id");
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
