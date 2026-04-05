using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Slug_And_System_Refinement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "overlay_token_version",
                table: "core_streamer_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "core_streamer_profiles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "log_command_executions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    is_success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    error_message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    donation_amount = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_command_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_log_command_executions_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_log_command_executions_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "log_point_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    balance_after = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_point_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_log_point_transactions_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_log_point_transactions_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "stats_point_daily",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    total_earned = table.Column<long>(type: "bigint", nullable: false),
                    total_spent = table.Column<long>(type: "bigint", nullable: false),
                    unique_viewer_count = table.Column<int>(type: "int", nullable: false),
                    top_command_stats_json = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stats_point_daily", x => x.id);
                    table.ForeignKey(
                        name: "fk_stats_point_daily_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "stats_roulette_audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    roulette_id = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    theoretical_probability = table.Column<double>(type: "double", nullable: false),
                    total_spins = table.Column<int>(type: "int", nullable: false),
                    win_count = table.Column<int>(type: "int", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stats_roulette_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_stats_roulette_audit_func_roulette_main_roulette_id",
                        column: x => x.roulette_id,
                        principalTable: "func_roulette_main",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_streamer_preferences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    preference_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preference_value = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_streamer_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_sys_streamer_preferences_core_streamer_profiles_streamer_pro",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 1,
                column: "keyword",
                value: "$(포인트)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 2,
                column: "keyword",
                value: "$(닉네임)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 3,
                column: "keyword",
                value: "$(방제)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 4,
                column: "keyword",
                value: "$(카테고리)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 5,
                column: "keyword",
                value: "$(공지)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 6,
                column: "keyword",
                value: "$(연속출석일수)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 7,
                column: "keyword",
                value: "$(누적출석일수)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 8,
                column: "keyword",
                value: "$(마지막출석일)");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 10,
                column: "keyword",
                value: "$(송리스트)");

            migrationBuilder.CreateIndex(
                name: "ix_core_streamer_profiles_slug",
                table: "core_streamer_profiles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_log_command_executions_global_viewer_id",
                table: "log_command_executions",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_log_command_executions_keyword_created_at",
                table: "log_command_executions",
                columns: new[] { "keyword", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_log_command_executions_streamer_profile_id_created_at",
                table: "log_command_executions",
                columns: new[] { "streamer_profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_log_point_transactions_global_viewer_id_created_at",
                table: "log_point_transactions",
                columns: new[] { "global_viewer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_log_point_transactions_streamer_profile_id_created_at",
                table: "log_point_transactions",
                columns: new[] { "streamer_profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stats_point_daily_streamer_profile_id_date",
                table: "stats_point_daily",
                columns: new[] { "streamer_profile_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stats_roulette_audit_roulette_id_item_name",
                table: "stats_roulette_audit",
                columns: new[] { "roulette_id", "item_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sys_streamer_preferences_streamer_profile_id_preference_key",
                table: "sys_streamer_preferences",
                columns: new[] { "streamer_profile_id", "preference_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_command_executions");

            migrationBuilder.DropTable(
                name: "log_point_transactions");

            migrationBuilder.DropTable(
                name: "stats_point_daily");

            migrationBuilder.DropTable(
                name: "stats_roulette_audit");

            migrationBuilder.DropTable(
                name: "sys_streamer_preferences");

            migrationBuilder.DropIndex(
                name: "ix_core_streamer_profiles_slug",
                table: "core_streamer_profiles");

            migrationBuilder.DropColumn(
                name: "overlay_token_version",
                table: "core_streamer_profiles");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "core_streamer_profiles");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 1,
                column: "keyword",
                value: "{포인트}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 2,
                column: "keyword",
                value: "{닉네임}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 3,
                column: "keyword",
                value: "{방제}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 4,
                column: "keyword",
                value: "{카테고리}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 5,
                column: "keyword",
                value: "{공지}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 6,
                column: "keyword",
                value: "{연속출석일수}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 7,
                column: "keyword",
                value: "{누적출석일수}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 8,
                column: "keyword",
                value: "{마지막출석일}");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "id",
                keyValue: 10,
                column: "keyword",
                value: "{송리스트}");
        }
    }
}
