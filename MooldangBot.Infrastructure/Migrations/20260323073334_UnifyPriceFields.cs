using MooldangBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyPriceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SongCheesePrice",
                table: "streamerprofiles",
                newName: "SongPrice");

            migrationBuilder.RenameColumn(
                name: "OmakaseCheesePrice",
                table: "streamerprofiles",
                newName: "OmakasePrice");

            migrationBuilder.RenameColumn(
                name: "CheesePrice",
                table: "streameromakases",
                newName: "Price");

            migrationBuilder.UpdateData(
                table: "streamerprofiles",
                keyColumn: "ChzzkUid",
                keyValue: null,
                column: "ChzzkUid",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ChzzkUid",
                table: "streamerprofiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SongPrice",
                table: "streamerprofiles",
                newName: "SongCheesePrice");

            migrationBuilder.RenameColumn(
                name: "OmakasePrice",
                table: "streamerprofiles",
                newName: "OmakaseCheesePrice");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "streameromakases",
                newName: "CheesePrice");

            migrationBuilder.AlterColumn<string>(
                name: "ChzzkUid",
                table: "streamerprofiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
