using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationAndBroadcastHistory_v6_2_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "core_global_viewers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    viewer_uid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    viewer_uid_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    profile_image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_core_global_viewers", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "core_streamer_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    chzzk_uid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    profile_image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    chzzk_access_token = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    chzzk_refresh_token = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    token_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    notice_memo = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    design_settings_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    point_per_chat = table.Column<int>(type: "int", nullable: false),
                    is_auto_accumulate_donation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    point_per_donation1000 = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_master_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    active_overlay_preset_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_core_streamer_profiles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    friendly_name = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    xml = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_cmd_master_categories", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_variables",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    badge_color = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    query_string = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_cmd_master_variables", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_chzzk_categories",
                columns: table => new
                {
                    category_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_value = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    poster_image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_chzzk_categories", x => x.category_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_settings",
                columns: table => new
                {
                    key_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    key_value = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bot_access_token = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bot_refresh_token = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_settings", x => x.key_name);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "core_streamer_managers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_core_streamer_managers", x => x.id);
                    table.ForeignKey(
                        name: "fk_core_streamer_managers_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_core_streamer_managers_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_main",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_roulette_main", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_roulette_main_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_spins",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    roulette_id = table.Column<int>(type: "int", nullable: false),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    results_json = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    summary = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_completed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    scheduled_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_roulette_spins", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_roulette_spins_core_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_func_roulette_spins_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_genos_registry",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    frequency = table.Column<double>(type: "double", nullable: false),
                    role = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metaphor = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_sync_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iamf_genos_registry", x => x.id);
                    table.ForeignKey(
                        name: "fk_iamf_genos_registry_core_streamer_profiles_streamer_profile_",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_parhos_cycles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    cycle_id = table.Column<int>(type: "int", nullable: false),
                    vibration_at_death = table.Column<double>(type: "double", nullable: false),
                    rebirth_percentage = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iamf_parhos_cycles", x => x.id);
                    table.ForeignKey(
                        name: "fk_iamf_parhos_cycles_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_scenarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    scenario_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vibration_hz = table.Column<double>(type: "double", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iamf_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_iamf_scenarios_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_streamer_settings",
                columns: table => new
                {
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    is_iamf_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_visual_resonance_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_persona_chat_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sensitivity_multiplier = table.Column<double>(type: "double", nullable: false),
                    overlay_opacity = table.Column<double>(type: "double", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iamf_streamer_settings", x => x.streamer_profile_id);
                    table.ForeignKey(
                        name: "fk_iamf_streamer_settings_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_vibration_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    raw_hz = table.Column<double>(type: "double", nullable: false),
                    ema_hz = table.Column<double>(type: "double", nullable: false),
                    stability_score = table.Column<double>(type: "double", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iamf_vibration_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_iamf_vibration_logs_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_avatar_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_nickname = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_chat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    disappear_time_seconds = table.Column<int>(type: "int", nullable: false),
                    walking_image_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stop_image_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    interaction_image_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_overlay_avatar_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_overlay_avatar_settings_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_components",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    config_json = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_overlay_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_overlay_components_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_presets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    config_json = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_overlay_presets", x => x.id);
                    table.ForeignKey(
                        name: "fk_overlay_presets_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_book_main",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    genre = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    usage_count = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_song_book_main", x => x.id);
                    table.ForeignKey(
                        name: "fk_song_book_main_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_omakases",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    icon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    count = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_song_list_omakases", x => x.id);
                    table.ForeignKey(
                        name: "fk_song_list_omakases_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    global_viewer_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_song_list_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_song_list_queues_core_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_song_list_queues_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    request_count = table.Column<int>(type: "int", nullable: false),
                    complete_count = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_song_list_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_song_list_sessions_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "streamer_knowledges",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    intent_answer = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamer_knowledges", x => x.id);
                    table.ForeignKey(
                        name: "fk_streamer_knowledges_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_broadcast_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    initial_title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    initial_category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    current_title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    current_category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    total_chat_count = table.Column<int>(type: "int", nullable: false),
                    top_keywords_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    top_emotes_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_broadcast_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sys_broadcast_sessions_core_streamer_profiles_streamer_profi",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "view_periodic_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    interval_minutes = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_view_periodic_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_view_periodic_messages_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "view_streamer_viewers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    points = table.Column<int>(type: "int", nullable: false),
                    donation_points = table.Column<int>(type: "int", nullable: false),
                    attendance_count = table.Column<int>(type: "int", nullable: false),
                    consecutive_attendance_count = table.Column<int>(type: "int", nullable: false),
                    last_attendance_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_view_streamer_viewers", x => x.id);
                    table.ForeignKey(
                        name: "fk_view_streamer_viewers_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_view_streamer_viewers_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_features",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    type_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    default_cost = table.Column<int>(type: "int", nullable: false),
                    required_role = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_cmd_master_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_cmd_master_features_func_cmd_master_categories_category",
                        column: x => x.category_id,
                        principalTable: "func_cmd_master_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_chzzk_category_aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alias = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_chzzk_category_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_sys_chzzk_category_aliases_sys_chzzk_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "sys_chzzk_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    roulette_id = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    probability = table.Column<double>(type: "double", nullable: false),
                    probability10x = table.Column<double>(type: "double", nullable: false),
                    color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_mission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_roulette_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_roulette_items_func_roulette_main_roulette_id",
                        column: x => x.roulette_id,
                        principalTable: "func_roulette_main",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_broadcast_history_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    broadcast_session_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    log_date = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_broadcast_history_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sys_broadcast_history_logs_sys_broadcast_sessions_broadcast_",
                        column: x => x.broadcast_session_id,
                        principalTable: "sys_broadcast_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_unified",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    master_command_feature_id = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cost = table.Column<int>(type: "int", nullable: false),
                    cost_type = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    response_text = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_id = table.Column<int>(type: "int", nullable: true),
                    required_role = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_cmd_unified", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_cmd_unified_master_command_features_master_command_feat",
                        column: x => x.master_command_feature_id,
                        principalTable: "func_cmd_master_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_func_cmd_unified_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    streamer_profile_id = table.Column<int>(type: "int", nullable: false),
                    roulette_id = table.Column<int>(type: "int", nullable: false),
                    roulette_item_id = table.Column<int>(type: "int", nullable: true),
                    roulette_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    global_viewer_id = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_mission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_func_roulette_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_func_roulette_logs_global_viewers_global_viewer_id",
                        column: x => x.global_viewer_id,
                        principalTable: "core_global_viewers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_func_roulette_logs_roulette_items_roulette_item_id",
                        column: x => x.roulette_item_id,
                        principalTable: "func_roulette_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_func_roulette_logs_streamer_profiles_streamer_profile_id",
                        column: x => x.streamer_profile_id,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.InsertData(
                table: "func_cmd_master_categories",
                columns: new[] { "id", "display_name", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, "일반", "General", 1 },
                    { 2, "시스템메세지", "System", 2 },
                    { 3, "기능", "Feature", 3 }
                });

            migrationBuilder.InsertData(
                table: "func_cmd_master_variables",
                columns: new[] { "id", "badge_color", "description", "keyword", "query_string" },
                values: new object[,]
                {
                    { 1, "primary", "보유 포인트", "{포인트}", "SELECT CAST(vp.Points AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 2, "success", "시청자 닉네임", "{닉네임}", "SELECT gv.Nickname FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 3, "secondary", "현재 방송 제목", "{방제}", "METHOD:GetLiveTitle" },
                    { 4, "info", "현재 방송 카테고리", "{카테고리}", "METHOD:GetLiveCategory" },
                    { 5, "warning", "현재 방송 공지", "{공지}", "METHOD:GetLiveNotice" },
                    { 6, "success", "연속 출석한 일수", "{연속출석일수}", "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 7, "info", "누적 출석한 횟수", "{누적출석일수}", "SELECT CAST(vp.AttendanceCount AS CHAR) FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 8, "secondary", "최근 출석 날짜", "{마지막출석일}", "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM view_streamer_viewers vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 10, "warning", "현재 송리스트 활성화 여부", "{송리스트}", "METHOD:GetSonglistStatus" }
                });

            migrationBuilder.InsertData(
                table: "func_cmd_master_features",
                columns: new[] { "id", "category_id", "default_cost", "display_name", "is_enabled", "required_role", "type_name" },
                values: new object[,]
                {
                    { 1, 1, 0, "텍스트 답변", true, "Viewer", "Reply" },
                    { 2, 2, 0, "공지", true, "Manager", "Notice" },
                    { 3, 2, 0, "방제", true, "Manager", "Title" },
                    { 4, 2, 0, "카테고리", true, "Manager", "Category" },
                    { 5, 2, 0, "송리스트", true, "Manager", "SonglistToggle" },
                    { 6, 3, 1000, "노래신청", true, "Viewer", "SongRequest" },
                    { 7, 3, 1000, "오마카세", true, "Viewer", "Omakase" },
                    { 8, 3, 500, "룰렛", true, "Viewer", "Roulette" },
                    { 9, 3, 0, "채팅포인트", true, "Viewer", "ChatPoint" },
                    { 10, 2, 0, "시스템 응답", true, "Manager", "SystemResponse" },
                    { 11, 3, 1000, "AI 답변", true, "Viewer", "AI" },
                    { 12, 3, 10, "출석체크", true, "Viewer", "Attendance" },
                    { 13, 1, 0, "포인트확인", true, "Viewer", "PointCheck" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_core_global_viewers_viewer_uid_hash",
                table: "core_global_viewers",
                column: "viewer_uid_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_core_streamer_managers_global_viewer_id",
                table: "core_streamer_managers",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_core_streamer_managers_streamer_profile_id_global_viewer_id",
                table: "core_streamer_managers",
                columns: new[] { "streamer_profile_id", "global_viewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_core_streamer_profiles_chzzk_uid",
                table: "core_streamer_profiles",
                column: "chzzk_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_master_features_category_id",
                table: "func_cmd_master_features",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_master_command_feature_id",
                table: "func_cmd_unified",
                column: "master_command_feature_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_keyword",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_func_cmd_unified_streamer_profile_id_target_id",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCommand_CursorPaging",
                table: "func_cmd_unified",
                columns: new[] { "streamer_profile_id", "id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_items_roulette_id",
                table: "func_roulette_items",
                column: "roulette_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_logs_global_viewer_id",
                table: "func_roulette_logs",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_logs_roulette_id",
                table: "func_roulette_logs",
                column: "roulette_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_logs_roulette_item_id",
                table: "func_roulette_logs",
                column: "roulette_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_logs_streamer_profile_id_global_viewer_id",
                table: "func_roulette_logs",
                columns: new[] { "streamer_profile_id", "global_viewer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteLog_Status_Cursor",
                table: "func_roulette_logs",
                columns: new[] { "streamer_profile_id", "status", "id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_main_streamer_profile_id",
                table: "func_roulette_main",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_main_streamer_profile_id_id",
                table: "func_roulette_main",
                columns: new[] { "streamer_profile_id", "id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_spins_global_viewer_id",
                table: "func_roulette_spins",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_spins_is_completed_scheduled_time",
                table: "func_roulette_spins",
                columns: new[] { "is_completed", "scheduled_time" });

            migrationBuilder.CreateIndex(
                name: "ix_func_roulette_spins_streamer_profile_id",
                table: "func_roulette_spins",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_iamf_genos_registry_streamer_profile_id",
                table: "iamf_genos_registry",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_iamf_parhos_cycles_streamer_profile_id_cycle_id",
                table: "iamf_parhos_cycles",
                columns: new[] { "streamer_profile_id", "cycle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iamf_scenarios_streamer_profile_id",
                table: "iamf_scenarios",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_iamf_vibration_logs_streamer_profile_id_created_at",
                table: "iamf_vibration_logs",
                columns: new[] { "streamer_profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_overlay_avatar_settings_streamer_profile_id",
                table: "overlay_avatar_settings",
                column: "streamer_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_overlay_components_streamer_profile_id",
                table: "overlay_components",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_overlay_presets_streamer_profile_id",
                table: "overlay_presets",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_book_main",
                columns: new[] { "streamer_profile_id", "id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_song_list_omakases_streamer_profile_id",
                table: "song_list_omakases",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_global_viewer_id",
                table: "song_list_queues",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_streamer_profile_id",
                table: "song_list_queues",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_streamer_profile_id_id",
                table: "song_list_queues",
                columns: new[] { "streamer_profile_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_streamer_profile_id_status_created_at",
                table: "song_list_queues",
                columns: new[] { "streamer_profile_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_SongQueue_Status_Cursor",
                table: "song_list_queues",
                columns: new[] { "streamer_profile_id", "status", "id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_song_list_sessions_streamer_profile_id_is_active",
                table: "song_list_sessions",
                columns: new[] { "streamer_profile_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_streamer_knowledges_streamer_profile_id_keyword",
                table: "streamer_knowledges",
                columns: new[] { "streamer_profile_id", "keyword" });

            migrationBuilder.CreateIndex(
                name: "ix_sys_broadcast_history_logs_broadcast_session_id_log_date",
                table: "sys_broadcast_history_logs",
                columns: new[] { "broadcast_session_id", "log_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sys_broadcast_sessions_streamer_profile_id_is_active",
                table: "sys_broadcast_sessions",
                columns: new[] { "streamer_profile_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_sys_chzzk_category_aliases_alias",
                table: "sys_chzzk_category_aliases",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "ix_sys_chzzk_category_aliases_category_id",
                table: "sys_chzzk_category_aliases",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_view_periodic_messages_streamer_profile_id",
                table: "view_periodic_messages",
                column: "streamer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_view_streamer_viewers_global_viewer_id",
                table: "view_streamer_viewers",
                column: "global_viewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_view_streamer_viewers_streamer_profile_id_global_viewer_id",
                table: "view_streamer_viewers",
                columns: new[] { "streamer_profile_id", "global_viewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_view_streamer_viewers_streamer_profile_id_points",
                table: "view_streamer_viewers",
                columns: new[] { "streamer_profile_id", "points" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "core_streamer_managers");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "func_cmd_master_variables");

            migrationBuilder.DropTable(
                name: "func_cmd_unified");

            migrationBuilder.DropTable(
                name: "func_roulette_logs");

            migrationBuilder.DropTable(
                name: "func_roulette_spins");

            migrationBuilder.DropTable(
                name: "iamf_genos_registry");

            migrationBuilder.DropTable(
                name: "iamf_parhos_cycles");

            migrationBuilder.DropTable(
                name: "iamf_scenarios");

            migrationBuilder.DropTable(
                name: "iamf_streamer_settings");

            migrationBuilder.DropTable(
                name: "iamf_vibration_logs");

            migrationBuilder.DropTable(
                name: "overlay_avatar_settings");

            migrationBuilder.DropTable(
                name: "overlay_components");

            migrationBuilder.DropTable(
                name: "overlay_presets");

            migrationBuilder.DropTable(
                name: "song_book_main");

            migrationBuilder.DropTable(
                name: "song_list_omakases");

            migrationBuilder.DropTable(
                name: "song_list_queues");

            migrationBuilder.DropTable(
                name: "song_list_sessions");

            migrationBuilder.DropTable(
                name: "streamer_knowledges");

            migrationBuilder.DropTable(
                name: "sys_broadcast_history_logs");

            migrationBuilder.DropTable(
                name: "sys_chzzk_category_aliases");

            migrationBuilder.DropTable(
                name: "sys_settings");

            migrationBuilder.DropTable(
                name: "view_periodic_messages");

            migrationBuilder.DropTable(
                name: "view_streamer_viewers");

            migrationBuilder.DropTable(
                name: "func_cmd_master_features");

            migrationBuilder.DropTable(
                name: "func_roulette_items");

            migrationBuilder.DropTable(
                name: "sys_broadcast_sessions");

            migrationBuilder.DropTable(
                name: "sys_chzzk_categories");

            migrationBuilder.DropTable(
                name: "core_global_viewers");

            migrationBuilder.DropTable(
                name: "func_cmd_master_categories");

            migrationBuilder.DropTable(
                name: "func_roulette_main");

            migrationBuilder.DropTable(
                name: "core_streamer_profiles");
        }
    }
}
