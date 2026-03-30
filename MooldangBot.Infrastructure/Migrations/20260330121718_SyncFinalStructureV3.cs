using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncFinalStructureV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plainchatmessages");

            migrationBuilder.AddColumn<string>(
                name: "ApiClientId",
                table: "streamerprofiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApiClientSecret",
                table: "streamerprofiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApiRedirectUrl",
                table: "streamerprofiles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BotAccessToken",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BotChzzkUid",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BotNickname",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BotRefreshToken",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "BotTokenExpiresAt",
                table: "streamerprofiles",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiClientId",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "ApiClientSecret",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "ApiRedirectUrl",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotAccessToken",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotChzzkUid",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotNickname",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotRefreshToken",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotTokenExpiresAt",
                table: "streamerprofiles");

            migrationBuilder.CreateTable(
                name: "plainchatmessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DonationAmount = table.Column<int>(type: "int", nullable: false),
                    SenderNickname = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderUid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plainchatmessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_plainchatmessages_ChzzkUid_CreatedAt",
                table: "plainchatmessages",
                columns: new[] { "ChzzkUid", "CreatedAt" });
        }
    }
}
