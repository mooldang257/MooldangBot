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
            // [v4.4.5] Emergency Repair: Idempotent migration for Roulette
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                
                -- 1. 외래 키 제약 조건 선제거 (삭제 블로킹 방지)
                -- rouletteitems -> roulettes
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'rouletteitems' AND CONSTRAINT_NAME = 'FK_rouletteitems_roulettes_RouletteId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE rouletteitems DROP FOREIGN KEY FK_rouletteitems_roulettes_RouletteId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- roulettelogs -> globalviewers, rouletteitems, streamerprofiles
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND CONSTRAINT_NAME = 'FK_roulettelogs_globalviewers_GlobalViewerId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettelogs DROP FOREIGN KEY FK_roulettelogs_globalviewers_GlobalViewerId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND CONSTRAINT_NAME = 'FK_roulettelogs_rouletteitems_RouletteItemId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettelogs DROP FOREIGN KEY FK_roulettelogs_rouletteitems_RouletteItemId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettelogs' AND CONSTRAINT_NAME = 'FK_roulettelogs_streamerprofiles_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettelogs DROP FOREIGN KEY FK_roulettelogs_streamerprofiles_StreamerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- roulettespins -> globalviewers, streamerprofiles
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND CONSTRAINT_NAME = 'FK_roulettespins_globalviewers_GlobalViewerId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettespins DROP FOREIGN KEY FK_roulettespins_globalviewers_GlobalViewerId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettespins' AND CONSTRAINT_NAME = 'FK_roulettespins_streamerprofiles_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettespins DROP FOREIGN KEY FK_roulettespins_streamerprofiles_StreamerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- roulettes -> streamerprofiles
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'roulettes' AND CONSTRAINT_NAME = 'FK_roulettes_streamerprofiles_StreamerProfileId');
                SET @sql = IF(@exist > 0, 'ALTER TABLE roulettes DROP FOREIGN KEY FK_roulettes_streamerprofiles_StreamerProfileId', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- 2. 테이블 삭제
                DROP TABLE IF EXISTS rouletteitems;
                DROP TABLE IF EXISTS roulettelogs;
                DROP TABLE IF EXISTS roulettespins;
                DROP TABLE IF EXISTS roulettes;
            ");

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
