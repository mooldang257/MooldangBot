using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRemainingUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword_feature_type",
                table: "func_cmd_unified");

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword_feature_type",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "keyword", "feature_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword_feature_type",
                table: "func_cmd_unified");

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword_feature_type",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "keyword", "feature_type" },
                unique: true);
        }
    }
}
