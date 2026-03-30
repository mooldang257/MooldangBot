using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
/*
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
*/

/*
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
*/

/*
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
*/

/*
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
*/

/*
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
*/

/*
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
*/

/*
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
*/

/*
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
*/

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

/*
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
                name: "IX_songbooks_ChzzkUid_Id",
                table: "songbooks",
                columns: new[] { "ChzzkUid", "Id" },
                descending: new[] { false, true });
*/
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
/*
            migrationBuilder.DropTable(
                name: "broadcastsessions");

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
*/

/*
            migrationBuilder.DropTable(
                name: "roulettelogs");

            migrationBuilder.DropTable(
                name: "songbooks");
*/

            migrationBuilder.DropTable(
                name: "StreamerKnowledges");
        }
    }
}
