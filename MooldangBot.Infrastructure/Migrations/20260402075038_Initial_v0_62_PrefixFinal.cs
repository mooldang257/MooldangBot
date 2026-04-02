using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_v0_62_PrefixFinal : Migration
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ViewerUid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerUidHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_global_viewers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "core_streamer_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChannelName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkAccessToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkRefreshToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApiClientId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiClientSecret = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiRedirectUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoticeMemo = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OmakaseCount = table.Column<int>(type: "int", nullable: false),
                    OmakaseCommand = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OmakasePrice = table.Column<int>(type: "int", nullable: false),
                    SongCommand = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SongPrice = table.Column<int>(type: "int", nullable: false),
                    DesignSettingsJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointPerChat = table.Column<int>(type: "int", nullable: false),
                    PointPerDonation1000 = table.Column<int>(type: "int", nullable: false),
                    PointPerAttendance = table.Column<int>(type: "int", nullable: false),
                    AttendanceCommands = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttendanceReply = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCheckCommand = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCheckReply = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotChzzkUid = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotNickname = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotAccessToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotRefreshToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotTokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOmakaseEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsMasterEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActiveOverlayPresetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_streamer_profiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FriendlyName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Xml = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_cmd_master_categories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_variables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BadgeColor = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryString = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_cmd_master_variables", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_chzzk_categories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryValue = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PosterImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_chzzk_categories", x => x.CategoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_settings",
                columns: table => new
                {
                    KeyName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyValue = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotAccessToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotRefreshToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_settings", x => x.KeyName);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "core_streamer_managers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_streamer_managers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_core_streamer_managers_core_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "core_global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_core_streamer_managers_core_streamer_profiles_StreamerProfil~",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_main",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_roulette_main", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_roulette_main_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_spins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultsJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_roulette_spins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_roulette_spins_core_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "core_global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_func_roulette_spins_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_genos_registry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Frequency = table.Column<double>(type: "double", nullable: false),
                    Role = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metaphor = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_genos_registry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_genos_registry_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_parhos_cycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    CycleId = table.Column<int>(type: "int", nullable: false),
                    VibrationAtDeath = table.Column<double>(type: "double", nullable: false),
                    RebirthPercentage = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_parhos_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_parhos_cycles_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_scenarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    ScenarioId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VibrationHz = table.Column<double>(type: "double", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_scenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_scenarios_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_streamer_settings",
                columns: table => new
                {
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    IsIamfEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsVisualResonanceEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPersonaChatEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SensitivityMultiplier = table.Column<double>(type: "double", nullable: false),
                    OverlayOpacity = table.Column<double>(type: "double", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_streamer_settings", x => x.StreamerProfileId);
                    table.ForeignKey(
                        name: "FK_iamf_streamer_settings_core_streamer_profiles_StreamerProfil~",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "iamf_vibration_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RawHz = table.Column<double>(type: "double", nullable: false),
                    EmaHz = table.Column<double>(type: "double", nullable: false),
                    StabilityScore = table.Column<double>(type: "double", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_vibration_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_vibration_logs_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_avatar_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowNickname = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowChat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisappearTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    WalkingImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StopImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InteractionImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overlay_avatar_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overlay_avatar_settings_core_streamer_profiles_StreamerProfi~",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overlay_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overlay_components_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "overlay_presets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overlay_presets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overlay_presets_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_book_main",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genre = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_book_main", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_book_main_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_omakases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_list_omakases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_list_omakases_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_queues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_list_queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_list_queues_core_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "core_global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_song_list_queues_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_list_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    CompleteCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_list_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_list_sessions_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "streamer_knowledges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntentAnswer = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamer_knowledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_streamer_knowledges_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_broadcast_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalChatCount = table.Column<int>(type: "int", nullable: false),
                    TopKeywordsJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TopEmotesJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_broadcast_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_broadcast_sessions_core_streamer_profiles_StreamerProfil~",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "view_periodic_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_view_periodic_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_view_periodic_messages_core_streamer_profiles_StreamerProfil~",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "view_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    Nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AttendanceCount = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveAttendanceCount = table.Column<int>(type: "int", nullable: false),
                    LastAttendanceAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_view_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_view_profiles_core_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "core_global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_view_profiles_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_master_features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCost = table.Column<int>(type: "int", nullable: false),
                    RequiredRole = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_cmd_master_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_cmd_master_features_func_cmd_master_categories_Category~",
                        column: x => x.CategoryId,
                        principalTable: "func_cmd_master_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "sys_chzzk_category_aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Alias = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_chzzk_category_aliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_chzzk_category_aliases_sys_chzzk_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "sys_chzzk_categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Probability = table.Column<double>(type: "double", nullable: false),
                    Probability10x = table.Column<double>(type: "double", nullable: false),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_roulette_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_roulette_items_func_roulette_main_RouletteId",
                        column: x => x.RouletteId,
                        principalTable: "func_roulette_main",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_cmd_unified",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    MasterCommandFeatureId = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    RequiredRole = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_cmd_unified", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_cmd_unified_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_func_cmd_unified_func_cmd_master_features_MasterCommandFeatu~",
                        column: x => x.MasterCommandFeatureId,
                        principalTable: "func_cmd_master_features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "func_roulette_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    RouletteItemId = table.Column<int>(type: "int", nullable: true),
                    RouletteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_func_roulette_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_func_roulette_logs_core_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "core_global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_func_roulette_logs_core_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "core_streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_func_roulette_logs_func_roulette_items_RouletteItemId",
                        column: x => x.RouletteItemId,
                        principalTable: "func_roulette_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.InsertData(
                table: "func_cmd_master_categories",
                columns: new[] { "Id", "DisplayName", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "일반", "General", 1 },
                    { 2, "시스템메세지", "System", 2 },
                    { 3, "기능", "Feature", 3 }
                });

            migrationBuilder.InsertData(
                table: "func_cmd_master_variables",
                columns: new[] { "Id", "BadgeColor", "Description", "Keyword", "QueryString" },
                values: new object[,]
                {
                    { 1, "primary", "보유 포인트", "{포인트}", "SELECT CAST(vp.Points AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 2, "success", "시청자 닉네임", "{닉네임}", "SELECT vp.Nickname FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 3, "secondary", "현재 방송 제목", "{방제}", "METHOD:GetLiveTitle" },
                    { 4, "info", "현재 방송 카테고리", "{카테고리}", "METHOD:GetLiveCategory" },
                    { 5, "warning", "현재 방송 공지", "{공지}", "METHOD:GetLiveNotice" },
                    { 6, "success", "연속 출석한 일수", "{연속출석일수}", "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 7, "info", "누적 출석한 횟수", "{누적출석일수}", "SELECT CAST(vp.AttendanceCount AS CHAR) FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 8, "secondary", "최근 출석 날짜", "{마지막출석일}", "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM view_profiles vp JOIN core_streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN core_global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 10, "warning", "현재 송리스트 활성화 여부", "{송리스트}", "METHOD:GetSonglistStatus" }
                });

            migrationBuilder.InsertData(
                table: "func_cmd_master_features",
                columns: new[] { "Id", "CategoryId", "DefaultCost", "DisplayName", "IsEnabled", "RequiredRole", "TypeName" },
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
                    { 11, 3, 1000, "AI 답변", true, "Viewer", "AI" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_core_global_viewers_ViewerUidHash",
                table: "core_global_viewers",
                column: "ViewerUidHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_core_streamer_managers_GlobalViewerId",
                table: "core_streamer_managers",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_core_streamer_managers_StreamerProfileId_GlobalViewerId",
                table: "core_streamer_managers",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_core_streamer_profiles_ChzzkUid",
                table: "core_streamer_profiles",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_func_cmd_master_features_CategoryId",
                table: "func_cmd_master_features",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_func_cmd_unified_MasterCommandFeatureId",
                table: "func_cmd_unified",
                column: "MasterCommandFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_func_cmd_unified_StreamerProfileId_keyword",
                table: "func_cmd_unified",
                columns: new[] { "StreamerProfileId", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_func_cmd_unified_StreamerProfileId_TargetId",
                table: "func_cmd_unified",
                columns: new[] { "StreamerProfileId", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_items_RouletteId",
                table: "func_roulette_items",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_logs_GlobalViewerId",
                table: "func_roulette_logs",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_logs_RouletteId",
                table: "func_roulette_logs",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_logs_RouletteItemId",
                table: "func_roulette_logs",
                column: "RouletteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_logs_StreamerProfileId_GlobalViewerId",
                table: "func_roulette_logs",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" });

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_logs_StreamerProfileId_Status_Id",
                table: "func_roulette_logs",
                columns: new[] { "StreamerProfileId", "Status", "Id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_main_StreamerProfileId",
                table: "func_roulette_main",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_main_StreamerProfileId_Id",
                table: "func_roulette_main",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_spins_GlobalViewerId",
                table: "func_roulette_spins",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_spins_IsCompleted_ScheduledTime",
                table: "func_roulette_spins",
                columns: new[] { "IsCompleted", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_func_roulette_spins_StreamerProfileId",
                table: "func_roulette_spins",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_iamf_genos_registry_StreamerProfileId",
                table: "iamf_genos_registry",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_iamf_parhos_cycles_StreamerProfileId_CycleId",
                table: "iamf_parhos_cycles",
                columns: new[] { "StreamerProfileId", "CycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iamf_scenarios_StreamerProfileId",
                table: "iamf_scenarios",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_iamf_vibration_logs_StreamerProfileId_CreatedAt",
                table: "iamf_vibration_logs",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_overlay_avatar_settings_StreamerProfileId",
                table: "overlay_avatar_settings",
                column: "StreamerProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_overlay_components_StreamerProfileId",
                table: "overlay_components",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_overlay_presets_StreamerProfileId",
                table: "overlay_presets",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_song_book_main_StreamerProfileId_Id",
                table: "song_book_main",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_song_list_omakases_StreamerProfileId",
                table: "song_list_omakases",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_song_list_queues_GlobalViewerId",
                table: "song_list_queues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_song_list_queues_StreamerProfileId",
                table: "song_list_queues",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_song_list_queues_StreamerProfileId_Id",
                table: "song_list_queues",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_song_list_queues_StreamerProfileId_Status_CreatedAt",
                table: "song_list_queues",
                columns: new[] { "StreamerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_song_list_sessions_StreamerProfileId_IsActive",
                table: "song_list_sessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_streamer_knowledges_StreamerProfileId_Keyword",
                table: "streamer_knowledges",
                columns: new[] { "StreamerProfileId", "Keyword" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_broadcast_sessions_StreamerProfileId_IsActive",
                table: "sys_broadcast_sessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_chzzk_category_aliases_Alias",
                table: "sys_chzzk_category_aliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_sys_chzzk_category_aliases_CategoryId",
                table: "sys_chzzk_category_aliases",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_view_periodic_messages_StreamerProfileId",
                table: "view_periodic_messages",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_view_profiles_GlobalViewerId",
                table: "view_profiles",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_view_profiles_StreamerProfileId_GlobalViewerId",
                table: "view_profiles",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_view_profiles_StreamerProfileId_Points",
                table: "view_profiles",
                columns: new[] { "StreamerProfileId", "Points" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "core_streamer_managers");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

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
                name: "sys_broadcast_sessions");

            migrationBuilder.DropTable(
                name: "sys_chzzk_category_aliases");

            migrationBuilder.DropTable(
                name: "sys_settings");

            migrationBuilder.DropTable(
                name: "view_periodic_messages");

            migrationBuilder.DropTable(
                name: "view_profiles");

            migrationBuilder.DropTable(
                name: "func_cmd_master_features");

            migrationBuilder.DropTable(
                name: "func_roulette_items");

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
