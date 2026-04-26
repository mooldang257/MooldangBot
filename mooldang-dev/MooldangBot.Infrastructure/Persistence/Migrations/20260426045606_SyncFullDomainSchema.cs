using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncFullDomainSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_items_func_roulette_main_roulette_id",
                table: "func_roulette_items");

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
                name: "fk_iamf_parhos_cycles_streamer_profiles_streamer_profile_id",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_scenarios_streamer_profiles_streamer_profile_id",
                table: "iamf_scenarios");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_streamer_settings_streamer_profiles_streamer_profile_id",
                table: "iamf_streamer_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_vibration_logs_streamer_profiles_streamer_profile_id",
                table: "iamf_vibration_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_log_chat_interactions_streamer_profiles_streamer_profile_id",
                table: "log_chat_interactions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_command_executions_global_viewers_global_viewer_id",
                table: "log_command_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_command_executions_streamer_profiles_streamer_profile_id",
                table: "log_command_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_avatar_settings_core_streamer_profiles_streamer_prof",
                table: "overlay_avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_components_core_streamer_profiles_streamer_profile_id",
                table: "overlay_components");

            migrationBuilder.DropForeignKey(
                name: "fk_overlay_presets_streamer_profiles_streamer_profile_id",
                table: "overlay_presets");

            migrationBuilder.DropForeignKey(
                name: "fk_song_books_core_streamer_profiles_streamer_profile_id",
                table: "song_books");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_omakases_core_streamer_profiles_streamer_profile_id",
                table: "song_list_omakases");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_core_global_viewers_global_viewer_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_core_streamer_profiles_streamer_profile_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_sessions_core_streamer_profiles_streamer_profile_id",
                table: "song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_sound_assets_streamer_profiles_streamer_profile_id",
                table: "sound_assets");

            migrationBuilder.DropForeignKey(
                name: "fk_stats_point_daily_core_streamer_profiles_streamer_profile_id",
                table: "stats_point_daily");

            migrationBuilder.DropForeignKey(
                name: "fk_streamer_knowledges_streamer_profiles_streamer_profile_id",
                table: "streamer_knowledges");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_broadcast_history_logs_broadcast_sessions_broadcast_sess",
                table: "sys_broadcast_history_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_chzzk_category_aliases_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "fk_view_periodic_messages_core_streamer_profiles_streamer_profi",
                table: "view_periodic_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_donations_global_viewers_global_viewer_id",
                table: "viewer_donations");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_donations_streamer_profiles_streamer_profile_id",
                table: "viewer_donations");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_donations_history_global_viewers_global_viewer_id",
                table: "viewer_donations_history");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_donations_history_streamer_profiles_streamer_profile_",
                table: "viewer_donations_history");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_points_global_viewers_global_viewer_id",
                table: "viewer_points");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_points_streamer_profiles_streamer_profile_id",
                table: "viewer_points");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_relations_global_viewers_global_viewer_id",
                table: "viewer_relations");

            migrationBuilder.DropForeignKey(
                name: "fk_viewer_relations_streamer_profiles_streamer_profile_id",
                table: "viewer_relations");

            migrationBuilder.RenameTable(
                name: "stats_roulette_audit",
                newName: "log_roulette_stats");

            migrationBuilder.DropPrimaryKey(
                name: "pk_viewer_relations",
                table: "viewer_relations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_viewer_points",
                table: "viewer_points");

            migrationBuilder.DropPrimaryKey(
                name: "pk_viewer_donations_history",
                table: "viewer_donations_history");

            migrationBuilder.DropPrimaryKey(
                name: "pk_viewer_donations",
                table: "viewer_donations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_view_periodic_messages",
                table: "view_periodic_messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_broadcast_history_logs",
                table: "sys_broadcast_history_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_streamer_knowledges",
                table: "streamer_knowledges");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stats_point_daily",
                table: "stats_point_daily");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sound_assets",
                table: "sound_assets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_list_sessions",
                table: "song_list_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_list_queues",
                table: "song_list_queues");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_list_omakases",
                table: "song_list_omakases");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_books",
                table: "song_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_overlay_presets",
                table: "overlay_presets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_overlay_components",
                table: "overlay_components");

            migrationBuilder.DropPrimaryKey(
                name: "pk_overlay_avatar_settings",
                table: "overlay_avatar_settings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_iamf_vibration_logs",
                table: "iamf_vibration_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_roulette_logs",
                table: "func_roulette_logs");

            migrationBuilder.RenameTable(
                name: "viewer_relations",
                newName: "core_viewer_relations");

            migrationBuilder.RenameTable(
                name: "viewer_points",
                newName: "func_viewer_points");

            migrationBuilder.RenameTable(
                name: "viewer_donations_history",
                newName: "func_viewer_donation_histories");

            migrationBuilder.RenameTable(
                name: "viewer_donations",
                newName: "func_viewer_donations");

            migrationBuilder.RenameTable(
                name: "view_periodic_messages",
                newName: "sys_periodic_messages");

            migrationBuilder.RenameTable(
                name: "sys_broadcast_history_logs",
                newName: "log_broadcast_history");

            migrationBuilder.RenameTable(
                name: "streamer_knowledges",
                newName: "sys_streamer_knowledges");

            migrationBuilder.RenameTable(
                name: "stats_point_daily",
                newName: "log_point_daily_summaries");

            migrationBuilder.RenameTable(
                name: "sound_assets",
                newName: "func_sound_assets");

            migrationBuilder.RenameTable(
                name: "song_list_sessions",
                newName: "func_song_list_sessions");

            migrationBuilder.RenameTable(
                name: "song_list_queues",
                newName: "func_song_list_queues");

            migrationBuilder.RenameTable(
                name: "song_list_omakases",
                newName: "func_song_list_omakases");

            migrationBuilder.RenameTable(
                name: "song_books",
                newName: "func_song_books");

            migrationBuilder.RenameTable(
                name: "overlay_presets",
                newName: "sys_overlay_presets");

            migrationBuilder.RenameTable(
                name: "overlay_components",
                newName: "sys_shared_components");

            migrationBuilder.RenameTable(
                name: "overlay_avatar_settings",
                newName: "sys_avatar_settings");

            migrationBuilder.RenameTable(
                name: "iamf_vibration_logs",
                newName: "log_iamf_vibrations");

            migrationBuilder.RenameTable(
                name: "func_roulette_logs",
                newName: "log_roulette_results");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_relations_streamer_profile_id_global_viewer_id",
                table: "core_viewer_relations",
                newName: "ix_core_viewer_relations_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_relations_global_viewer_id",
                table: "core_viewer_relations",
                newName: "ix_core_viewer_relations_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_points_streamer_profile_id_global_viewer_id",
                table: "func_viewer_points",
                newName: "ix_func_viewer_points_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_points_global_viewer_id",
                table: "func_viewer_points",
                newName: "ix_func_viewer_points_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_donations_history_streamer_profile_id",
                table: "func_viewer_donation_histories",
                newName: "ix_func_viewer_donation_histories_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_donations_history_platform_transaction_id",
                table: "func_viewer_donation_histories",
                newName: "ix_func_viewer_donation_histories_platform_transaction_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_donations_history_global_viewer_id",
                table: "func_viewer_donation_histories",
                newName: "ix_func_viewer_donation_histories_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_donations_streamer_profile_id_global_viewer_id",
                table: "func_viewer_donations",
                newName: "ix_func_viewer_donations_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_viewer_donations_global_viewer_id",
                table: "func_viewer_donations",
                newName: "ix_func_viewer_donations_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_view_periodic_messages_streamer_profile_id",
                table: "sys_periodic_messages",
                newName: "ix_sys_periodic_messages_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_sys_broadcast_history_logs_broadcast_session_id_log_date",
                table: "log_broadcast_history",
                newName: "ix_log_broadcast_history_broadcast_session_id_log_date");

            migrationBuilder.RenameIndex(
                name: "ix_streamer_knowledges_streamer_profile_id_keyword",
                table: "sys_streamer_knowledges",
                newName: "ix_sys_streamer_knowledges_streamer_profile_id_keyword");

            migrationBuilder.RenameIndex(
                name: "ix_stats_point_daily_streamer_profile_id_date",
                table: "log_point_daily_summaries",
                newName: "ix_log_point_daily_summaries_streamer_profile_id_date");

            migrationBuilder.RenameIndex(
                name: "ix_sound_assets_streamer_profile_id",
                table: "func_sound_assets",
                newName: "ix_func_sound_assets_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_sessions_streamer_profile_id_is_active",
                table: "func_song_list_sessions",
                newName: "ix_func_song_list_sessions_streamer_profile_id_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_streamer_profile_id_status_created_at",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_streamer_profile_id_status_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_streamer_profile_id_id",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_streamer_profile_id_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_streamer_profile_id",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_song_library_id",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_song_library_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_song_book_id",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_song_book_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_queues_global_viewer_id",
                table: "func_song_list_queues",
                newName: "ix_func_song_list_queues_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_list_omakases_streamer_profile_id",
                table: "func_song_list_omakases",
                newName: "ix_func_song_list_omakases_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_title_chosung",
                table: "func_song_books",
                newName: "ix_func_song_books_title_chosung");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_title",
                table: "func_song_books",
                newName: "ix_func_song_books_title");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_streamer_profile_id_id",
                table: "func_song_books",
                newName: "ix_func_song_books_streamer_profile_id_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_song_library_id",
                table: "func_song_books",
                newName: "ix_func_song_books_song_library_id");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_is_requestable",
                table: "func_song_books",
                newName: "ix_func_song_books_is_requestable");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_category",
                table: "func_song_books",
                newName: "ix_func_song_books_category");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_alias",
                table: "func_song_books",
                newName: "ix_func_song_books_alias");

            migrationBuilder.RenameIndex(
                name: "ix_overlay_presets_streamer_profile_id",
                table: "sys_overlay_presets",
                newName: "ix_sys_overlay_presets_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_overlay_components_streamer_profile_id",
                table: "sys_shared_components",
                newName: "ix_sys_shared_components_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_overlay_avatar_settings_streamer_profile_id",
                table: "sys_avatar_settings",
                newName: "ix_sys_avatar_settings_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_iamf_vibration_logs_streamer_profile_id_created_at",
                table: "log_iamf_vibrations",
                newName: "ix_log_iamf_vibrations_streamer_profile_id_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_func_roulette_logs_streamer_profile_id_global_viewer_id",
                table: "log_roulette_results",
                newName: "ix_log_roulette_results_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_roulette_logs_roulette_item_id",
                table: "log_roulette_results",
                newName: "ix_log_roulette_results_roulette_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_roulette_logs_roulette_id",
                table: "log_roulette_results",
                newName: "ix_log_roulette_results_roulette_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_roulette_logs_global_viewer_id",
                table: "log_roulette_results",
                newName: "ix_log_roulette_results_global_viewer_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_core_viewer_relations",
                table: "core_viewer_relations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_viewer_points",
                table: "func_viewer_points",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_viewer_donation_histories",
                table: "func_viewer_donation_histories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_viewer_donations",
                table: "func_viewer_donations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_periodic_messages",
                table: "sys_periodic_messages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_broadcast_history",
                table: "log_broadcast_history",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_streamer_knowledges",
                table: "sys_streamer_knowledges",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_point_daily_summaries",
                table: "log_point_daily_summaries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_sound_assets",
                table: "func_sound_assets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_song_list_sessions",
                table: "func_song_list_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_song_list_queues",
                table: "func_song_list_queues",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_song_list_omakases",
                table: "func_song_list_omakases",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_song_books",
                table: "func_song_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_overlay_presets",
                table: "sys_overlay_presets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_shared_components",
                table: "sys_shared_components",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_avatar_settings",
                table: "sys_avatar_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_iamf_vibrations",
                table: "log_iamf_vibrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_roulette_results",
                table: "log_roulette_results",
                column: "id");



            migrationBuilder.CreateIndex(
                name: "ix_log_roulette_stats_roulette_id_item_name",
                table: "log_roulette_stats",
                columns: new[] { "roulette_id", "item_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_core_viewer_relations_core_global_viewers_global_viewer_id",
                table: "core_viewer_relations",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_core_viewer_relations_core_streamer_profiles_streamer_profil",
                table: "core_viewer_relations",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_items_func_roulettes_roulette_id",
                table: "func_roulette_items",
                column: "roulette_id",
                principalTable: "func_roulette_main",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_books_core_streamer_profiles_streamer_profile_id",
                table: "func_song_books",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_list_omakases_core_streamer_profiles_streamer_prof",
                table: "func_song_list_omakases",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_list_queues_core_global_viewers_global_viewer_id",
                table: "func_song_list_queues",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_list_queues_core_streamer_profiles_streamer_profil",
                table: "func_song_list_queues",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_list_queues_func_song_books_song_book_id",
                table: "func_song_list_queues",
                column: "song_book_id",
                principalTable: "func_song_books",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_func_song_list_sessions_core_streamer_profiles_streamer_prof",
                table: "func_song_list_sessions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_sound_assets_core_streamer_profiles_streamer_profile_id",
                table: "func_sound_assets",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_donation_histories_core_global_viewers_global_vi",
                table: "func_viewer_donation_histories",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_donation_histories_core_streamer_profiles_stream",
                table: "func_viewer_donation_histories",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_donations_core_global_viewers_global_viewer_id",
                table: "func_viewer_donations",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_donations_core_streamer_profiles_streamer_profil",
                table: "func_viewer_donations",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_points_core_global_viewers_global_viewer_id",
                table: "func_viewer_points",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_func_viewer_points_core_streamer_profiles_streamer_profile_id",
                table: "func_viewer_points",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_parhos_cycles_core_streamer_profiles_streamer_profile_id",
                table: "iamf_parhos_cycles",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_scenarios_core_streamer_profiles_streamer_profile_id",
                table: "iamf_scenarios",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_streamer_settings_core_streamer_profiles_streamer_profi",
                table: "iamf_streamer_settings",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_broadcast_history_sys_broadcast_sessions_broadcast_sessi",
                table: "log_broadcast_history",
                column: "broadcast_session_id",
                principalTable: "sys_broadcast_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_chat_interactions_core_streamer_profiles_streamer_profil",
                table: "log_chat_interactions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_command_executions_core_global_viewers_global_viewer_id",
                table: "log_command_executions",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_command_executions_core_streamer_profiles_streamer_profi",
                table: "log_command_executions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_iamf_vibrations_core_streamer_profiles_streamer_profile_",
                table: "log_iamf_vibrations",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_log_point_daily_summaries_core_streamer_profiles_streamer_pr",
                table: "log_point_daily_summaries",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_roulette_results_core_global_viewers_global_viewer_id",
                table: "log_roulette_results",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_log_roulette_results_core_streamer_profiles_streamer_profile",
                table: "log_roulette_results",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_roulette_results_func_roulette_items_roulette_item_id",
                table: "log_roulette_results",
                column: "roulette_item_id",
                principalTable: "func_roulette_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_avatar_settings_core_streamer_profiles_streamer_profile_",
                table: "sys_avatar_settings",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
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
                name: "fk_sys_overlay_presets_core_streamer_profiles_streamer_profile_",
                table: "sys_overlay_presets",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_periodic_messages_core_streamer_profiles_streamer_profil",
                table: "sys_periodic_messages",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_shared_components_core_streamer_profiles_streamer_profil",
                table: "sys_shared_components",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sys_streamer_knowledges_core_streamer_profiles_streamer_prof",
                table: "sys_streamer_knowledges",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_core_viewer_relations_core_global_viewers_global_viewer_id",
                table: "core_viewer_relations");

            migrationBuilder.DropForeignKey(
                name: "fk_core_viewer_relations_core_streamer_profiles_streamer_profil",
                table: "core_viewer_relations");

            migrationBuilder.DropForeignKey(
                name: "fk_func_roulette_items_func_roulettes_roulette_id",
                table: "func_roulette_items");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_books_core_streamer_profiles_streamer_profile_id",
                table: "func_song_books");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_list_omakases_core_streamer_profiles_streamer_prof",
                table: "func_song_list_omakases");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_list_queues_core_global_viewers_global_viewer_id",
                table: "func_song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_list_queues_core_streamer_profiles_streamer_profil",
                table: "func_song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_list_queues_func_song_books_song_book_id",
                table: "func_song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "fk_func_song_list_sessions_core_streamer_profiles_streamer_prof",
                table: "func_song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_func_sound_assets_core_streamer_profiles_streamer_profile_id",
                table: "func_sound_assets");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_donation_histories_core_global_viewers_global_vi",
                table: "func_viewer_donation_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_donation_histories_core_streamer_profiles_stream",
                table: "func_viewer_donation_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_donations_core_global_viewers_global_viewer_id",
                table: "func_viewer_donations");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_donations_core_streamer_profiles_streamer_profil",
                table: "func_viewer_donations");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_points_core_global_viewers_global_viewer_id",
                table: "func_viewer_points");

            migrationBuilder.DropForeignKey(
                name: "fk_func_viewer_points_core_streamer_profiles_streamer_profile_id",
                table: "func_viewer_points");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_parhos_cycles_core_streamer_profiles_streamer_profile_id",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_scenarios_core_streamer_profiles_streamer_profile_id",
                table: "iamf_scenarios");

            migrationBuilder.DropForeignKey(
                name: "fk_iamf_streamer_settings_core_streamer_profiles_streamer_profi",
                table: "iamf_streamer_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_log_broadcast_history_sys_broadcast_sessions_broadcast_sessi",
                table: "log_broadcast_history");

            migrationBuilder.DropForeignKey(
                name: "fk_log_chat_interactions_core_streamer_profiles_streamer_profil",
                table: "log_chat_interactions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_command_executions_core_global_viewers_global_viewer_id",
                table: "log_command_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_command_executions_core_streamer_profiles_streamer_profi",
                table: "log_command_executions");

            migrationBuilder.DropForeignKey(
                name: "fk_log_iamf_vibrations_core_streamer_profiles_streamer_profile_",
                table: "log_iamf_vibrations");

            migrationBuilder.DropForeignKey(
                name: "fk_log_point_daily_summaries_core_streamer_profiles_streamer_pr",
                table: "log_point_daily_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_log_roulette_results_core_global_viewers_global_viewer_id",
                table: "log_roulette_results");

            migrationBuilder.DropForeignKey(
                name: "fk_log_roulette_results_core_streamer_profiles_streamer_profile",
                table: "log_roulette_results");

            migrationBuilder.DropForeignKey(
                name: "fk_log_roulette_results_func_roulette_items_roulette_item_id",
                table: "log_roulette_results");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_avatar_settings_core_streamer_profiles_streamer_profile_",
                table: "sys_avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_chzzk_category_aliases_sys_chzzk_categories_category_id",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_overlay_presets_core_streamer_profiles_streamer_profile_",
                table: "sys_overlay_presets");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_periodic_messages_core_streamer_profiles_streamer_profil",
                table: "sys_periodic_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_shared_components_core_streamer_profiles_streamer_profil",
                table: "sys_shared_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sys_streamer_knowledges_core_streamer_profiles_streamer_prof",
                table: "sys_streamer_knowledges");

            migrationBuilder.RenameTable(
                name: "log_roulette_stats",
                newName: "stats_roulette_audit");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_streamer_knowledges",
                table: "sys_streamer_knowledges");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_shared_components",
                table: "sys_shared_components");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_periodic_messages",
                table: "sys_periodic_messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_overlay_presets",
                table: "sys_overlay_presets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sys_avatar_settings",
                table: "sys_avatar_settings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_roulette_results",
                table: "log_roulette_results");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_point_daily_summaries",
                table: "log_point_daily_summaries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_iamf_vibrations",
                table: "log_iamf_vibrations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_broadcast_history",
                table: "log_broadcast_history");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_viewer_points",
                table: "func_viewer_points");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_viewer_donations",
                table: "func_viewer_donations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_viewer_donation_histories",
                table: "func_viewer_donation_histories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_sound_assets",
                table: "func_sound_assets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_song_list_sessions",
                table: "func_song_list_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_song_list_queues",
                table: "func_song_list_queues");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_song_list_omakases",
                table: "func_song_list_omakases");

            migrationBuilder.DropPrimaryKey(
                name: "pk_func_song_books",
                table: "func_song_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_core_viewer_relations",
                table: "core_viewer_relations");

            migrationBuilder.RenameTable(
                name: "sys_streamer_knowledges",
                newName: "streamer_knowledges");

            migrationBuilder.RenameTable(
                name: "sys_shared_components",
                newName: "overlay_components");

            migrationBuilder.RenameTable(
                name: "sys_periodic_messages",
                newName: "view_periodic_messages");

            migrationBuilder.RenameTable(
                name: "sys_overlay_presets",
                newName: "overlay_presets");

            migrationBuilder.RenameTable(
                name: "sys_avatar_settings",
                newName: "overlay_avatar_settings");

            migrationBuilder.RenameTable(
                name: "log_roulette_results",
                newName: "func_roulette_logs");

            migrationBuilder.RenameTable(
                name: "log_point_daily_summaries",
                newName: "stats_point_daily");

            migrationBuilder.RenameTable(
                name: "log_iamf_vibrations",
                newName: "iamf_vibration_logs");

            migrationBuilder.RenameTable(
                name: "log_broadcast_history",
                newName: "sys_broadcast_history_logs");

            migrationBuilder.RenameTable(
                name: "func_viewer_points",
                newName: "viewer_points");

            migrationBuilder.RenameTable(
                name: "func_viewer_donations",
                newName: "viewer_donations");

            migrationBuilder.RenameTable(
                name: "func_viewer_donation_histories",
                newName: "viewer_donations_history");

            migrationBuilder.RenameTable(
                name: "func_sound_assets",
                newName: "sound_assets");

            migrationBuilder.RenameTable(
                name: "func_song_list_sessions",
                newName: "song_list_sessions");

            migrationBuilder.RenameTable(
                name: "func_song_list_queues",
                newName: "song_list_queues");

            migrationBuilder.RenameTable(
                name: "func_song_list_omakases",
                newName: "song_list_omakases");

            migrationBuilder.RenameTable(
                name: "func_song_books",
                newName: "song_books");

            migrationBuilder.RenameTable(
                name: "core_viewer_relations",
                newName: "viewer_relations");

            migrationBuilder.RenameIndex(
                name: "ix_sys_streamer_knowledges_streamer_profile_id_keyword",
                table: "streamer_knowledges",
                newName: "ix_streamer_knowledges_streamer_profile_id_keyword");

            migrationBuilder.RenameIndex(
                name: "ix_sys_shared_components_streamer_profile_id",
                table: "overlay_components",
                newName: "ix_overlay_components_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_sys_periodic_messages_streamer_profile_id",
                table: "view_periodic_messages",
                newName: "ix_view_periodic_messages_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_sys_overlay_presets_streamer_profile_id",
                table: "overlay_presets",
                newName: "ix_overlay_presets_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_sys_avatar_settings_streamer_profile_id",
                table: "overlay_avatar_settings",
                newName: "ix_overlay_avatar_settings_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_log_roulette_results_streamer_profile_id_global_viewer_id",
                table: "func_roulette_logs",
                newName: "ix_func_roulette_logs_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_log_roulette_results_roulette_item_id",
                table: "func_roulette_logs",
                newName: "ix_func_roulette_logs_roulette_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_log_roulette_results_roulette_id",
                table: "func_roulette_logs",
                newName: "ix_func_roulette_logs_roulette_id");

            migrationBuilder.RenameIndex(
                name: "ix_log_roulette_results_global_viewer_id",
                table: "func_roulette_logs",
                newName: "ix_func_roulette_logs_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_log_point_daily_summaries_streamer_profile_id_date",
                table: "stats_point_daily",
                newName: "ix_stats_point_daily_streamer_profile_id_date");

            migrationBuilder.RenameIndex(
                name: "ix_log_iamf_vibrations_streamer_profile_id_created_at",
                table: "iamf_vibration_logs",
                newName: "ix_iamf_vibration_logs_streamer_profile_id_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_log_broadcast_history_broadcast_session_id_log_date",
                table: "sys_broadcast_history_logs",
                newName: "ix_sys_broadcast_history_logs_broadcast_session_id_log_date");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_points_streamer_profile_id_global_viewer_id",
                table: "viewer_points",
                newName: "ix_viewer_points_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_points_global_viewer_id",
                table: "viewer_points",
                newName: "ix_viewer_points_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_donations_streamer_profile_id_global_viewer_id",
                table: "viewer_donations",
                newName: "ix_viewer_donations_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_donations_global_viewer_id",
                table: "viewer_donations",
                newName: "ix_viewer_donations_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_donation_histories_streamer_profile_id",
                table: "viewer_donations_history",
                newName: "ix_viewer_donations_history_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_donation_histories_platform_transaction_id",
                table: "viewer_donations_history",
                newName: "ix_viewer_donations_history_platform_transaction_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_viewer_donation_histories_global_viewer_id",
                table: "viewer_donations_history",
                newName: "ix_viewer_donations_history_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_sound_assets_streamer_profile_id",
                table: "sound_assets",
                newName: "ix_sound_assets_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_sessions_streamer_profile_id_is_active",
                table: "song_list_sessions",
                newName: "ix_song_list_sessions_streamer_profile_id_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_streamer_profile_id_status_created_at",
                table: "song_list_queues",
                newName: "ix_song_list_queues_streamer_profile_id_status_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_streamer_profile_id_id",
                table: "song_list_queues",
                newName: "ix_song_list_queues_streamer_profile_id_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_streamer_profile_id",
                table: "song_list_queues",
                newName: "ix_song_list_queues_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_song_library_id",
                table: "song_list_queues",
                newName: "ix_song_list_queues_song_library_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_song_book_id",
                table: "song_list_queues",
                newName: "ix_song_list_queues_song_book_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_queues_global_viewer_id",
                table: "song_list_queues",
                newName: "ix_song_list_queues_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_list_omakases_streamer_profile_id",
                table: "song_list_omakases",
                newName: "ix_song_list_omakases_streamer_profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_title_chosung",
                table: "song_books",
                newName: "ix_song_books_title_chosung");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_title",
                table: "song_books",
                newName: "ix_song_books_title");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_streamer_profile_id_id",
                table: "song_books",
                newName: "ix_song_books_streamer_profile_id_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_song_library_id",
                table: "song_books",
                newName: "ix_song_books_song_library_id");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_is_requestable",
                table: "song_books",
                newName: "ix_song_books_is_requestable");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_category",
                table: "song_books",
                newName: "ix_song_books_category");

            migrationBuilder.RenameIndex(
                name: "ix_func_song_books_alias",
                table: "song_books",
                newName: "ix_song_books_alias");

            migrationBuilder.RenameIndex(
                name: "ix_core_viewer_relations_streamer_profile_id_global_viewer_id",
                table: "viewer_relations",
                newName: "ix_viewer_relations_streamer_profile_id_global_viewer_id");

            migrationBuilder.RenameIndex(
                name: "ix_core_viewer_relations_global_viewer_id",
                table: "viewer_relations",
                newName: "ix_viewer_relations_global_viewer_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_streamer_knowledges",
                table: "streamer_knowledges",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_overlay_components",
                table: "overlay_components",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_view_periodic_messages",
                table: "view_periodic_messages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_overlay_presets",
                table: "overlay_presets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_overlay_avatar_settings",
                table: "overlay_avatar_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_func_roulette_logs",
                table: "func_roulette_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stats_point_daily",
                table: "stats_point_daily",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_iamf_vibration_logs",
                table: "iamf_vibration_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sys_broadcast_history_logs",
                table: "sys_broadcast_history_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_viewer_points",
                table: "viewer_points",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_viewer_donations",
                table: "viewer_donations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_viewer_donations_history",
                table: "viewer_donations_history",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sound_assets",
                table: "sound_assets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_list_sessions",
                table: "song_list_sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_list_queues",
                table: "song_list_queues",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_list_omakases",
                table: "song_list_omakases",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_books",
                table: "song_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_viewer_relations",
                table: "viewer_relations",
                column: "id");



            migrationBuilder.CreateIndex(
                name: "ix_stats_roulette_audit_roulette_id_item_name",
                table: "stats_roulette_audit",
                columns: new[] { "roulette_id", "item_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_func_roulette_items_func_roulette_main_roulette_id",
                table: "func_roulette_items",
                column: "roulette_id",
                principalTable: "func_roulette_main",
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
                name: "fk_iamf_parhos_cycles_streamer_profiles_streamer_profile_id",
                table: "iamf_parhos_cycles",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_scenarios_streamer_profiles_streamer_profile_id",
                table: "iamf_scenarios",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_streamer_settings_streamer_profiles_streamer_profile_id",
                table: "iamf_streamer_settings",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_iamf_vibration_logs_streamer_profiles_streamer_profile_id",
                table: "iamf_vibration_logs",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_log_chat_interactions_streamer_profiles_streamer_profile_id",
                table: "log_chat_interactions",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_command_executions_global_viewers_global_viewer_id",
                table: "log_command_executions",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_log_command_executions_streamer_profiles_streamer_profile_id",
                table: "log_command_executions",
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
                name: "fk_overlay_presets_streamer_profiles_streamer_profile_id",
                table: "overlay_presets",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_books_core_streamer_profiles_streamer_profile_id",
                table: "song_books",
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
                name: "fk_song_list_queues_core_global_viewers_global_viewer_id",
                table: "song_list_queues",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_core_streamer_profiles_streamer_profile_id",
                table: "song_list_queues",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_books",
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
                name: "fk_sound_assets_streamer_profiles_streamer_profile_id",
                table: "sound_assets",
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
                name: "fk_streamer_knowledges_streamer_profiles_streamer_profile_id",
                table: "streamer_knowledges",
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

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_donations_global_viewers_global_viewer_id",
                table: "viewer_donations",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_donations_streamer_profiles_streamer_profile_id",
                table: "viewer_donations",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_donations_history_global_viewers_global_viewer_id",
                table: "viewer_donations_history",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_donations_history_streamer_profiles_streamer_profile_",
                table: "viewer_donations_history",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_points_global_viewers_global_viewer_id",
                table: "viewer_points",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_points_streamer_profiles_streamer_profile_id",
                table: "viewer_points",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_relations_global_viewers_global_viewer_id",
                table: "viewer_relations",
                column: "global_viewer_id",
                principalTable: "core_global_viewers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_viewer_relations_streamer_profiles_streamer_profile_id",
                table: "viewer_relations",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
