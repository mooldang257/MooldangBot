using MooldangBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToChzzkUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_streamerprofiles_ChzzkUid",
                table: "streamerprofiles",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_streamercommands_ChzzkUid",
                table: "streamercommands",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues",
                column: "ChzzkUid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_streamerprofiles_ChzzkUid",
                table: "streamerprofiles");

            migrationBuilder.DropIndex(
                name: "IX_streamercommands_ChzzkUid",
                table: "streamercommands");

            migrationBuilder.DropIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues");
        }
    }
}
