using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUnifiedCommandV43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_chzzkuid_keyword",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_chzzkuid_TargetId",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "FeatureType",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "chzzkuid",
                table: "unifiedcommands");

            migrationBuilder.AddColumn<int>(
                name: "MasterCommandFeatureId",
                table: "unifiedcommands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "unifiedcommands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_MasterCommandFeatureId",
                table: "unifiedcommands",
                column: "MasterCommandFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_StreamerProfileId_keyword",
                table: "unifiedcommands",
                columns: new[] { "StreamerProfileId", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_StreamerProfileId_TargetId",
                table: "unifiedcommands",
                columns: new[] { "StreamerProfileId", "TargetId" });

            migrationBuilder.AddForeignKey(
                name: "FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId",
                table: "unifiedcommands",
                column: "MasterCommandFeatureId",
                principalTable: "master_commandfeatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unifiedcommands_streamerprofiles_StreamerProfileId",
                table: "unifiedcommands",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_unifiedcommands_master_commandfeatures_MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropForeignKey(
                name: "FK_unifiedcommands_streamerprofiles_StreamerProfileId",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_StreamerProfileId_keyword",
                table: "unifiedcommands");

            migrationBuilder.DropIndex(
                name: "IX_unifiedcommands_StreamerProfileId_TargetId",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "MasterCommandFeatureId",
                table: "unifiedcommands");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "unifiedcommands");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "unifiedcommands",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FeatureType",
                table: "unifiedcommands",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "chzzkuid",
                table: "unifiedcommands",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_keyword",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_TargetId",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "TargetId" });
        }
    }
}
