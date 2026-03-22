using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOmakaseEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOmakaseEnabled",
                table: "streamerprofiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOmakaseEnabled",
                table: "streamerprofiles");
        }
    }
}
