using MooldangBot.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouletteIsActiveAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
/*
            migrationBuilder.DropIndex(
                name: "IX_roulettes_ChzzkUid",
                table: "roulettes");
*/

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "roulettes",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "rouletteitems",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

/*
            migrationBuilder.CreateTable(
                name: "streamermanagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ManagerChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamermanagers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
*/

            migrationBuilder.CreateIndex(
                name: "IX_roulettes_ChzzkUid_Id",
                table: "roulettes",
                columns: new[] { "ChzzkUid", "Id" });

/*
            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_ManagerChzzkUid",
                table: "streamermanagers",
                column: "ManagerChzzkUid");
*/
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
/*
            migrationBuilder.DropTable(
                name: "streamermanagers");
*/

            migrationBuilder.DropIndex(
                name: "IX_roulettes_ChzzkUid_Id",
                table: "roulettes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "roulettes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "rouletteitems");

/*
            migrationBuilder.CreateIndex(
                name: "IX_roulettes_ChzzkUid",
                table: "roulettes",
                column: "ChzzkUid");
*/
}
    }
}
