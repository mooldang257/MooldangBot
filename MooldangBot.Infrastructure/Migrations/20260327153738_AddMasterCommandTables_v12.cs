using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterCommandTables_v12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unifiedcommandcategories");

            migrationBuilder.DropTable(
                name: "unifiedcommandfeatures");

            migrationBuilder.CreateTable(
                name: "master_commandcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_commandcategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "master_commandfeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCost = table.Column<int>(type: "int", nullable: false),
                    RequiredRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_commandfeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_commandfeatures_master_commandcategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "master_commandcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "master_commandcategories",
                columns: new[] { "Id", "DisplayName", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "일반", "General", 1 },
                    { 2, "시스템 고정", "Fixed", 2 },
                    { 3, "후원 연동", "Donation", 3 },
                    { 4, "포인트 소모", "Point", 4 }
                });

            migrationBuilder.InsertData(
                table: "master_commandfeatures",
                columns: new[] { "Id", "CategoryId", "DefaultCost", "DisplayName", "IsEnabled", "RequiredRole", "TypeName" },
                values: new object[,]
                {
                    { 1, 1, 0, "텍스트 답변", true, "Viewer", "Reply" },
                    { 2, 2, 0, "출석 체크", true, "Viewer", "Attendance" },
                    { 3, 2, 0, "포인트 확인", true, "Viewer", "PointCheck" },
                    { 4, 3, 1000, "노래 신청", true, "Viewer", "Song" },
                    { 5, 3, 500, "치즈 룰렛", true, "Viewer", "Roulette" },
                    { 6, 4, 100, "포인트 룰렛", true, "Viewer", "PointRoulette" },
                    { 7, 1, 0, "송리스트 활성/비활성", true, "Manager", "SonglistToggle" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_master_commandfeatures_CategoryId",
                table: "master_commandfeatures",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "master_commandfeatures");

            migrationBuilder.DropTable(
                name: "master_commandcategories");

            migrationBuilder.CreateTable(
                name: "unifiedcommandcategories",
                columns: table => new
                {
                    CategoryName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unifiedcommandcategories", x => x.CategoryName);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "unifiedcommandfeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCost = table.Column<int>(type: "int", nullable: false),
                    DefaultCostType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultRequiredRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultResponse = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeatureName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unifiedcommandfeatures", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommandfeatures_CategoryName_FeatureName",
                table: "unifiedcommandfeatures",
                columns: new[] { "CategoryName", "FeatureName" },
                unique: true);
        }
    }
}
