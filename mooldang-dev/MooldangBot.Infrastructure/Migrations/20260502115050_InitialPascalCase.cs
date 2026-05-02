using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPascalCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CoreGlobalViewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ViewerUid = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerUidHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreGlobalViewers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "CoreStreamerProfiles",
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
                    Slug = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkAccessToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChzzkRefreshToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NoticeMemo = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignSettingsJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointPerChat = table.Column<int>(type: "int", nullable: false),
                    IsAutoAccumulateDonation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PointPerDonation1000 = table.Column<int>(type: "int", nullable: false),
                    PointPerAttendance = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsMasterEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActiveOverlayPresetId = table.Column<int>(type: "int", nullable: true),
                    OverlayTokenVersion = table.Column<int>(type: "int", nullable: false),
                    OverlayToken = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientSecret = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BotNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreStreamerProfiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongMasterLibrary",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SongLibraryId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Alias = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Album = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeTitle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MrUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LyricsUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongMasterLibrary", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongMasterStaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SongLibraryId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Alias = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeTitle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LyricsUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongMasterStaging", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysChzzkCategories",
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
                    table.PrimaryKey("PK_SysChzzkCategories", x => x.CategoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysSagaCommandExecutions",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CurrentState = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StreamerUid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerUid = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewerNickname = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChargedAmount = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysSagaCommandExecutions", x => x.CorrelationId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "CoreStreamerManagers",
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
                    table.PrimaryKey("PK_CoreStreamerManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreStreamerManagers_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoreStreamerManagers_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "CoreViewerRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    AttendanceCount = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveAttendanceCount = table.Column<int>(type: "int", nullable: false),
                    LastAttendanceAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FirstVisitAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastChatAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreViewerRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreViewerRelations_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoreViewerRelations_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncCmdUnified",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    FeatureType = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Keyword = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MatchType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresSpace = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_FuncCmdUnified", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncCmdUnified_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncRouletteMain",
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
                    table.PrimaryKey("PK_FuncRouletteMain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncRouletteMain_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncRouletteSpins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FuncRouletteSpins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncRouletteSpins_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncRouletteSpins_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SongNo = table.Column<int>(type: "int", nullable: false),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    SongLibraryId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Album = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pitch = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Proficiency = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MrUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LyricsUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRequestable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sing_count = table.Column<int>(type: "int", nullable: false),
                    RequiredPoints = table.Column<int>(type: "int", nullable: false),
                    LastSungAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Alias = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSongBooks_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongListOmakases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongListOmakases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSongListOmakases_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongListSessions",
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
                    table.PrimaryKey("PK_FuncSongListSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSongListSessions_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongStreamerLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    SongLibraryId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeTitle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LyricsUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Alias = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleChosung = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongStreamerLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSongStreamerLibrary_CoreStreamerProfiles_StreamerProfile~",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSoundAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoundUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssetType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSoundAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSoundAssets_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncViewerDonationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    PlatformTransactionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncViewerDonationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncViewerDonationHistories_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncViewerDonationHistories_CoreStreamerProfiles_StreamerPro~",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncViewerDonations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false),
                    TotalDonated = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncViewerDonations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncViewerDonations_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncViewerDonations_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncViewerPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncViewerPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncViewerPoints_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncViewerPoints_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "IamfGenosRegistry",
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
                    table.PrimaryKey("PK_IamfGenosRegistry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IamfGenosRegistry_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "IamfParhosCycles",
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
                    table.PrimaryKey("PK_IamfParhosCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IamfParhosCycles_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "IamfScenarios",
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
                    table.PrimaryKey("PK_IamfScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IamfScenarios_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "IamfStreamerSettings",
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
                    table.PrimaryKey("PK_IamfStreamerSettings", x => x.StreamerProfileId);
                    table.ForeignKey(
                        name: "FK_IamfStreamerSettings_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogChatInteractions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    SenderNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCommand = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MessageType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogChatInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogChatInteractions_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogCommandExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DonationAmount = table.Column<int>(type: "int", nullable: false),
                    Arguments = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawMessage = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogCommandExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogCommandExecutions_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogCommandExecutions_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogIamfVibrations",
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
                    table.PrimaryKey("PK_LogIamfVibrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogIamfVibrations_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogPointDailySummaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalEarned = table.Column<long>(type: "bigint", nullable: false),
                    TotalSpent = table.Column<long>(type: "bigint", nullable: false),
                    UniqueViewerCount = table.Column<int>(type: "int", nullable: false),
                    TopCommandStatsJson = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogPointDailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogPointDailySummaries_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogPointTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogPointTransactions_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogPointTransactions_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysAvatarSettings",
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
                    table.PrimaryKey("PK_SysAvatarSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysAvatarSettings_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysBroadcastSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    InitialTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialCategory = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentCategory = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_SysBroadcastSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysBroadcastSessions_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysOverlayPresets",
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
                    table.PrimaryKey("PK_SysOverlayPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysOverlayPresets_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysPeriodicMessages",
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
                    table.PrimaryKey("PK_SysPeriodicMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysPeriodicMessages_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysSharedComponents",
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
                    table.PrimaryKey("PK_SysSharedComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysSharedComponents_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysStreamerKnowledges",
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
                    table.PrimaryKey("PK_SysStreamerKnowledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysStreamerKnowledges_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysStreamerPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: true),
                    PreferenceKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferenceValue = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysStreamerPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysStreamerPreferences_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "SysChzzkCategoryAliases",
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
                    table.PrimaryKey("PK_SysChzzkCategoryAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysChzzkCategoryAliases_SysChzzkCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SysChzzkCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncRouletteItems",
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
                    Template = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoundUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UseDefaultSound = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncRouletteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncRouletteItems_FuncRouletteMain_RouletteId",
                        column: x => x.RouletteId,
                        principalTable: "FuncRouletteMain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogRouletteStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TheoreticalProbability = table.Column<double>(type: "double", nullable: false),
                    TotalSpins = table.Column<int>(type: "int", nullable: false),
                    WinCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogRouletteStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogRouletteStats_FuncRouletteMain_RouletteId",
                        column: x => x.RouletteId,
                        principalTable: "FuncRouletteMain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "FuncSongListQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: true),
                    SongBookId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SongLibraryId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequesterNickname = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cost = table.Column<int>(type: "int", nullable: true),
                    CostType = table.Column<int>(type: "int", nullable: true),
                    VideoId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailUrl = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pitch = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncSongListQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncSongListQueues_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncSongListQueues_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuncSongListQueues_FuncSongBooks_SongBookId",
                        column: x => x.SongBookId,
                        principalTable: "FuncSongBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogBroadcastHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BroadcastSessionId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogBroadcastHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogBroadcastHistory_SysBroadcastSessions_BroadcastSessionId",
                        column: x => x.BroadcastSessionId,
                        principalTable: "SysBroadcastSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "LogRouletteResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    RouletteItemId = table.Column<int>(type: "int", nullable: true),
                    RouletteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
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
                    table.PrimaryKey("PK_LogRouletteResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogRouletteResults_CoreGlobalViewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "CoreGlobalViewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogRouletteResults_CoreStreamerProfiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "CoreStreamerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogRouletteResults_FuncRouletteItems_RouletteItemId",
                        column: x => x.RouletteItemId,
                        principalTable: "FuncRouletteItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalViewer_Nickname",
                table: "CoreGlobalViewers",
                column: "Nickname");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalViewer_ViewerUid",
                table: "CoreGlobalViewers",
                column: "ViewerUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalViewer_ViewerUidHash",
                table: "CoreGlobalViewers",
                column: "ViewerUidHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreStreamerManagers_GlobalViewerId",
                table: "CoreStreamerManagers",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoreStreamerManagers_StreamerProfileId_GlobalViewerId",
                table: "CoreStreamerManagers",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreStreamerProfiles_ChzzkUid",
                table: "CoreStreamerProfiles",
                column: "ChzzkUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreStreamerProfiles_Slug",
                table: "CoreStreamerProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreViewerRelations_GlobalViewerId",
                table: "CoreViewerRelations",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoreViewerRelations_StreamerProfileId_GlobalViewerId",
                table: "CoreViewerRelations",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Command_Search",
                table: "FuncCmdUnified",
                columns: new[] { "StreamerProfileId", "Keyword", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncCmdUnified_StreamerProfileId_Keyword_FeatureType",
                table: "FuncCmdUnified",
                columns: new[] { "StreamerProfileId", "Keyword", "FeatureType" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncCmdUnified_StreamerProfileId_TargetId",
                table: "FuncCmdUnified",
                columns: new[] { "StreamerProfileId", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCommand_CursorPaging",
                table: "FuncCmdUnified",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteItems_RouletteId",
                table: "FuncRouletteItems",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteMain_StreamerProfileId",
                table: "FuncRouletteMain",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteMain_StreamerProfileId_Id",
                table: "FuncRouletteMain",
                columns: new[] { "StreamerProfileId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteSpins_GlobalViewerId",
                table: "FuncRouletteSpins",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteSpins_IsCompleted_ScheduledTime",
                table: "FuncRouletteSpins",
                columns: new[] { "IsCompleted", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncRouletteSpins_StreamerProfileId",
                table: "FuncRouletteSpins",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_Alias",
                table: "FuncSongBooks",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_Category",
                table: "FuncSongBooks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_IsRequestable",
                table: "FuncSongBooks",
                column: "IsRequestable");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_SongLibraryId",
                table: "FuncSongBooks",
                column: "SongLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_StreamerProfileId_Id",
                table: "FuncSongBooks",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_StreamerProfileId_SongNo",
                table: "FuncSongBooks",
                columns: new[] { "StreamerProfileId", "SongNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_Title",
                table: "FuncSongBooks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongBooks_TitleChosung",
                table: "FuncSongBooks",
                column: "TitleChosung");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListOmakases_StreamerProfileId",
                table: "FuncSongListOmakases",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_GlobalViewerId",
                table: "FuncSongListQueues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_SongBookId",
                table: "FuncSongListQueues",
                column: "SongBookId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_SongLibraryId",
                table: "FuncSongListQueues",
                column: "SongLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_StreamerProfileId",
                table: "FuncSongListQueues",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_StreamerProfileId_Id",
                table: "FuncSongListQueues",
                columns: new[] { "StreamerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListQueues_StreamerProfileId_Status_CreatedAt",
                table: "FuncSongListQueues",
                columns: new[] { "StreamerProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SongQueue_Status_Cursor",
                table: "FuncSongListQueues",
                columns: new[] { "StreamerProfileId", "Status", "Id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongListSessions_StreamerProfileId_IsActive",
                table: "FuncSongListSessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_Album",
                table: "FuncSongMasterLibrary",
                column: "Album");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_Alias",
                table: "FuncSongMasterLibrary",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_ArtistChosung",
                table: "FuncSongMasterLibrary",
                column: "ArtistChosung");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_Category",
                table: "FuncSongMasterLibrary",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_SongLibraryId",
                table: "FuncSongMasterLibrary",
                column: "SongLibraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_Title",
                table: "FuncSongMasterLibrary",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_TitleChosung",
                table: "FuncSongMasterLibrary",
                column: "TitleChosung");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterLibrary_YoutubeUrl",
                table: "FuncSongMasterLibrary",
                column: "YoutubeUrl");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterStaging_ArtistChosung",
                table: "FuncSongMasterStaging",
                column: "ArtistChosung");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterStaging_CreatedAt",
                table: "FuncSongMasterStaging",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterStaging_SongLibraryId",
                table: "FuncSongMasterStaging",
                column: "SongLibraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterStaging_TitleChosung",
                table: "FuncSongMasterStaging",
                column: "TitleChosung");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongMasterStaging_YoutubeUrl",
                table: "FuncSongMasterStaging",
                column: "YoutubeUrl");

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongStreamerLibrary_SongLibraryId",
                table: "FuncSongStreamerLibrary",
                column: "SongLibraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncSongStreamerLibrary_StreamerProfileId_SongLibraryId",
                table: "FuncSongStreamerLibrary",
                columns: new[] { "StreamerProfileId", "SongLibraryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncSoundAssets_StreamerProfileId",
                table: "FuncSoundAssets",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerDonationHistories_GlobalViewerId",
                table: "FuncViewerDonationHistories",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerDonationHistories_PlatformTransactionId",
                table: "FuncViewerDonationHistories",
                column: "PlatformTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerDonationHistories_StreamerProfileId",
                table: "FuncViewerDonationHistories",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerDonations_GlobalViewerId",
                table: "FuncViewerDonations",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerDonations_StreamerProfileId_GlobalViewerId",
                table: "FuncViewerDonations",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerPoints_GlobalViewerId",
                table: "FuncViewerPoints",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncViewerPoints_StreamerProfileId_GlobalViewerId",
                table: "FuncViewerPoints",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IamfGenosRegistry_StreamerProfileId",
                table: "IamfGenosRegistry",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_IamfParhosCycles_StreamerProfileId_CycleId",
                table: "IamfParhosCycles",
                columns: new[] { "StreamerProfileId", "CycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IamfScenarios_StreamerProfileId",
                table: "IamfScenarios",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LogBroadcastHistory_BroadcastSessionId_LogDate",
                table: "LogBroadcastHistory",
                columns: new[] { "BroadcastSessionId", "LogDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LogChatInteractions_IsCommand_CreatedAt",
                table: "LogChatInteractions",
                columns: new[] { "IsCommand", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogChatInteractions_StreamerProfileId_CreatedAt",
                table: "LogChatInteractions",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogCommandExecutions_GlobalViewerId",
                table: "LogCommandExecutions",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LogCommandExecutions_Keyword_CreatedAt",
                table: "LogCommandExecutions",
                columns: new[] { "Keyword", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogCommandExecutions_StreamerProfileId_CreatedAt",
                table: "LogCommandExecutions",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogIamfVibrations_StreamerProfileId_CreatedAt",
                table: "LogIamfVibrations",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogPointDailySummaries_StreamerProfileId_Date",
                table: "LogPointDailySummaries",
                columns: new[] { "StreamerProfileId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogPointTransactions_GlobalViewerId_CreatedAt",
                table: "LogPointTransactions",
                columns: new[] { "GlobalViewerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogPointTransactions_StreamerProfileId_CreatedAt",
                table: "LogPointTransactions",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LogRouletteResults_GlobalViewerId",
                table: "LogRouletteResults",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LogRouletteResults_RouletteId",
                table: "LogRouletteResults",
                column: "RouletteId");

            migrationBuilder.CreateIndex(
                name: "IX_LogRouletteResults_RouletteItemId",
                table: "LogRouletteResults",
                column: "RouletteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LogRouletteResults_StreamerProfileId_GlobalViewerId",
                table: "LogRouletteResults",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteLog_Status_Cursor",
                table: "LogRouletteResults",
                columns: new[] { "StreamerProfileId", "Status", "Id" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_LogRouletteStats_RouletteId_ItemName",
                table: "LogRouletteStats",
                columns: new[] { "RouletteId", "ItemName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysAvatarSettings_StreamerProfileId",
                table: "SysAvatarSettings",
                column: "StreamerProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysBroadcastSessions_StreamerProfileId_IsActive",
                table: "SysBroadcastSessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SysChzzkCategoryAliases_Alias",
                table: "SysChzzkCategoryAliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_SysChzzkCategoryAliases_CategoryId",
                table: "SysChzzkCategoryAliases",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SysOverlayPresets_StreamerProfileId",
                table: "SysOverlayPresets",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SysPeriodicMessages_StreamerProfileId",
                table: "SysPeriodicMessages",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SysSagaCommandExecutions_CreatedAt",
                table: "SysSagaCommandExecutions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SysSagaCommandExecutions_CurrentState",
                table: "SysSagaCommandExecutions",
                column: "CurrentState");

            migrationBuilder.CreateIndex(
                name: "IX_SysSharedComponents_StreamerProfileId",
                table: "SysSharedComponents",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SysStreamerKnowledges_StreamerProfileId_Keyword",
                table: "SysStreamerKnowledges",
                columns: new[] { "StreamerProfileId", "Keyword" });

            migrationBuilder.CreateIndex(
                name: "IX_SysStreamerPreferences_StreamerProfileId_PreferenceKey",
                table: "SysStreamerPreferences",
                columns: new[] { "StreamerProfileId", "PreferenceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoreStreamerManagers");

            migrationBuilder.DropTable(
                name: "CoreViewerRelations");

            migrationBuilder.DropTable(
                name: "FuncCmdUnified");

            migrationBuilder.DropTable(
                name: "FuncRouletteSpins");

            migrationBuilder.DropTable(
                name: "FuncSongListOmakases");

            migrationBuilder.DropTable(
                name: "FuncSongListQueues");

            migrationBuilder.DropTable(
                name: "FuncSongListSessions");

            migrationBuilder.DropTable(
                name: "FuncSongMasterLibrary");

            migrationBuilder.DropTable(
                name: "FuncSongMasterStaging");

            migrationBuilder.DropTable(
                name: "FuncSongStreamerLibrary");

            migrationBuilder.DropTable(
                name: "FuncSoundAssets");

            migrationBuilder.DropTable(
                name: "FuncViewerDonationHistories");

            migrationBuilder.DropTable(
                name: "FuncViewerDonations");

            migrationBuilder.DropTable(
                name: "FuncViewerPoints");

            migrationBuilder.DropTable(
                name: "IamfGenosRegistry");

            migrationBuilder.DropTable(
                name: "IamfParhosCycles");

            migrationBuilder.DropTable(
                name: "IamfScenarios");

            migrationBuilder.DropTable(
                name: "IamfStreamerSettings");

            migrationBuilder.DropTable(
                name: "LogBroadcastHistory");

            migrationBuilder.DropTable(
                name: "LogChatInteractions");

            migrationBuilder.DropTable(
                name: "LogCommandExecutions");

            migrationBuilder.DropTable(
                name: "LogIamfVibrations");

            migrationBuilder.DropTable(
                name: "LogPointDailySummaries");

            migrationBuilder.DropTable(
                name: "LogPointTransactions");

            migrationBuilder.DropTable(
                name: "LogRouletteResults");

            migrationBuilder.DropTable(
                name: "LogRouletteStats");

            migrationBuilder.DropTable(
                name: "SysAvatarSettings");

            migrationBuilder.DropTable(
                name: "SysChzzkCategoryAliases");

            migrationBuilder.DropTable(
                name: "SysOverlayPresets");

            migrationBuilder.DropTable(
                name: "SysPeriodicMessages");

            migrationBuilder.DropTable(
                name: "SysSagaCommandExecutions");

            migrationBuilder.DropTable(
                name: "SysSharedComponents");

            migrationBuilder.DropTable(
                name: "SysStreamerKnowledges");

            migrationBuilder.DropTable(
                name: "SysStreamerPreferences");

            migrationBuilder.DropTable(
                name: "FuncSongBooks");

            migrationBuilder.DropTable(
                name: "SysBroadcastSessions");

            migrationBuilder.DropTable(
                name: "CoreGlobalViewers");

            migrationBuilder.DropTable(
                name: "FuncRouletteItems");

            migrationBuilder.DropTable(
                name: "SysChzzkCategories");

            migrationBuilder.DropTable(
                name: "FuncRouletteMain");

            migrationBuilder.DropTable(
                name: "CoreStreamerProfiles");
        }
    }
}
