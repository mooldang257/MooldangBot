using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Revision_v0_62_DomainPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_avatar_settings_streamer_profiles_StreamerProfileId",
                table: "avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_broadcast_sessions_streamer_profiles_StreamerProfileId",
                table: "broadcast_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_chzzk_category_aliases_chzzk_categories_CategoryId",
                table: "chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_genos_registry_streamer_profiles_StreamerProfileId",
                table: "iamf_genos_registry");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_parhos_cycles_streamer_profiles_StreamerProfileId",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_scenarios_streamer_profiles_StreamerProfileId",
                table: "iamf_scenarios");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_streamer_settings_streamer_profiles_StreamerProfileId",
                table: "iamf_streamer_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_vibration_logs_streamer_profiles_StreamerProfileId",
                table: "iamf_vibration_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_master_command_features_master_command_categories_CategoryId",
                table: "master_command_features");

            migrationBuilder.DropForeignKey(
                name: "FK_overlay_presets_streamer_profiles_StreamerProfileId",
                table: "overlay_presets");

            migrationBuilder.DropForeignKey(
                name: "FK_periodic_messages_streamer_profiles_StreamerProfileId",
                table: "periodic_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_items_roulettes_RouletteId",
                table: "roulette_items");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_logs_global_viewers_GlobalViewerId",
                table: "roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_logs_roulette_items_RouletteItemId",
                table: "roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_logs_streamer_profiles_StreamerProfileId",
                table: "roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_spins_global_viewers_GlobalViewerId",
                table: "roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "FK_roulette_spins_streamer_profiles_StreamerProfileId",
                table: "roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "FK_roulettes_streamer_profiles_StreamerProfileId",
                table: "roulettes");

            migrationBuilder.DropForeignKey(
                name: "FK_shared_components_streamer_profiles_StreamerProfileId",
                table: "shared_components");

            migrationBuilder.DropForeignKey(
                name: "FK_song_books_streamer_profiles_StreamerProfileId",
                table: "song_books");

            migrationBuilder.DropForeignKey(
                name: "FK_song_list_sessions_streamer_profiles_StreamerProfileId",
                table: "song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_song_queues_global_viewers_GlobalViewerId",
                table: "song_queues");

            migrationBuilder.DropForeignKey(
                name: "FK_song_queues_streamer_profiles_StreamerProfileId",
                table: "song_queues");

            migrationBuilder.DropForeignKey(
                name: "FK_streamer_knowledges_streamer_profiles_StreamerProfileId",
                table: "streamer_knowledges");

            migrationBuilder.DropForeignKey(
                name: "FK_streamer_managers_global_viewers_GlobalViewerId",
                table: "streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "FK_streamer_managers_streamer_profiles_StreamerProfileId",
                table: "streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "FK_streamer_omakases_streamer_profiles_StreamerProfileId",
                table: "streamer_omakases");

            migrationBuilder.DropForeignKey(
                name: "FK_unified_commands_master_command_features_MasterCommandFeatur~",
                table: "unified_commands");

            migrationBuilder.DropForeignKey(
                name: "FK_unified_commands_streamer_profiles_StreamerProfileId",
                table: "unified_commands");

            migrationBuilder.DropForeignKey(
                name: "FK_viewer_profiles_global_viewers_GlobalViewerId",
                table: "viewer_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_viewer_profiles_streamer_profiles_StreamerProfileId",
                table: "viewer_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_viewer_profiles",
                table: "viewer_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_unified_commands",
                table: "unified_commands");

            migrationBuilder.DropPrimaryKey(
                name: "PK_system_settings",
                table: "system_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streamer_profiles",
                table: "streamer_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streamer_omakases",
                table: "streamer_omakases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_streamer_managers",
                table: "streamer_managers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_song_queues",
                table: "song_queues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_song_books",
                table: "song_books");

            migrationBuilder.DropPrimaryKey(
                name: "PK_shared_components",
                table: "shared_components");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roulettes",
                table: "roulettes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roulette_spins",
                table: "roulette_spins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roulette_logs",
                table: "roulette_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roulette_items",
                table: "roulette_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_periodic_messages",
                table: "periodic_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_master_dynamic_variables",
                table: "master_dynamic_variables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_master_command_features",
                table: "master_command_features");

            migrationBuilder.DropPrimaryKey(
                name: "PK_master_command_categories",
                table: "master_command_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_global_viewers",
                table: "global_viewers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_chzzk_category_aliases",
                table: "chzzk_category_aliases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_chzzk_categories",
                table: "chzzk_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_broadcast_sessions",
                table: "broadcast_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_avatar_settings",
                table: "avatar_settings");

            migrationBuilder.RenameTable(
                name: "viewer_profiles",
                newName: "view_profiles");

            migrationBuilder.RenameTable(
                name: "unified_commands",
                newName: "func_cmd_unified");

            migrationBuilder.RenameTable(
                name: "system_settings",
                newName: "sys_settings");

            migrationBuilder.RenameTable(
                name: "streamer_profiles",
                newName: "core_streamer_profiles");

            migrationBuilder.RenameTable(
                name: "streamer_omakases",
                newName: "func_omakase_items");

            migrationBuilder.RenameTable(
                name: "streamer_managers",
                newName: "core_streamer_managers");

            migrationBuilder.RenameTable(
                name: "song_queues",
                newName: "song_list_queues");

            migrationBuilder.RenameTable(
                name: "song_books",
                newName: "song_book_main");

            migrationBuilder.RenameTable(
                name: "shared_components",
                newName: "overlay_components");

            migrationBuilder.RenameTable(
                name: "roulettes",
                newName: "func_roulette_main");

            migrationBuilder.RenameTable(
                name: "roulette_spins",
                newName: "func_roulette_spins");

            migrationBuilder.RenameTable(
                name: "roulette_logs",
                newName: "func_roulette_logs");

            migrationBuilder.RenameTable(
                name: "roulette_items",
                newName: "func_roulette_items");

            migrationBuilder.RenameTable(
                name: "periodic_messages",
                newName: "view_periodic_messages");

            migrationBuilder.RenameTable(
                name: "master_dynamic_variables",
                newName: "func_cmd_master_variables");

            migrationBuilder.RenameTable(
                name: "master_command_features",
                newName: "func_cmd_master_features");

            migrationBuilder.RenameTable(
                name: "master_command_categories",
                newName: "func_cmd_master_categories");

            migrationBuilder.RenameTable(
                name: "global_viewers",
                newName: "core_global_viewers");

            migrationBuilder.RenameTable(
                name: "chzzk_category_aliases",
                newName: "sys_chzzk_category_aliases");

            migrationBuilder.RenameTable(
                name: "chzzk_categories",
                newName: "sys_chzzk_categories");

            migrationBuilder.RenameTable(
                name: "broadcast_sessions",
                newName: "sys_broadcast_sessions");

            migrationBuilder.RenameTable(
                name: "avatar_settings",
                newName: "overlay_avatar_settings");

            migrationBuilder.RenameIndex(
                name: "IX_viewer_profiles_StreamerProfileId_Points",
                table: "view_profiles",
                newName: "IX_view_profiles_StreamerProfileId_Points");

            migrationBuilder.RenameIndex(
                name: "IX_viewer_profiles_StreamerProfileId_GlobalViewerId",
                table: "view_profiles",
                newName: "IX_view_profiles_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_viewer_profiles_GlobalViewerId",
                table: "view_profiles",
                newName: "IX_view_profiles_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_unified_commands_StreamerProfileId_TargetId",
                table: "func_cmd_unified",
                newName: "IX_func_cmd_unified_StreamerProfileId_TargetId");

            migrationBuilder.RenameIndex(
                name: "IX_unified_commands_StreamerProfileId_keyword",
                table: "func_cmd_unified",
                newName: "IX_func_cmd_unified_StreamerProfileId_keyword");

            migrationBuilder.RenameIndex(
                name: "IX_unified_commands_MasterCommandFeatureId",
                table: "func_cmd_unified",
                newName: "IX_func_cmd_unified_MasterCommandFeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_streamer_profiles_ChzzkUid",
                table: "core_streamer_profiles",
                newName: "IX_core_streamer_profiles_ChzzkUid");

            migrationBuilder.RenameIndex(
                name: "IX_streamer_omakases_StreamerProfileId",
                table: "func_omakase_items",
                newName: "IX_func_omakase_items_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_streamer_managers_StreamerProfileId_GlobalViewerId",
                table: "core_streamer_managers",
                newName: "IX_core_streamer_managers_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_streamer_managers_GlobalViewerId",
                table: "core_streamer_managers",
                newName: "IX_core_streamer_managers_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_song_queues_StreamerProfileId_Status_CreatedAt",
                table: "song_list_queues",
                newName: "IX_song_list_queues_StreamerProfileId_Status_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_song_queues_StreamerProfileId_Id",
                table: "song_list_queues",
                newName: "IX_song_list_queues_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_song_queues_StreamerProfileId",
                table: "song_list_queues",
                newName: "IX_song_list_queues_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_song_queues_GlobalViewerId",
                table: "song_list_queues",
                newName: "IX_song_list_queues_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_song_books_StreamerProfileId_Id",
                table: "song_book_main",
                newName: "IX_song_book_main_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_shared_components_StreamerProfileId",
                table: "overlay_components",
                newName: "IX_overlay_components_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_roulettes_StreamerProfileId_Id",
                table: "func_roulette_main",
                newName: "IX_func_roulette_main_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_roulettes_StreamerProfileId",
                table: "func_roulette_main",
                newName: "IX_func_roulette_main_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_spins_StreamerProfileId",
                table: "func_roulette_spins",
                newName: "IX_func_roulette_spins_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_spins_IsCompleted_ScheduledTime",
                table: "func_roulette_spins",
                newName: "IX_func_roulette_spins_IsCompleted_ScheduledTime");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_spins_GlobalViewerId",
                table: "func_roulette_spins",
                newName: "IX_func_roulette_spins_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_logs_StreamerProfileId_Status_Id",
                table: "func_roulette_logs",
                newName: "IX_func_roulette_logs_StreamerProfileId_Status_Id");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_logs_StreamerProfileId_GlobalViewerId",
                table: "func_roulette_logs",
                newName: "IX_func_roulette_logs_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_logs_RouletteItemId",
                table: "func_roulette_logs",
                newName: "IX_func_roulette_logs_RouletteItemId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_logs_RouletteId",
                table: "func_roulette_logs",
                newName: "IX_func_roulette_logs_RouletteId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_logs_GlobalViewerId",
                table: "func_roulette_logs",
                newName: "IX_func_roulette_logs_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_roulette_items_RouletteId",
                table: "func_roulette_items",
                newName: "IX_func_roulette_items_RouletteId");

            migrationBuilder.RenameIndex(
                name: "IX_periodic_messages_StreamerProfileId",
                table: "view_periodic_messages",
                newName: "IX_view_periodic_messages_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_master_command_features_CategoryId",
                table: "func_cmd_master_features",
                newName: "IX_func_cmd_master_features_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_global_viewers_ViewerUidHash",
                table: "core_global_viewers",
                newName: "IX_core_global_viewers_ViewerUidHash");

            migrationBuilder.RenameIndex(
                name: "IX_chzzk_category_aliases_CategoryId",
                table: "sys_chzzk_category_aliases",
                newName: "IX_sys_chzzk_category_aliases_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_chzzk_category_aliases_Alias",
                table: "sys_chzzk_category_aliases",
                newName: "IX_sys_chzzk_category_aliases_Alias");

            migrationBuilder.RenameIndex(
                name: "IX_broadcast_sessions_StreamerProfileId_IsActive",
                table: "sys_broadcast_sessions",
                newName: "IX_sys_broadcast_sessions_StreamerProfileId_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_avatar_settings_StreamerProfileId",
                table: "overlay_avatar_settings",
                newName: "IX_overlay_avatar_settings_StreamerProfileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_view_profiles",
                table: "view_profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_cmd_unified",
                table: "func_cmd_unified",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sys_settings",
                table: "sys_settings",
                column: "KeyName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_core_streamer_profiles",
                table: "core_streamer_profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_omakase_items",
                table: "func_omakase_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_core_streamer_managers",
                table: "core_streamer_managers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_song_list_queues",
                table: "song_list_queues",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_song_book_main",
                table: "song_book_main",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_overlay_components",
                table: "overlay_components",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_roulette_main",
                table: "func_roulette_main",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_roulette_spins",
                table: "func_roulette_spins",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_roulette_logs",
                table: "func_roulette_logs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_roulette_items",
                table: "func_roulette_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_view_periodic_messages",
                table: "view_periodic_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_cmd_master_variables",
                table: "func_cmd_master_variables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_cmd_master_features",
                table: "func_cmd_master_features",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_func_cmd_master_categories",
                table: "func_cmd_master_categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_core_global_viewers",
                table: "core_global_viewers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sys_chzzk_category_aliases",
                table: "sys_chzzk_category_aliases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sys_chzzk_categories",
                table: "sys_chzzk_categories",
                column: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sys_broadcast_sessions",
                table: "sys_broadcast_sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_overlay_avatar_settings",
                table: "overlay_avatar_settings",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "Id",
                keyValue: 1,
                column: "QueryString",
                value: "SELECT CAST(vp.Points AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "Id",
                keyValue: 2,
                column: "QueryString",
                value: "SELECT vp.Nickname FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "Id",
                keyValue: 6,
                column: "QueryString",
                value: "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "Id",
                keyValue: 7,
                column: "QueryString",
                value: "SELECT CAST(vp.AttendanceCount AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "func_cmd_master_variables",
                keyColumn: "Id",
                keyValue: 8,
                column: "QueryString",
                value: "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.AddForeignKey(
                name: "FK_core_streamer_managers_core_global_viewers_GlobalViewerId",
                table: "core_streamer_managers",
                column: "GlobalViewerId",
                principalTable: "core_global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_core_streamer_managers_core_streamer_profiles_StreamerProfil~",
                table: "core_streamer_managers",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_cmd_master_features_func_cmd_master_categories_Category~",
                table: "func_cmd_master_features",
                column: "CategoryId",
                principalTable: "func_cmd_master_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_cmd_unified_core_streamer_profiles_StreamerProfileId",
                table: "func_cmd_unified",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_cmd_unified_func_cmd_master_features_MasterCommandFeatu~",
                table: "func_cmd_unified",
                column: "MasterCommandFeatureId",
                principalTable: "func_cmd_master_features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_func_omakase_items_core_streamer_profiles_StreamerProfileId",
                table: "func_omakase_items",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_items_func_roulette_main_RouletteId",
                table: "func_roulette_items",
                column: "RouletteId",
                principalTable: "func_roulette_main",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_logs_core_global_viewers_GlobalViewerId",
                table: "func_roulette_logs",
                column: "GlobalViewerId",
                principalTable: "core_global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_logs_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_logs",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_logs_func_roulette_items_RouletteItemId",
                table: "func_roulette_logs",
                column: "RouletteItemId",
                principalTable: "func_roulette_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_main_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_main",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_spins_core_global_viewers_GlobalViewerId",
                table: "func_roulette_spins",
                column: "GlobalViewerId",
                principalTable: "core_global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_func_roulette_spins_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_spins",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_genos_registry_core_streamer_profiles_StreamerProfileId",
                table: "iamf_genos_registry",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_parhos_cycles_core_streamer_profiles_StreamerProfileId",
                table: "iamf_parhos_cycles",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_scenarios_core_streamer_profiles_StreamerProfileId",
                table: "iamf_scenarios",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_streamer_settings_core_streamer_profiles_StreamerProfil~",
                table: "iamf_streamer_settings",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_vibration_logs_core_streamer_profiles_StreamerProfileId",
                table: "iamf_vibration_logs",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_overlay_avatar_settings_core_streamer_profiles_StreamerProfi~",
                table: "overlay_avatar_settings",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_overlay_components_core_streamer_profiles_StreamerProfileId",
                table: "overlay_components",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_overlay_presets_core_streamer_profiles_StreamerProfileId",
                table: "overlay_presets",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_book_main_core_streamer_profiles_StreamerProfileId",
                table: "song_book_main",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_list_queues_core_global_viewers_GlobalViewerId",
                table: "song_list_queues",
                column: "GlobalViewerId",
                principalTable: "core_global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_song_list_queues_core_streamer_profiles_StreamerProfileId",
                table: "song_list_queues",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_list_sessions_core_streamer_profiles_StreamerProfileId",
                table: "song_list_sessions",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamer_knowledges_core_streamer_profiles_StreamerProfileId",
                table: "streamer_knowledges",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sys_broadcast_sessions_core_streamer_profiles_StreamerProfil~",
                table: "sys_broadcast_sessions",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sys_chzzk_category_aliases_sys_chzzk_categories_CategoryId",
                table: "sys_chzzk_category_aliases",
                column: "CategoryId",
                principalTable: "sys_chzzk_categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_view_periodic_messages_core_streamer_profiles_StreamerProfil~",
                table: "view_periodic_messages",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_view_profiles_core_global_viewers_GlobalViewerId",
                table: "view_profiles",
                column: "GlobalViewerId",
                principalTable: "core_global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_view_profiles_core_streamer_profiles_StreamerProfileId",
                table: "view_profiles",
                column: "StreamerProfileId",
                principalTable: "core_streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_core_streamer_managers_core_global_viewers_GlobalViewerId",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "FK_core_streamer_managers_core_streamer_profiles_StreamerProfil~",
                table: "core_streamer_managers");

            migrationBuilder.DropForeignKey(
                name: "FK_func_cmd_master_features_func_cmd_master_categories_Category~",
                table: "func_cmd_master_features");

            migrationBuilder.DropForeignKey(
                name: "FK_func_cmd_unified_core_streamer_profiles_StreamerProfileId",
                table: "func_cmd_unified");

            migrationBuilder.DropForeignKey(
                name: "FK_func_cmd_unified_func_cmd_master_features_MasterCommandFeatu~",
                table: "func_cmd_unified");

            migrationBuilder.DropForeignKey(
                name: "FK_func_omakase_items_core_streamer_profiles_StreamerProfileId",
                table: "func_omakase_items");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_items_func_roulette_main_RouletteId",
                table: "func_roulette_items");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_logs_core_global_viewers_GlobalViewerId",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_logs_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_logs_func_roulette_items_RouletteItemId",
                table: "func_roulette_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_main_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_main");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_spins_core_global_viewers_GlobalViewerId",
                table: "func_roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "FK_func_roulette_spins_core_streamer_profiles_StreamerProfileId",
                table: "func_roulette_spins");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_genos_registry_core_streamer_profiles_StreamerProfileId",
                table: "iamf_genos_registry");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_parhos_cycles_core_streamer_profiles_StreamerProfileId",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_scenarios_core_streamer_profiles_StreamerProfileId",
                table: "iamf_scenarios");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_streamer_settings_core_streamer_profiles_StreamerProfil~",
                table: "iamf_streamer_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_vibration_logs_core_streamer_profiles_StreamerProfileId",
                table: "iamf_vibration_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_overlay_avatar_settings_core_streamer_profiles_StreamerProfi~",
                table: "overlay_avatar_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_overlay_components_core_streamer_profiles_StreamerProfileId",
                table: "overlay_components");

            migrationBuilder.DropForeignKey(
                name: "FK_overlay_presets_core_streamer_profiles_StreamerProfileId",
                table: "overlay_presets");

            migrationBuilder.DropForeignKey(
                name: "FK_song_book_main_core_streamer_profiles_StreamerProfileId",
                table: "song_book_main");

            migrationBuilder.DropForeignKey(
                name: "FK_song_list_queues_core_global_viewers_GlobalViewerId",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "FK_song_list_queues_core_streamer_profiles_StreamerProfileId",
                table: "song_list_queues");

            migrationBuilder.DropForeignKey(
                name: "FK_song_list_sessions_core_streamer_profiles_StreamerProfileId",
                table: "song_list_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_streamer_knowledges_core_streamer_profiles_StreamerProfileId",
                table: "streamer_knowledges");

            migrationBuilder.DropForeignKey(
                name: "FK_sys_broadcast_sessions_core_streamer_profiles_StreamerProfil~",
                table: "sys_broadcast_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_sys_chzzk_category_aliases_sys_chzzk_categories_CategoryId",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropForeignKey(
                name: "FK_view_periodic_messages_core_streamer_profiles_StreamerProfil~",
                table: "view_periodic_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_view_profiles_core_global_viewers_GlobalViewerId",
                table: "view_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_view_profiles_core_streamer_profiles_StreamerProfileId",
                table: "view_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_view_profiles",
                table: "view_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_view_periodic_messages",
                table: "view_periodic_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sys_settings",
                table: "sys_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sys_chzzk_category_aliases",
                table: "sys_chzzk_category_aliases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sys_chzzk_categories",
                table: "sys_chzzk_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sys_broadcast_sessions",
                table: "sys_broadcast_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_song_list_queues",
                table: "song_list_queues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_song_book_main",
                table: "song_book_main");

            migrationBuilder.DropPrimaryKey(
                name: "PK_overlay_components",
                table: "overlay_components");

            migrationBuilder.DropPrimaryKey(
                name: "PK_overlay_avatar_settings",
                table: "overlay_avatar_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_roulette_spins",
                table: "func_roulette_spins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_roulette_main",
                table: "func_roulette_main");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_roulette_logs",
                table: "func_roulette_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_roulette_items",
                table: "func_roulette_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_omakase_items",
                table: "func_omakase_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_cmd_unified",
                table: "func_cmd_unified");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_cmd_master_variables",
                table: "func_cmd_master_variables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_cmd_master_features",
                table: "func_cmd_master_features");

            migrationBuilder.DropPrimaryKey(
                name: "PK_func_cmd_master_categories",
                table: "func_cmd_master_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_core_streamer_profiles",
                table: "core_streamer_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_core_streamer_managers",
                table: "core_streamer_managers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_core_global_viewers",
                table: "core_global_viewers");

            migrationBuilder.RenameTable(
                name: "view_profiles",
                newName: "viewer_profiles");

            migrationBuilder.RenameTable(
                name: "view_periodic_messages",
                newName: "periodic_messages");

            migrationBuilder.RenameTable(
                name: "sys_settings",
                newName: "system_settings");

            migrationBuilder.RenameTable(
                name: "sys_chzzk_category_aliases",
                newName: "chzzk_category_aliases");

            migrationBuilder.RenameTable(
                name: "sys_chzzk_categories",
                newName: "chzzk_categories");

            migrationBuilder.RenameTable(
                name: "sys_broadcast_sessions",
                newName: "broadcast_sessions");

            migrationBuilder.RenameTable(
                name: "song_list_queues",
                newName: "song_queues");

            migrationBuilder.RenameTable(
                name: "song_book_main",
                newName: "song_books");

            migrationBuilder.RenameTable(
                name: "overlay_components",
                newName: "shared_components");

            migrationBuilder.RenameTable(
                name: "overlay_avatar_settings",
                newName: "avatar_settings");

            migrationBuilder.RenameTable(
                name: "func_roulette_spins",
                newName: "roulette_spins");

            migrationBuilder.RenameTable(
                name: "func_roulette_main",
                newName: "roulettes");

            migrationBuilder.RenameTable(
                name: "func_roulette_logs",
                newName: "roulette_logs");

            migrationBuilder.RenameTable(
                name: "func_roulette_items",
                newName: "roulette_items");

            migrationBuilder.RenameTable(
                name: "func_omakase_items",
                newName: "streamer_omakases");

            migrationBuilder.RenameTable(
                name: "func_cmd_unified",
                newName: "unified_commands");

            migrationBuilder.RenameTable(
                name: "func_cmd_master_variables",
                newName: "master_dynamic_variables");

            migrationBuilder.RenameTable(
                name: "func_cmd_master_features",
                newName: "master_command_features");

            migrationBuilder.RenameTable(
                name: "func_cmd_master_categories",
                newName: "master_command_categories");

            migrationBuilder.RenameTable(
                name: "core_streamer_profiles",
                newName: "streamer_profiles");

            migrationBuilder.RenameTable(
                name: "core_streamer_managers",
                newName: "streamer_managers");

            migrationBuilder.RenameTable(
                name: "core_global_viewers",
                newName: "global_viewers");

            migrationBuilder.RenameIndex(
                name: "IX_view_profiles_StreamerProfileId_Points",
                table: "viewer_profiles",
                newName: "IX_viewer_profiles_StreamerProfileId_Points");

            migrationBuilder.RenameIndex(
                name: "IX_view_profiles_StreamerProfileId_GlobalViewerId",
                table: "viewer_profiles",
                newName: "IX_viewer_profiles_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_view_profiles_GlobalViewerId",
                table: "viewer_profiles",
                newName: "IX_viewer_profiles_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_view_periodic_messages_StreamerProfileId",
                table: "periodic_messages",
                newName: "IX_periodic_messages_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_sys_chzzk_category_aliases_CategoryId",
                table: "chzzk_category_aliases",
                newName: "IX_chzzk_category_aliases_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_sys_chzzk_category_aliases_Alias",
                table: "chzzk_category_aliases",
                newName: "IX_chzzk_category_aliases_Alias");

            migrationBuilder.RenameIndex(
                name: "IX_sys_broadcast_sessions_StreamerProfileId_IsActive",
                table: "broadcast_sessions",
                newName: "IX_broadcast_sessions_StreamerProfileId_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_song_list_queues_StreamerProfileId_Status_CreatedAt",
                table: "song_queues",
                newName: "IX_song_queues_StreamerProfileId_Status_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_song_list_queues_StreamerProfileId_Id",
                table: "song_queues",
                newName: "IX_song_queues_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_song_list_queues_StreamerProfileId",
                table: "song_queues",
                newName: "IX_song_queues_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_song_list_queues_GlobalViewerId",
                table: "song_queues",
                newName: "IX_song_queues_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_song_book_main_StreamerProfileId_Id",
                table: "song_books",
                newName: "IX_song_books_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_overlay_components_StreamerProfileId",
                table: "shared_components",
                newName: "IX_shared_components_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_overlay_avatar_settings_StreamerProfileId",
                table: "avatar_settings",
                newName: "IX_avatar_settings_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_spins_StreamerProfileId",
                table: "roulette_spins",
                newName: "IX_roulette_spins_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_spins_IsCompleted_ScheduledTime",
                table: "roulette_spins",
                newName: "IX_roulette_spins_IsCompleted_ScheduledTime");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_spins_GlobalViewerId",
                table: "roulette_spins",
                newName: "IX_roulette_spins_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_main_StreamerProfileId_Id",
                table: "roulettes",
                newName: "IX_roulettes_StreamerProfileId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_main_StreamerProfileId",
                table: "roulettes",
                newName: "IX_roulettes_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_logs_StreamerProfileId_Status_Id",
                table: "roulette_logs",
                newName: "IX_roulette_logs_StreamerProfileId_Status_Id");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_logs_StreamerProfileId_GlobalViewerId",
                table: "roulette_logs",
                newName: "IX_roulette_logs_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_logs_RouletteItemId",
                table: "roulette_logs",
                newName: "IX_roulette_logs_RouletteItemId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_logs_RouletteId",
                table: "roulette_logs",
                newName: "IX_roulette_logs_RouletteId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_logs_GlobalViewerId",
                table: "roulette_logs",
                newName: "IX_roulette_logs_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_func_roulette_items_RouletteId",
                table: "roulette_items",
                newName: "IX_roulette_items_RouletteId");

            migrationBuilder.RenameIndex(
                name: "IX_func_omakase_items_StreamerProfileId",
                table: "streamer_omakases",
                newName: "IX_streamer_omakases_StreamerProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_func_cmd_unified_StreamerProfileId_TargetId",
                table: "unified_commands",
                newName: "IX_unified_commands_StreamerProfileId_TargetId");

            migrationBuilder.RenameIndex(
                name: "IX_func_cmd_unified_StreamerProfileId_keyword",
                table: "unified_commands",
                newName: "IX_unified_commands_StreamerProfileId_keyword");

            migrationBuilder.RenameIndex(
                name: "IX_func_cmd_unified_MasterCommandFeatureId",
                table: "unified_commands",
                newName: "IX_unified_commands_MasterCommandFeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_func_cmd_master_features_CategoryId",
                table: "master_command_features",
                newName: "IX_master_command_features_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_core_streamer_profiles_ChzzkUid",
                table: "streamer_profiles",
                newName: "IX_streamer_profiles_ChzzkUid");

            migrationBuilder.RenameIndex(
                name: "IX_core_streamer_managers_StreamerProfileId_GlobalViewerId",
                table: "streamer_managers",
                newName: "IX_streamer_managers_StreamerProfileId_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_core_streamer_managers_GlobalViewerId",
                table: "streamer_managers",
                newName: "IX_streamer_managers_GlobalViewerId");

            migrationBuilder.RenameIndex(
                name: "IX_core_global_viewers_ViewerUidHash",
                table: "global_viewers",
                newName: "IX_global_viewers_ViewerUidHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_viewer_profiles",
                table: "viewer_profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_periodic_messages",
                table: "periodic_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_system_settings",
                table: "system_settings",
                column: "KeyName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_chzzk_category_aliases",
                table: "chzzk_category_aliases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_chzzk_categories",
                table: "chzzk_categories",
                column: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_broadcast_sessions",
                table: "broadcast_sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_song_queues",
                table: "song_queues",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_song_books",
                table: "song_books",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_shared_components",
                table: "shared_components",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_avatar_settings",
                table: "avatar_settings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roulette_spins",
                table: "roulette_spins",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roulettes",
                table: "roulettes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roulette_logs",
                table: "roulette_logs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roulette_items",
                table: "roulette_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streamer_omakases",
                table: "streamer_omakases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_unified_commands",
                table: "unified_commands",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_master_dynamic_variables",
                table: "master_dynamic_variables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_master_command_features",
                table: "master_command_features",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_master_command_categories",
                table: "master_command_categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streamer_profiles",
                table: "streamer_profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streamer_managers",
                table: "streamer_managers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_global_viewers",
                table: "global_viewers",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "master_dynamic_variables",
                keyColumn: "Id",
                keyValue: 1,
                column: "QueryString",
                value: "SELECT CAST(vp.Points AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamic_variables",
                keyColumn: "Id",
                keyValue: 2,
                column: "QueryString",
                value: "SELECT vp.Nickname FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamic_variables",
                keyColumn: "Id",
                keyValue: 6,
                column: "QueryString",
                value: "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamic_variables",
                keyColumn: "Id",
                keyValue: 7,
                column: "QueryString",
                value: "SELECT CAST(vp.AttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.UpdateData(
                table: "master_dynamic_variables",
                keyColumn: "Id",
                keyValue: 8,
                column: "QueryString",
                value: "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash");

            migrationBuilder.AddForeignKey(
                name: "FK_avatar_settings_streamer_profiles_StreamerProfileId",
                table: "avatar_settings",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_broadcast_sessions_streamer_profiles_StreamerProfileId",
                table: "broadcast_sessions",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_chzzk_category_aliases_chzzk_categories_CategoryId",
                table: "chzzk_category_aliases",
                column: "CategoryId",
                principalTable: "chzzk_categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_genos_registry_streamer_profiles_StreamerProfileId",
                table: "iamf_genos_registry",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_parhos_cycles_streamer_profiles_StreamerProfileId",
                table: "iamf_parhos_cycles",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_scenarios_streamer_profiles_StreamerProfileId",
                table: "iamf_scenarios",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_streamer_settings_streamer_profiles_StreamerProfileId",
                table: "iamf_streamer_settings",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_vibration_logs_streamer_profiles_StreamerProfileId",
                table: "iamf_vibration_logs",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_master_command_features_master_command_categories_CategoryId",
                table: "master_command_features",
                column: "CategoryId",
                principalTable: "master_command_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_overlay_presets_streamer_profiles_StreamerProfileId",
                table: "overlay_presets",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_periodic_messages_streamer_profiles_StreamerProfileId",
                table: "periodic_messages",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_items_roulettes_RouletteId",
                table: "roulette_items",
                column: "RouletteId",
                principalTable: "roulettes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_logs_global_viewers_GlobalViewerId",
                table: "roulette_logs",
                column: "GlobalViewerId",
                principalTable: "global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_logs_roulette_items_RouletteItemId",
                table: "roulette_logs",
                column: "RouletteItemId",
                principalTable: "roulette_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_logs_streamer_profiles_StreamerProfileId",
                table: "roulette_logs",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_spins_global_viewers_GlobalViewerId",
                table: "roulette_spins",
                column: "GlobalViewerId",
                principalTable: "global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roulette_spins_streamer_profiles_StreamerProfileId",
                table: "roulette_spins",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roulettes_streamer_profiles_StreamerProfileId",
                table: "roulettes",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_shared_components_streamer_profiles_StreamerProfileId",
                table: "shared_components",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_books_streamer_profiles_StreamerProfileId",
                table: "song_books",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_list_sessions_streamer_profiles_StreamerProfileId",
                table: "song_list_sessions",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_song_queues_global_viewers_GlobalViewerId",
                table: "song_queues",
                column: "GlobalViewerId",
                principalTable: "global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_song_queues_streamer_profiles_StreamerProfileId",
                table: "song_queues",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamer_knowledges_streamer_profiles_StreamerProfileId",
                table: "streamer_knowledges",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamer_managers_global_viewers_GlobalViewerId",
                table: "streamer_managers",
                column: "GlobalViewerId",
                principalTable: "global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamer_managers_streamer_profiles_StreamerProfileId",
                table: "streamer_managers",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamer_omakases_streamer_profiles_StreamerProfileId",
                table: "streamer_omakases",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_unified_commands_master_command_features_MasterCommandFeatur~",
                table: "unified_commands",
                column: "MasterCommandFeatureId",
                principalTable: "master_command_features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unified_commands_streamer_profiles_StreamerProfileId",
                table: "unified_commands",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_viewer_profiles_global_viewers_GlobalViewerId",
                table: "viewer_profiles",
                column: "GlobalViewerId",
                principalTable: "global_viewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_viewer_profiles_streamer_profiles_StreamerProfileId",
                table: "viewer_profiles",
                column: "StreamerProfileId",
                principalTable: "streamer_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
