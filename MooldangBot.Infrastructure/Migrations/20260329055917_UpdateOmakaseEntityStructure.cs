using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOmakaseEntityStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "streamercommands");

            migrationBuilder.DropColumn(
                name: "Command",
                table: "streameromakases");

            migrationBuilder.AddColumn<string>(
                name: "ApiRedirectUrl",
                table: "streamerprofiles",
                type: "varchar(500)",
                maxLength: 500,
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

            migrationBuilder.AddColumn<int>(
                name: "MenuId",
                table: "streameromakases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 4,
                column: "TypeName",
                value: "Category");

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 6,
                column: "TypeName",
                value: "SongRequest");

            migrationBuilder.InsertData(
                table: "master_commandfeatures",
                columns: new[] { "Id", "CategoryId", "DefaultCost", "DisplayName", "IsEnabled", "RequiredRole", "TypeName" },
                values: new object[] { 11, 3, 1000, "AI 답변", true, "Viewer", "AI" });

            migrationBuilder.InsertData(
                table: "master_dynamicvariables",
                columns: new[] { "Id", "BadgeColor", "Description", "Keyword", "QueryString" },
                values: new object[,]
                {
                    { 3, "secondary", "현재 방송 제목", "{방제}", "METHOD:GetLiveTitle" },
                    { 4, "info", "현재 방송 카테고리", "{카테고리}", "METHOD:GetLiveCategory" },
                    { 5, "warning", "현재 방송 공지", "{공지}", "METHOD:GetLiveNotice" },
                    { 6, "success", "연속 출석한 일수", "{연속출석일수}", "SELECT CAST(ConsecutiveAttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 7, "info", "누적 출석한 횟수", "{누적출석일수}", "SELECT CAST(AttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 8, "secondary", "최근 출석 날짜", "{마지막출석일}", "SELECT DATE_FORMAT(LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 9, "warning", "현재 송리스트 활성화 여부", "{송리스트}", "METHOD:GetSonglistStatus" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "master_dynamicvariables",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "ApiRedirectUrl",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotChzzkUid",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "BotNickname",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "MenuId",
                table: "streameromakases");

            migrationBuilder.AddColumn<string>(
                name: "Command",
                table: "streameromakases",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamercommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ActionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommandKeyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<int>(type: "int", nullable: false),
                    RequiredRole = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamercommands", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 4,
                column: "TypeName",
                value: "StreamCategory");

            migrationBuilder.UpdateData(
                table: "master_commandfeatures",
                keyColumn: "Id",
                keyValue: 6,
                column: "TypeName",
                value: "Song");

            migrationBuilder.CreateIndex(
                name: "IX_streamercommands_ChzzkUid",
                table: "streamercommands",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_streamercommands_ChzzkUid_CommandKeyword",
                table: "streamercommands",
                columns: new[] { "ChzzkUid", "CommandKeyword" },
                unique: true);
        }
    }
}
