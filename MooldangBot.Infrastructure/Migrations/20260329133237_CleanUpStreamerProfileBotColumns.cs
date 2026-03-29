using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpStreamerProfileBotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_streameromakases_ChzzkUid_MenuId",
                table: "streameromakases");

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

            migrationBuilder.DropColumn(
                name: "MenuId",
                table: "streameromakases");

            migrationBuilder.DropColumn(
                name: "Command",
                table: "roulettes");

            migrationBuilder.DropColumn(
                name: "CostPerSpin",
                table: "roulettes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "roulettes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "roulettes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streameromakases",
                table: "streameromakases",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "plainchatmessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderUid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderNickname = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DonationAmount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plainchatmessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roulettespins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ViewerUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettespins", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_streameromakases_ChzzkUid",
                table: "streameromakases",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_plainchatmessages_ChzzkUid_CreatedAt",
                table: "plainchatmessages",
                columns: new[] { "ChzzkUid", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_roulettespins_IsCompleted_ScheduledTime",
                table: "roulettespins",
                columns: new[] { "IsCompleted", "ScheduledTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plainchatmessages");

            migrationBuilder.DropTable(
                name: "roulettespins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streameromakases",
                table: "streameromakases");

            migrationBuilder.DropIndex(
                name: "IX_streameromakases_ChzzkUid",
                table: "streameromakases");

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

            migrationBuilder.AddColumn<int>(
                name: "MenuId",
                table: "streameromakases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Command",
                table: "roulettes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CostPerSpin",
                table: "roulettes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "roulettes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "roulettes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_streameromakases_ChzzkUid_MenuId",
                table: "streameromakases",
                columns: new[] { "ChzzkUid", "MenuId" });
        }
    }
}
