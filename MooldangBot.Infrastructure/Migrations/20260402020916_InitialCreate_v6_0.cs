using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_v6_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chzzk_categories",
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
                    table.PrimaryKey("PK_chzzk_categories", x => x.CategoryId);
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
                name: "global_viewers",
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
                    table.PrimaryKey("PK_global_viewers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "master_command_categories",
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
                    table.PrimaryKey("PK_master_command_categories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "master_dynamic_variables",
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
                    table.PrimaryKey("PK_master_dynamic_variables", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "streamer_profiles",
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
                    IsBotEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOmakaseEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DelYn = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MasterUseYn = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActiveOverlayPresetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamer_profiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "system_settings",
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
                    table.PrimaryKey("PK_system_settings", x => x.KeyName);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "chzzk_category_aliases",
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
                    table.PrimaryKey("PK_chzzk_category_aliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chzzk_category_aliases_chzzk_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "chzzk_categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "master_command_features",
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
                    table.PrimaryKey("PK_master_command_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_command_features_master_command_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "master_command_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "avatar_settings",
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
                    table.PrimaryKey("PK_avatar_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_avatar_settings_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "broadcast_sessions",
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
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broadcast_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_broadcast_sessions_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                    LastSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_genos_registry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_genos_registry_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_parhos_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_parhos_cycles_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_scenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iamf_scenarios_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                        name: "FK_iamf_streamer_settings_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                        name: "FK_iamf_vibration_logs_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_overlay_presets_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "periodic_messages",
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
                    table.PrimaryKey("PK_periodic_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_periodic_messages_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "roulette_spins",
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
                    table.PrimaryKey("PK_roulette_spins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulette_spins_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roulette_spins_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "roulettes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulettes_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "shared_components",
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
                    table.PrimaryKey("PK_shared_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shared_components_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_books_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_list_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_list_sessions_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "song_queues",
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
                    table.PrimaryKey("PK_song_queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_song_queues_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_song_queues_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamer_knowledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_streamer_knowledges_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "streamer_managers",
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
                    table.PrimaryKey("PK_streamer_managers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_streamer_managers_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_streamer_managers_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "streamer_omakases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamer_omakases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_streamer_omakases_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "viewer_profiles",
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
                    LastAttendanceAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viewer_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_viewer_profiles_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_viewer_profiles_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "unified_commands",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unified_commands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unified_commands_master_command_features_MasterCommandFeatur~",
                        column: x => x.MasterCommandFeatureId,
                        principalTable: "master_command_features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unified_commands_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "roulette_items",
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
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulette_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulette_items_roulettes_RouletteId",
                        column: x => x.RouletteId,
                        principalTable: "roulettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "roulette_logs",
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
                    table.PrimaryKey("PK_roulette_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulette_logs_global_viewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "global_viewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roulette_logs_roulette_items_RouletteItemId",
                        column: x => x.RouletteItemId,
                        principalTable: "roulette_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_roulette_logs_streamer_profiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.InsertData(
                table: "master_command_categories",
                columns: new[] { "Id", "DisplayName", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "일반", "General", 1 },
                    { 2, "시스템메세지", "System", 2 },
                    { 3, "기능", "Feature", 3 }
                });

            migrationBuilder.InsertData(
                table: "master_dynamic_variables",
                columns: new[] { "Id", "BadgeColor", "Description", "Keyword", "QueryString" },
                values: new object[,]
                {
                    { 1, "primary", "보유 포인트", "{포인트}", "SELECT CAST(vp.Points AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 2, "success", "시청자 닉네임", "{닉네임}", "SELECT vp.Nickname FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 3, "secondary", "현재 방송 제목", "{방제}", "METHOD:GetLiveTitle" },
                    { 4, "info", "현재 방송 카테고리", "{카테고리}", "METHOD:GetLiveCategory" },
                    { 5, "warning", "현재 방송 공지", "{공지}", "METHOD:GetLiveNotice" },
                    { 6, "success", "연속 출석한 일수", "{연속출석일수}", "SELECT CAST(vp.ConsecutiveAttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 7, "info", "누적 출석한 횟수", "{누적출석일수}", "SELECT CAST(vp.AttendanceCount AS CHAR) FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 8, "secondary", "최근 출석 날짜", "{마지막출석일}", "SELECT DATE_FORMAT(vp.LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewer_profiles vp JOIN streamer_profiles sp ON vp.StreamerProfileId = sp.Id JOIN global_viewers gv ON vp.GlobalViewerId = gv.Id WHERE sp.ChzzkUid = @streamerUid AND gv.ViewerUidHash = @viewerHash" },
                    { 9, "warning", "현재 송리스트 활성화 여부", "{송리스트}", "METHOD:GetSonglistStatus" }
                });

            migrationBuilder.InsertData(
                table: "master_command_features",
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
                name: "IX_avatar_settings_StreamerProfileId",
                table: "avatar_settings",
                column: "StreamerProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_broadcast_sessions_StreamerProfileId_IsActive",
                table: "broadcast_sessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_chzzk_category_aliases_Alias",
                table: "chzzk_category_aliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_chzzk_category_aliases_CategoryId",
                table: "chzzk_category_aliases",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_global_viewers_ViewerUidHash",
                table: "global_viewers",
                column: "ViewerUidHash",
                unique: true);

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
                name: "IX_master_command_features_CategoryId",
                table: "master_command_features",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_overlay_presets_StreamerProfileId",
                table: "overlay_presets",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_periodic_messages_StreamerProfileId",
                table: "periodic_messages",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_items_RouletteId",
                table: "roulette_items",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_logs_GlobalViewerId",
                table: "roulette_logs",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_logs_RouletteId",
                table: "roulette_logs",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_logs_RouletteItemId",
                table: "roulette_logs",
                column: "RouletteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_logs_StreamerProfileId_GlobalViewerId",
                table: "roulette_logs",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" });

            migrationBuilder.CreateIndex(
                name: "IX_roulette_logs_StreamerProfileId_Status_Id",
                table: "roulette_logs",
                columns: new[] { "StreamerProfileId", "Status", "Id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_roulette_spins_GlobalViewerId",
                table: "roulette_spins",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_roulette_spins_IsCompleted_ScheduledTime",
                table: "roulette_spins",
                columns: new[] { "IsCompleted", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_roulette_spins_StreamerProfileId",
                table: "roulette_spins",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettes_StreamerProfileId",
                table: "roulettes",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettes_StreamerProfileId_Id",
                table: "roulettes",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_shared_components_StreamerProfileId",
                table: "shared_components",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_song_books_StreamerProfileId_Id",
                table: "song_books",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_song_list_sessions_StreamerProfileId_IsActive",
                table: "song_list_sessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_song_queues_GlobalViewerId",
                table: "song_queues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_song_queues_StreamerProfileId",
                table: "song_queues",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_song_queues_StreamerProfileId_Id",
                table: "song_queues",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_song_queues_StreamerProfileId_Status_CreatedAt",
                table: "song_queues",
                columns: new[] { "StreamerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_streamer_knowledges_StreamerProfileId_Keyword",
                table: "streamer_knowledges",
                columns: new[] { "StreamerProfileId", "Keyword" });

            migrationBuilder.CreateIndex(
                name: "IX_streamer_managers_GlobalViewerId",
                table: "streamer_managers",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_streamer_managers_StreamerProfileId_GlobalViewerId",
                table: "streamer_managers",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_streamer_omakases_StreamerProfileId",
                table: "streamer_omakases",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_streamer_profiles_ChzzkUid",
                table: "streamer_profiles",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unified_commands_MasterCommandFeatureId",
                table: "unified_commands",
                column: "MasterCommandFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_unified_commands_StreamerProfileId_keyword",
                table: "unified_commands",
                columns: new[] { "StreamerProfileId", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unified_commands_StreamerProfileId_TargetId",
                table: "unified_commands",
                columns: new[] { "StreamerProfileId", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_viewer_profiles_GlobalViewerId",
                table: "viewer_profiles",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_viewer_profiles_StreamerProfileId_GlobalViewerId",
                table: "viewer_profiles",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_viewer_profiles_StreamerProfileId_Points",
                table: "viewer_profiles",
                columns: new[] { "StreamerProfileId", "Points" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avatar_settings");

            migrationBuilder.DropTable(
                name: "broadcast_sessions");

            migrationBuilder.DropTable(
                name: "chzzk_category_aliases");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

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
                name: "master_dynamic_variables");

            migrationBuilder.DropTable(
                name: "overlay_presets");

            migrationBuilder.DropTable(
                name: "periodic_messages");

            migrationBuilder.DropTable(
                name: "roulette_logs");

            migrationBuilder.DropTable(
                name: "roulette_spins");

            migrationBuilder.DropTable(
                name: "shared_components");

            migrationBuilder.DropTable(
                name: "song_books");

            migrationBuilder.DropTable(
                name: "song_list_sessions");

            migrationBuilder.DropTable(
                name: "song_queues");

            migrationBuilder.DropTable(
                name: "streamer_knowledges");

            migrationBuilder.DropTable(
                name: "streamer_managers");

            migrationBuilder.DropTable(
                name: "streamer_omakases");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "unified_commands");

            migrationBuilder.DropTable(
                name: "viewer_profiles");

            migrationBuilder.DropTable(
                name: "chzzk_categories");

            migrationBuilder.DropTable(
                name: "roulette_items");

            migrationBuilder.DropTable(
                name: "master_command_features");

            migrationBuilder.DropTable(
                name: "global_viewers");

            migrationBuilder.DropTable(
                name: "roulettes");

            migrationBuilder.DropTable(
                name: "master_command_categories");

            migrationBuilder.DropTable(
                name: "streamer_profiles");
        }
    }
}
