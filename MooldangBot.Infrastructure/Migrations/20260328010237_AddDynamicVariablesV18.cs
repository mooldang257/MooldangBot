using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicVariablesV18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "master_dynamicvariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BadgeColor = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryString = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_dynamicvariables", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "master_dynamicvariables",
                columns: new[] { "Id", "BadgeColor", "Description", "Keyword", "QueryString" },
                values: new object[,]
                {
                    { 1, "primary", "보유 포인트", "{포인트}", "SELECT CAST(Point AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 2, "success", "시청자 닉네임", "{닉네임}", "SELECT ViewerName FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "master_dynamicvariables");
        }
    }
}
