using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemResponseV194 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "master_commandfeatures",
                columns: new[] { "Id", "CategoryId", "DefaultCost", "DisplayName", "IsEnabled", "RequiredRole", "TypeName" },
                values: new object[] { 10, 2, 0, "시스템 응답", true, "Manager", "SystemResponse" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
