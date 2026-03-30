using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_v1_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "avatarsettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowNickname = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowChat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisappearTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    WalkingImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StopImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InteractionImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatarsettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "broadcastsessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalChatCount = table.Column<int>(type: "int", nullable: false),
                    TopKeywordsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TopEmotesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broadcastsessions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chzzkcategories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryValue = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PosterImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chzzkcategories", x => x.CategoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iamf_genos_registry",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Frequency = table.Column<double>(type: "double", nullable: false),
                    Role = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metaphor = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_genos_registry", x => x.Name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iamf_parhos_cycles",
                columns: table => new
                {
                    CycleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParhosId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VibrationAtDeath = table.Column<double>(type: "double", nullable: false),
                    RebirthPercentage = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_parhos_cycles", x => x.CycleId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iamf_scenarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScenarioId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VibrationHz = table.Column<double>(type: "double", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_scenarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iamf_streamer_settings",
                columns: table => new
                {
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsIamfEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsVisualResonanceEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPersonaChatEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SensitivityMultiplier = table.Column<double>(type: "double", nullable: false),
                    OverlayOpacity = table.Column<double>(type: "double", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_streamer_settings", x => x.ChzzkUid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iamf_vibration_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawHz = table.Column<double>(type: "double", nullable: false),
                    EmaHz = table.Column<double>(type: "double", nullable: false),
                    StabilityScore = table.Column<double>(type: "double", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iamf_vibration_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "master_commandcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_commandcategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "master_dynamicvariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BadgeColor = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryString = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_dynamicvariables", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "overlaypresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overlaypresets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "periodicmessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodicmessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roulettelogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    RouletteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettelogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roulettes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roulettespins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ViewerUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettespins", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sharedcomponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sharedcomponents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "songbooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_songbooks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "songlistsessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    CompleteCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_songlistsessions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "songqueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_songqueues", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StreamerKnowledges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Keyword = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntentAnswer = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamerKnowledges", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamermanagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ManagerChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamermanagers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streameromakases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streameromakases", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "streamerprofiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChannelName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkAccessToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkRefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApiClientId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiClientSecret = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiRedirectUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoticeMemo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OmakaseCount = table.Column<int>(type: "int", nullable: false),
                    OmakaseCommand = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OmakasePrice = table.Column<int>(type: "int", nullable: false),
                    SongCommand = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SongPrice = table.Column<int>(type: "int", nullable: false),
                    DesignSettingsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointPerChat = table.Column<int>(type: "int", nullable: false),
                    PointPerDonation1000 = table.Column<int>(type: "int", nullable: false),
                    PointPerAttendance = table.Column<int>(type: "int", nullable: false),
                    AttendanceCommands = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttendanceReply = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCheckCommand = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointCheckReply = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotChzzkUid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotNickname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotAccessToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotRefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotTokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsBotEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOmakaseEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiveOverlayPresetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streamerprofiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "systemsettings",
                columns: table => new
                {
                    KeyName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotAccessToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotRefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_systemsettings", x => x.KeyName);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "unifiedcommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    chzzkuid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeatureType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    RequiredRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unifiedcommands", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "viewerprofiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerChzzkUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerUid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AttendanceCount = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveAttendanceCount = table.Column<int>(type: "int", nullable: false),
                    LastAttendanceAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viewerprofiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chzzkcategoryaliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Alias = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chzzkcategoryaliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chzzkcategoryaliases_chzzkcategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "chzzkcategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "master_commandfeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCost = table.Column<int>(type: "int", nullable: false),
                    RequiredRole = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_commandfeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_commandfeatures_master_commandcategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "master_commandcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rouletteitems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Probability = table.Column<double>(type: "double", nullable: false),
                    Probability10x = table.Column<double>(type: "double", nullable: false),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rouletteitems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rouletteitems_roulettes_RouletteId",
                        column: x => x.RouletteId,
                        principalTable: "roulettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "master_commandcategories",
                columns: new[] { "Id", "DisplayName", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "일반", "General", 1 },
                    { 2, "시스템메세지", "System", 2 },
                    { 3, "기능", "Feature", 3 }
                });

            migrationBuilder.InsertData(
                table: "master_dynamicvariables",
                columns: new[] { "Id", "BadgeColor", "Description", "Keyword", "QueryString" },
                values: new object[,]
                {
                    { 1, "primary", "보유 포인트", "{포인트}", "SELECT CAST(Point AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 2, "success", "시청자 닉네임", "{닉네임}", "SELECT ViewerName FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 3, "secondary", "현재 방송 제목", "{방제}", "METHOD:GetLiveTitle" },
                    { 4, "info", "현재 방송 카테고리", "{카테고리}", "METHOD:GetLiveCategory" },
                    { 5, "warning", "현재 방송 공지", "{공지}", "METHOD:GetLiveNotice" },
                    { 6, "success", "연속 출석한 일수", "{연속출석일수}", "SELECT CAST(ConsecutiveAttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 7, "info", "누적 출석한 횟수", "{누적출석일수}", "SELECT CAST(AttendanceCount AS CHAR) FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 8, "secondary", "최근 출석 날짜", "{마지막출석일}", "SELECT DATE_FORMAT(LastAttendanceAt, '%Y-%m-%d %H:%i') FROM viewerprofiles WHERE StreamerChzzkUid = @streamerUid AND ViewerUid = @viewerUid" },
                    { 9, "warning", "현재 송리스트 활성화 여부", "{송리스트}", "METHOD:GetSonglistStatus" }
                });

            migrationBuilder.InsertData(
                table: "master_commandfeatures",
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
                name: "IX_avatarsettings_ChzzkUid",
                table: "avatarsettings",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chzzkcategoryaliases_Alias",
                table: "chzzkcategoryaliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_chzzkcategoryaliases_CategoryId",
                table: "chzzkcategoryaliases",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_master_commandfeatures_CategoryId",
                table: "master_commandfeatures",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_overlaypresets_ChzzkUid",
                table: "overlaypresets",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_periodicmessages_ChzzkUid",
                table: "periodicmessages",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_rouletteitems_RouletteId",
                table: "rouletteitems",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_ChzzkUid_Status_Id",
                table: "roulettelogs",
                columns: new[] { "ChzzkUid", "Status", "Id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_RouletteId",
                table: "roulettelogs",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_roulettes_ChzzkUid",
                table: "roulettes",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_roulettes_ChzzkUid_Id",
                table: "roulettes",
                columns: new[] { "ChzzkUid", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_roulettespins_IsCompleted_ScheduledTime",
                table: "roulettespins",
                columns: new[] { "IsCompleted", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_sharedcomponents_ChzzkUid",
                table: "sharedcomponents",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songbooks_ChzzkUid_Id",
                table: "songbooks",
                columns: new[] { "ChzzkUid", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_songlistsessions_ChzzkUid_IsActive",
                table: "songlistsessions",
                columns: new[] { "ChzzkUid", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid",
                table: "songqueues",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Id",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_ChzzkUid_Status_CreatedAt",
                table: "songqueues",
                columns: new[] { "ChzzkUid", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_ManagerChzzkUid",
                table: "streamermanagers",
                column: "ManagerChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_streameromakases_ChzzkUid",
                table: "streameromakases",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_streamerprofiles_ChzzkUid",
                table: "streamerprofiles",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_keyword",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unifiedcommands_chzzkuid_TargetId",
                table: "unifiedcommands",
                columns: new[] { "chzzkuid", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_viewerprofiles_StreamerChzzkUid_Points",
                table: "viewerprofiles",
                columns: new[] { "StreamerChzzkUid", "Points" });

            migrationBuilder.CreateIndex(
                name: "IX_viewerprofiles_StreamerChzzkUid_ViewerUid",
                table: "viewerprofiles",
                columns: new[] { "StreamerChzzkUid", "ViewerUid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avatarsettings");

            migrationBuilder.DropTable(
                name: "broadcastsessions");

            migrationBuilder.DropTable(
                name: "chzzkcategoryaliases");

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
                name: "master_commandfeatures");

            migrationBuilder.DropTable(
                name: "master_dynamicvariables");

            migrationBuilder.DropTable(
                name: "overlaypresets");

            migrationBuilder.DropTable(
                name: "periodicmessages");

            migrationBuilder.DropTable(
                name: "rouletteitems");

            migrationBuilder.DropTable(
                name: "roulettelogs");

            migrationBuilder.DropTable(
                name: "roulettespins");

            migrationBuilder.DropTable(
                name: "sharedcomponents");

            migrationBuilder.DropTable(
                name: "songbooks");

            migrationBuilder.DropTable(
                name: "songlistsessions");

            migrationBuilder.DropTable(
                name: "songqueues");

            migrationBuilder.DropTable(
                name: "StreamerKnowledges");

            migrationBuilder.DropTable(
                name: "streamermanagers");

            migrationBuilder.DropTable(
                name: "streameromakases");

            migrationBuilder.DropTable(
                name: "streamerprofiles");

            migrationBuilder.DropTable(
                name: "systemsettings");

            migrationBuilder.DropTable(
                name: "unifiedcommands");

            migrationBuilder.DropTable(
                name: "viewerprofiles");

            migrationBuilder.DropTable(
                name: "chzzkcategories");

            migrationBuilder.DropTable(
                name: "master_commandcategories");

            migrationBuilder.DropTable(
                name: "roulettes");
        }
    }
}
