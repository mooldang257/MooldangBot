using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRouletteV44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. [정문화 전야]: 기존 테이블들을 관계 역순으로 삭제합니다.
            migrationBuilder.DropTable(name: "rouletteitems");
            migrationBuilder.DropTable(name: "roulettelogs");
            migrationBuilder.DropTable(name: "roulettespins");
            migrationBuilder.DropTable(name: "roulettes");

            // 2. [오시리스의 재탄생]: 정문화된 스키마로 테이블을 재생성합니다.
            
            // Roulette
            migrationBuilder.CreateTable(
                name: "roulettes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulettes_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // RouletteItem
            migrationBuilder.CreateTable(
                name: "rouletteitems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci"),
                    Probability = table.Column<double>(type: "double", nullable: false),
                    Probability10x = table.Column<double>(type: "double", nullable: false),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true, collation: "utf8mb4_unicode_ci"),
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

            // RouletteLog
            migrationBuilder.CreateTable(
                name: "roulettelogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    RouletteItemId = table.Column<int>(type: "int", nullable: true),
                    RouletteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci"),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci"),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci"),
                    IsMission = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettelogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulettelogs_globalviewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "globalviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roulettelogs_rouletteitems_RouletteItemId",
                        column: x => x.RouletteItemId,
                        principalTable: "rouletteitems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_roulettelogs_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // RouletteSpin
            migrationBuilder.CreateTable(
                name: "roulettespins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci"),
                    StreamerProfileId = table.Column<int>(type: "int", nullable: false),
                    RouletteId = table.Column<int>(type: "int", nullable: false),
                    GlobalViewerId = table.Column<int>(type: "int", nullable: false),
                    ViewerNickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci"),
                    ResultsJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci"),
                    Summary = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci"),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roulettespins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roulettespins_globalviewers_GlobalViewerId",
                        column: x => x.GlobalViewerId,
                        principalTable: "globalviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roulettespins_streamerprofiles_StreamerProfileId",
                        column: x => x.StreamerProfileId,
                        principalTable: "streamerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 3. 인덱스 생성
            migrationBuilder.CreateIndex(name: "IX_roulettes_StreamerProfileId", table: "roulettes", column: "StreamerProfileId");
            migrationBuilder.CreateIndex(name: "IX_rouletteitems_RouletteId", table: "rouletteitems", column: "RouletteId");
            migrationBuilder.CreateIndex(name: "IX_roulettelogs_GlobalViewerId", table: "roulettelogs", column: "GlobalViewerId");
            migrationBuilder.CreateIndex(name: "IX_roulettelogs_RouletteItemId", table: "roulettelogs", column: "RouletteItemId");
            migrationBuilder.CreateIndex(name: "IX_roulettelogs_StreamerProfileId", table: "roulettelogs", column: "StreamerProfileId");
            migrationBuilder.CreateIndex(name: "IX_roulettespins_GlobalViewerId", table: "roulettespins", column: "GlobalViewerId");
            migrationBuilder.CreateIndex(name: "IX_roulettespins_StreamerProfileId", table: "roulettespins", column: "StreamerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down 시에도 Drop 후 재생성 (이전 스키마 복구 코드는 복잡하므로 단순 Drop 처리 권장하나, 필요시 수동 작성)
            migrationBuilder.DropTable(name: "rouletteitems");
            migrationBuilder.DropTable(name: "roulettelogs");
            migrationBuilder.DropTable(name: "roulettespins");
            migrationBuilder.DropTable(name: "roulettes");
        }
    }
}
