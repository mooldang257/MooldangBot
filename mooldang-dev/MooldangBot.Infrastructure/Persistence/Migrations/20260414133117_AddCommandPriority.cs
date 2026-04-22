using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword",
                table: "func_cmd_unified");

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "func_cmd_unified",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Command_Search",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "keyword", "is_active", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Command_Search",
                table: "func_cmd_unified");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "func_cmd_unified");

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "keyword" });
        }
    }
}
