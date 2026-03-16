using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class Step1_DatabaseExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StreamerProfiles",
                table: "StreamerProfiles");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "StreamerProfiles");

            migrationBuilder.RenameTable(
                name: "StreamerProfiles",
                newName: "streamerprofiles");

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
                name: "DesignSettingsJson",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NoticeMemo",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OmakaseCheesePrice",
                table: "streamerprofiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OmakaseCommand",
                table: "streamerprofiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OmakaseCount",
                table: "streamerprofiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SongCommand",
                table: "streamerprofiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streamerprofiles",
                table: "streamerprofiles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "songqueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_songqueues", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "songqueues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streamerprofiles",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "ApiClientId",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "ApiClientSecret",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "DesignSettingsJson",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "NoticeMemo",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "OmakaseCheesePrice",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "OmakaseCommand",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "OmakaseCount",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "SongCommand",
                table: "streamerprofiles");

            migrationBuilder.RenameTable(
                name: "streamerprofiles",
                newName: "StreamerProfiles");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "StreamerProfiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StreamerProfiles",
                table: "StreamerProfiles",
                column: "Id");
        }
    }
}
