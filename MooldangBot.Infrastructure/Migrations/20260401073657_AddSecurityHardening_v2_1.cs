using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityHardening_v2_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "ViewerUid",
                table: "roulettespins",
                type: "longtext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUid",
                table: "roulettelogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUidHash",
                table: "roulettelogs",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_ChzzkUid_ViewerUidHash",
                table: "roulettelogs",
                columns: new[] { "ChzzkUid", "ViewerUidHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roulettelogs_ChzzkUid_ViewerUidHash",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "ViewerUid",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "ViewerUidHash",
                table: "roulettelogs");

            migrationBuilder.AlterColumn<string>(
                name: "ViewerUid",
                table: "roulettespins",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

        }
    }
}
