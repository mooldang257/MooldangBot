using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPointSystemUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_core_global_viewers_viewer_uid_hash",
                table: "core_global_viewers",
                newName: "IX_GlobalViewer_ViewerUidHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_GlobalViewer_ViewerUidHash",
                table: "core_global_viewers",
                newName: "ix_core_global_viewers_viewer_uid_hash");
        }
    }
}
