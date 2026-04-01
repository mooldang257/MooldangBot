using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRouletteViewerData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewerUid",
                table: "roulettespins");

            migrationBuilder.DropColumn(
                name: "ViewerUid",
                table: "roulettelogs");

            migrationBuilder.AddColumn<int>(
                name: "ViewerProfileId",
                table: "roulettespins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewerProfileId",
                table: "roulettelogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_roulettespins_ViewerProfileId",
                table: "roulettespins",
                column: "ViewerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_ViewerProfileId",
                table: "roulettelogs",
                column: "ViewerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_roulettelogs_viewerprofiles_ViewerProfileId",
                table: "roulettelogs",
                column: "ViewerProfileId",
                principalTable: "viewerprofiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_roulettespins_viewerprofiles_ViewerProfileId",
                table: "roulettespins",
                column: "ViewerProfileId",
                principalTable: "viewerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roulettelogs_viewerprofiles_ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_roulettespins_viewerprofiles_ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropIndex(
                name: "IX_roulettespins_ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropIndex(
                name: "IX_roulettelogs_ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "ViewerProfileId",
                table: "roulettespins");

            migrationBuilder.DropColumn(
                name: "ViewerProfileId",
                table: "roulettelogs");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUid",
                table: "roulettespins",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViewerUid",
                table: "roulettelogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
