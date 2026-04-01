using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeConfigV46 : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.6] Drop and Create for a clean slate as requested
            migrationBuilder.DropTable(name: "avatarsettings");
            migrationBuilder.DropTable(name: "overlaypresets");
            migrationBuilder.DropTable(name: "periodicmessages");
            migrationBuilder.DropTable(name: "sharedcomponents");

            migrationBuilder.CreateTable(
                name: "avatarsettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowNickname = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowChat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisappearTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    WalkingImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StopImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InteractionImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatarsettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_avatarsettings_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "overlaypresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    PresetName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overlaypresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overlaypresets_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "periodicmessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    MinChatCount = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodicmessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_periodicmessages_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sharedcomponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_sharedcomponents_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_avatarsettings_StreamerProfileId",
                table: "avatarsettings",
                column: "StreamerProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_overlaypresets_StreamerProfileId",
                table: "overlaypresets",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_periodicmessages_StreamerProfileId",
                table: "periodicmessages",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_sharedcomponents_StreamerProfileId",
                table: "sharedcomponents",
                column: "StreamerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_avatarsettings_streamerprofiles_StreamerProfileId",
                table: "avatarsettings");

            migrationBuilder.DropForeignKey(
                name: "FK_overlaypresets_streamerprofiles_StreamerProfileId",
                table: "overlaypresets");

            migrationBuilder.DropForeignKey(
                name: "FK_periodicmessages_streamerprofiles_StreamerProfileId",
                table: "periodicmessages");

            migrationBuilder.DropForeignKey(
                name: "FK_sharedcomponents_streamerprofiles_StreamerProfileId",
                table: "sharedcomponents");

            migrationBuilder.DropForeignKey(
                name: "FK_streamermanagers_globalviewers_GlobalViewerId",
                table: "streamermanagers");

            migrationBuilder.DropForeignKey(
                name: "FK_streamermanagers_streamerprofiles_StreamerProfileId",
                table: "streamermanagers");

            migrationBuilder.DropForeignKey(
                name: "FK_streameromakases_streamerprofiles_StreamerProfileId",
                table: "streameromakases");

            migrationBuilder.DropIndex(
                name: "IX_streameromakases_StreamerProfileId",
                table: "streameromakases");

            migrationBuilder.DropIndex(
                name: "IX_streamermanagers_GlobalViewerId",
                table: "streamermanagers");

            migrationBuilder.DropIndex(
                name: "IX_streamermanagers_StreamerProfileId_GlobalViewerId",
                table: "streamermanagers");

            migrationBuilder.DropIndex(
                name: "IX_sharedcomponents_StreamerProfileId",
                table: "sharedcomponents");

            migrationBuilder.DropIndex(
                name: "IX_periodicmessages_StreamerProfileId",
                table: "periodicmessages");

            migrationBuilder.DropIndex(
                name: "IX_overlaypresets_StreamerProfileId",
                table: "overlaypresets");

            migrationBuilder.DropIndex(
                name: "IX_avatarsettings_StreamerProfileId",
                table: "avatarsettings");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "streameromakases");

            migrationBuilder.DropColumn(
                name: "GlobalViewerId",
                table: "streamermanagers");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "streamermanagers");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "sharedcomponents");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "periodicmessages");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "overlaypresets");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "avatarsettings");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "streameromakases",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManagerChzzkUid",
                table: "streamermanagers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StreamerChzzkUid",
                table: "streamermanagers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "sharedcomponents",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "periodicmessages",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "overlaypresets",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChzzkUid",
                table: "avatarsettings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_streameromakases_ChzzkUid",
                table: "streameromakases",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_sharedcomponents_ChzzkUid",
                table: "sharedcomponents",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_periodicmessages_ChzzkUid",
                table: "periodicmessages",
                column: "ChzzkUid");

            migrationBuilder.CreateIndex(
                name: "IX_avatarsettings_ChzzkUid",
                table: "avatarsettings",
                column: "ChzzkUid",
                unique: true);
        }
    }
}
