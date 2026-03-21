using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToStreamerCommand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "streamercommands",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "streamercommands");
        }
    }
}
