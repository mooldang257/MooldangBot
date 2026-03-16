using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddChzzkOAuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChzzkAccessToken",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkRefreshToken",
                table: "streamerprofiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "streamerprofiles",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChzzkAccessToken",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "ChzzkRefreshToken",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "streamerprofiles");
        }
    }
}
