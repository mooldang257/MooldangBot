using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReorganizeMasterData_v17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🌊 [오시리스의 정화]: 기존 마스터 및 연동 데이터를 전면 초기화합니다.
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 0;");
            migrationBuilder.Sql("TRUNCATE TABLE unifiedcommands;");
            migrationBuilder.Sql("TRUNCATE TABLE master_commandfeatures;");
            migrationBuilder.Sql("TRUNCATE TABLE master_commandcategories;");
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 1;");

            // 💎 [v1.7] 신규 마스터 카테고리 주입
            migrationBuilder.InsertData(
                table: "master_commandcategories",
                columns: new[] { "Id", "Name", "DisplayName", "SortOrder" },
                values: new object[,]
                {
                    { 1, "General", "일반", 1 },
                    { 2, "System", "시스템메세지", 2 },
                    { 3, "Feature", "기능", 3 }
                });

            // 🚀 [v1.7] 신규 마스터 기능 주입 (9종)
            migrationBuilder.InsertData(
                table: "master_commandfeatures",
                columns: new[] { "Id", "CategoryId", "TypeName", "DisplayName", "DefaultCost", "RequiredRole", "IsEnabled" },
                values: new object[,]
                {
                    { 1, 1, "Reply", "텍스트 답변", 0, "Viewer", true },
                    { 2, 2, "Notice", "공지", 0, "Manager", true },
                    { 3, 2, "Title", "방제", 0, "Manager", true },
                    { 4, 2, "StreamCategory", "카테고리", 0, "Manager", true },
                    { 5, 2, "SonglistToggle", "송리스트", 0, "Manager", true },
                    { 6, 3, "Song", "노래신청", 1000, "Viewer", true },
                    { 7, 3, "Omakase", "오마카세", 1000, "Viewer", true },
                    { 8, 3, "Roulette", "룰렛", 500, "Viewer", true },
                    { 9, 3, "ChatPoint", "채팅포인트", 0, "Viewer", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "master_commandcategories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "시스템 고정", "Fixed" });

            migrationBuilder.UpdateData(
                table: "master_commandcategories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "후원 연동", "Donation" });

            migrationBuilder.InsertData(
                table: "master_commandcategories",
                columns: new[] { "Id", "DisplayName", "Name", "SortOrder" },
                values: new object[] { 4, "포인트 소모", "Point", 4 });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DisplayName", "RequiredRole", "TypeName" },
                values: new object[] { "출석 체크", "Viewer", "Attendance" });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisplayName", "RequiredRole", "TypeName" },
                values: new object[] { "포인트 확인", "Viewer", "PointCheck" });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CategoryId", "DefaultCost", "DisplayName", "RequiredRole", "TypeName" },
                values: new object[] { 3, 1000, "노래 신청", "Viewer", "Song" });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CategoryId", "DefaultCost", "DisplayName", "RequiredRole", "TypeName" },
                values: new object[] { 3, 500, "치즈 룰렛", "Viewer", "Roulette" });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CategoryId", "DefaultCost", "DisplayName", "TypeName" },
                values: new object[] { 4, 100, "포인트 룰렛", "PointRoulette" });

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CategoryId", "DefaultCost", "DisplayName", "RequiredRole", "TypeName" },
                values: new object[] { 1, 0, "송리스트 활성/비활성", "Manager", "SonglistToggle" });
        }
    }
}
