using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Revision_v0_62_DomainPrefix_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_func_omakase_items_core_streamer_profiles_StreamerProfileId",
                table: "func_omakase_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_omakase_items",
                table: "func_omakase_items");

            migrationBuilder.RenameTable(
                name: "func_omakase_items",
                newName: "song_list_omakases");

            migrationBuilder.RenameIndex(
                name: "IX_func_omakase_items_StreamerProfileId",
                table: "song_list_omakases",
                newName: "IX_song_list_omakases_StreamerProfileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_song_list_omakases",
                table: "song_list_omakases",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_song_list_omakases_core_streamer_profiles_StreamerProfileId",
                table: "song_list_omakases",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_song_list_omakases_core_streamer_profiles_StreamerProfileId",
                table: "song_list_omakases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_song_list_omakases",
                table: "song_list_omakases");

            migrationBuilder.RenameTable(
                name: "song_list_omakases",
                newName: "func_omakase_items");

            migrationBuilder.RenameIndex(
                name: "IX_song_list_omakases_StreamerProfileId",
                table: "func_omakase_items",
                newName: "IX_func_omakase_items_StreamerProfileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_omakase_items",
                table: "func_omakase_items",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_func_omakase_items_core_streamer_profiles_StreamerProfileId",
                table: "func_omakase_items",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
