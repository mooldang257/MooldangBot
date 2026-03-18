using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleOmakases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StreamerCommands",
                table: "StreamerCommands");

            migrationBuilder.RenameTable(
                name: "StreamerCommands",
                newName: "streamercommands");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streamercommands",
                table: "streamercommands",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "streameromakases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Command = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CheesePrice = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streameromakases", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "streameromakases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streamercommands",
                table: "streamercommands");

            migrationBuilder.RenameTable(
                name: "streamercommands",
                newName: "StreamerCommands");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StreamerCommands",
                table: "StreamerCommands",
                column: "Id");
        }
    }
}
