using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class ResetRouletteStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Roulettes SET IsActive = 1;");
            migrationBuilder.Sql("UPDATE RouletteItems SET IsActive = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
