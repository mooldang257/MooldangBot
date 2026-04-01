using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSongV45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_streamermanagers_ManagerChzzkUid",
                table: "streamermanagers");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_ChzzkUid_Id",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_ChzzkUid_Status_CreatedAt",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songlistsessions_ChzzkUid_IsActive",
                table: "songlistsessions");

            migrationBuilder.DropIndex(
                name: "IX_songbooks_ChzzkUid_Id",
                table: "songbooks");

            migrationBuilder.DropIndex(
                name: "IX_overlaypresets_ChzzkUid",
                table: "overlaypresets");

            migrationBuilder.DropColumn(
                name: "ChzzkUid",
                table: "songqueues");

            migrationBuilder.DropColumn(
                name: "ChzzkUid",
                table: "songlistsessions");

            migrationBuilder.DropColumn(
                name: "ChzzkUid",
                table: "songbooks");

            migrationBuilder.AddColumn<int>(
                name: "GlobalViewerId",
                table: "songqueues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "songqueues",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "songlistsessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "songbooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_GlobalViewerId",
                table: "songqueues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId_Id",
                table: "songqueues",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId_Status_CreatedAt",
                table: "songqueues",
                columns: new[] { "StreamerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_songlistsessions_StreamerProfileId_IsActive",
                table: "songlistsessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_songbooks_StreamerProfileId_Id",
                table: "songbooks",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_songbooks_streamerprofiles_StreamerProfileId",
                table: "songbooks",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_songlistsessions_streamerprofiles_StreamerProfileId",
                table: "songlistsessions",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_songqueues_globalviewers_GlobalViewerId",
                table: "songqueues",
                column: "GlobalViewerId",
                principalTable: "globalviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_songqueues_streamerprofiles_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_songbooks_streamerprofiles_StreamerProfileId",
                table: "songbooks");

            migrationBuilder.DropForeignKey(
                name: "FK_songlistsessions_streamerprofiles_StreamerProfileId",
                table: "songlistsessions");

            migrationBuilder.DropForeignKey(
                name: "FK_songqueues_globalviewers_GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropForeignKey(
                name: "FK_songqueues_streamerprofiles_StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId_Id",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_StreamerProfileId_Status_CreatedAt",
                table: "songqueues");

            migrationBuilder.DropIndex(
                name: "IX_songlistsessions_StreamerProfileId_IsActive",
                table: "songlistsessions");

            migrationBuilder.DropIndex(
                name: "IX_songbooks_StreamerProfileId_Id",
                table: "songbooks");

            migrationBuilder.DropColumn(
                name: "GlobalViewerId",
                table: "songqueues");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songqueues");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songlistsessions");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "songbooks");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songqueues",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songlistsessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "songbooks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_ManagerChzzkUid",
                table: "streamermanagers",
                column: "ManagerChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Id",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Status_CreatedAt",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_songlistsessions_ChzzkUid_IsActive",
                table: "songlistsessions",
                columns: new[] { "ChzzkUid", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_songbooks_ChzzkUid_Id",
                table: "songbooks",
                columns: new[] { "ChzzkUid", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_overlaypresets_ChzzkUid",
                table: "overlaypresets",
                column: "ChzzkUid");
        }
    }
}
