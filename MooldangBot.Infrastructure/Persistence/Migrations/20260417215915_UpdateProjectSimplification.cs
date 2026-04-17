using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectSimplification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_core_streamer_managers_global_viewers_global_viewer_id",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "fk_core_streamer_managers_streamer_profiles_streamer_profile_id",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "fk_func_cmd_unified_streamer_profiles_streamer_profile_id",
                table: "func_cmd_unified");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_global_viewers_global_viewer_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_roulette_items_roulette_item_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_streamer_profiles_streamer_profile_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_main_streamer_profiles_streamer_profile_id",
                table: "func_roulette_main");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_spins_streamer_profiles_streamer_profile_id",
                table: "func_roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_streamer_library_streamer_profiles_streamer_profil",
                table: "func_song_streamer_library");

            migrationBuilder.DropForeignKey(
                name: "fk_log_point_transactions_global_viewers_global_viewer_id",
                table: "log_point_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_point_transactions_streamer_profiles_streamer_profile_id",
                table: "log_point_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_avatar_settings_streamer_profiles_streamer_profile_id",
                table: "overlay_avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_components_streamer_profiles_streamer_profile_id",
                table: "overlay_components");

            migrationBuilder.DropForeignKey(
                name: "fk_song_book_main_streamer_profiles_streamer_profile_id",
                table: "song_book_main");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_omakases_streamer_profiles_streamer_profile_id",
                table: "song_list_omakases");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_streamer_profiles_streamer_profile_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_sessions_streamer_profiles_streamer_profile_id",
                table: "song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stats_point_daily_streamer_profiles_streamer_profile_id",
                table: "stats_point_daily");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_broadcast_history_logs_sys_broadcast_sessions_broadcast_",
                table: "sys_broadcast_history_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_chzzk_category_aliases_sys_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "fk_view_periodic_messages_streamer_profiles_streamer_profile_id",
                table: "view_periodic_messages");

            migrationBuilder.CreateTable(
                name: "sys_saga_command_executions",
                columns: table => new
                {
                    correlation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    current_state = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    streamer_uid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    viewer_uid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    viewer_nickname = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    charged_amount = table.Column<int>(type: "int", nullable: false),
                    cost_type = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_saga_command_executions", x => x.correlation_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "ix_sys_saga_command_executions_created_at",
                table: "sys_saga_command_executions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sys_saga_command_executions_current_state",
                table: "sys_saga_command_executions",
                column: "current_state");

            migrationBuilder.AddForeignKey(
                name: "fk_core_streamer_managers_core_global_viewers_global_viewer_id",
                table: "core_streamer_managers",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_core_streamer_managers_core_streamer_profiles_streamer_profi",
                table: "core_streamer_managers",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_cmd_unified_core_streamer_profiles_streamer_profile_id",
                table: "func_cmd_unified",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_core_global_viewers_global_viewer_id",
                table: "func_roulette_logs",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_core_streamer_profiles_streamer_profile_id",
                table: "func_roulette_logs",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_func_roulette_items_roulette_item_id",
                table: "func_roulette_logs",
                column: "roulette_item_id",
                principalTable: "func_roulette_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_main_core_streamer_profiles_streamer_profile_id",
                table: "func_roulette_main",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_spins_core_streamer_profiles_streamer_profile_",
                table: "func_roulette_spins",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_streamer_library_core_streamer_profiles_streamer_p",
                table: "func_song_streamer_library",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_point_transactions_core_global_viewers_global_viewer_id",
                table: "log_point_transactions",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_point_transactions_core_streamer_profiles_streamer_profi",
                table: "log_point_transactions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_overlay_avatar_settings_core_streamer_profiles_streamer_prof",
                table: "overlay_avatar_settings",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_overlay_components_core_streamer_profiles_streamer_profile_id",
                table: "overlay_components",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_book_main_core_streamer_profiles_streamer_profile_id",
                table: "song_book_main",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_omakases_core_streamer_profiles_streamer_profile_id",
                table: "song_list_omakases",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_core_streamer_profiles_streamer_profile_id",
                table: "song_list_queues",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_book_main_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_book_main",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_sessions_core_streamer_profiles_streamer_profile_id",
                table: "song_list_sessions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stats_point_daily_core_streamer_profiles_streamer_profile_id",
                table: "stats_point_daily",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_broadcast_history_logs_broadcast_sessions_broadcast_sess",
                table: "sys_broadcast_history_logs",
                column: "broadcast_session_id",
                principalTable: "sys_broadcast_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_chzzk_category_aliases_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases",
                column: "category_id",
                principalTable: "sys_chzzk_categories",
                principalColumn: "category_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_view_periodic_messages_core_streamer_profiles_streamer_profi",
                table: "view_periodic_messages",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_core_streamer_managers_core_global_viewers_global_viewer_id",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "fk_core_streamer_managers_core_streamer_profiles_streamer_profi",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "fk_func_cmd_unified_core_streamer_profiles_streamer_profile_id",
                table: "func_cmd_unified");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_core_global_viewers_global_viewer_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_core_streamer_profiles_streamer_profile_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_logs_func_roulette_items_roulette_item_id",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_main_core_streamer_profiles_streamer_profile_id",
                table: "func_roulette_main");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_spins_core_streamer_profiles_streamer_profile_",
                table: "func_roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_streamer_library_core_streamer_profiles_streamer_p",
                table: "func_song_streamer_library");

            migrationBuilder.DropForeignKey(
                name: "fk_log_point_transactions_core_global_viewers_global_viewer_id",
                table: "log_point_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_point_transactions_core_streamer_profiles_streamer_profi",
                table: "log_point_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_avatar_settings_core_streamer_profiles_streamer_prof",
                table: "overlay_avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_components_core_streamer_profiles_streamer_profile_id",
                table: "overlay_components");

            migrationBuilder.DropForeignKey(
                name: "fk_song_book_main_core_streamer_profiles_streamer_profile_id",
                table: "song_book_main");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_omakases_core_streamer_profiles_streamer_profile_id",
                table: "song_list_omakases");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_core_streamer_profiles_streamer_profile_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_book_main_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_sessions_core_streamer_profiles_streamer_profile_id",
                table: "song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stats_point_daily_core_streamer_profiles_streamer_profile_id",
                table: "stats_point_daily");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_broadcast_history_logs_broadcast_sessions_broadcast_sess",
                table: "sys_broadcast_history_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_chzzk_category_aliases_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "fk_view_periodic_messages_core_streamer_profiles_streamer_profi",
                table: "view_periodic_messages");

            migrationBuilder.DropTable(
                name: "sys_saga_command_executions");

            migrationBuilder.AddForeignKey(
                name: "fk_core_streamer_managers_global_viewers_global_viewer_id",
                table: "core_streamer_managers",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_core_streamer_managers_streamer_profiles_streamer_profile_id",
                table: "core_streamer_managers",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_cmd_unified_streamer_profiles_streamer_profile_id",
                table: "func_cmd_unified",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_global_viewers_global_viewer_id",
                table: "func_roulette_logs",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_roulette_items_roulette_item_id",
                table: "func_roulette_logs",
                column: "roulette_item_id",
                principalTable: "func_roulette_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_logs_streamer_profiles_streamer_profile_id",
                table: "func_roulette_logs",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_main_streamer_profiles_streamer_profile_id",
                table: "func_roulette_main",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_spins_streamer_profiles_streamer_profile_id",
                table: "func_roulette_spins",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_streamer_library_streamer_profiles_streamer_profil",
                table: "func_song_streamer_library",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_point_transactions_global_viewers_global_viewer_id",
                table: "log_point_transactions",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_point_transactions_streamer_profiles_streamer_profile_id",
                table: "log_point_transactions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_overlay_avatar_settings_streamer_profiles_streamer_profile_id",
                table: "overlay_avatar_settings",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_overlay_components_streamer_profiles_streamer_profile_id",
                table: "overlay_components",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_book_main_streamer_profiles_streamer_profile_id",
                table: "song_book_main",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_omakases_streamer_profiles_streamer_profile_id",
                table: "song_list_omakases",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_book_main",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_streamer_profiles_streamer_profile_id",
                table: "song_list_queues",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_sessions_streamer_profiles_streamer_profile_id",
                table: "song_list_sessions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stats_point_daily_streamer_profiles_streamer_profile_id",
                table: "stats_point_daily",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_broadcast_history_logs_sys_broadcast_sessions_broadcast_",
                table: "sys_broadcast_history_logs",
                column: "broadcast_session_id",
                principalTable: "sys_broadcast_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_chzzk_category_aliases_sys_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases",
                column: "category_id",
                principalTable: "sys_chzzk_categories",
                principalColumn: "category_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_view_periodic_messages_streamer_profiles_streamer_profile_id",
                table: "view_periodic_messages",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
