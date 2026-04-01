using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeGovernanceV47 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. [streameromakases] 테이블 정규화
            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "streameromakases",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE streameromakases o
                JOIN streamerprofiles p ON LOWER(o.ChzzkUid) = LOWER(p.ChzzkUid)
                SET o.StreamerProfileId = p.Id;
            ");

            migrationBuilder.Sql("DELETE FROM streameromakases WHERE StreamerProfileId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "StreamerProfileId",
                table: "streameromakases",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "ChzzkUid",
                table: "streameromakases");

            migrationBuilder.AddForeignKey(
                name: "FK_streameromakases_streamerprofiles_StreamerProfileId",
                table: "streameromakases",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 2. [streamermanagers] 테이블 정규화
            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "streamermanagers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GlobalViewerId",
                table: "streamermanagers",
                type: "int",
                nullable: true);

            // 2-1. 매니저 정보 보존을 위한 GlobalViewers 선제 생성 (존재하지 않는 경우)
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO globalviewers (ViewerUidHash, ViewerUid)
                SELECT 
                    UPPER(HEX(SHA2(CONCAT(LOWER(ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256))),
                    'MIGRATED_MANAGER'
                FROM streamermanagers;
            ");

            // 2-2. StreamerProfileId 매핑
            migrationBuilder.Sql(@"
                UPDATE streamermanagers m
                JOIN streamerprofiles p ON LOWER(m.StreamerChzzkUid) = LOWER(p.ChzzkUid)
                SET m.StreamerProfileId = p.Id;
            ");

            // 2-3. GlobalViewerId 매핑
            migrationBuilder.Sql(@"
                UPDATE streamermanagers m
                JOIN globalviewers g ON g.ViewerUidHash = UPPER(HEX(SHA2(CONCAT(LOWER(m.ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256)))
                SET m.GlobalViewerId = g.Id;
            ");

            migrationBuilder.Sql("DELETE FROM streamermanagers WHERE StreamerProfileId IS NULL OR GlobalViewerId IS NULL;");

            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamermanagers", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "GlobalViewerId", table: "streamermanagers", nullable: false);

            migrationBuilder.DropColumn(name: "StreamerChzzkUid", table: "streamermanagers");
            migrationBuilder.DropColumn(name: "ManagerChzzkUid", table: "streamermanagers");

            migrationBuilder.CreateIndex(
                name: "IX_streamermanagers_StreamerProfileId_GlobalViewerId",
                table: "streamermanagers",
                columns: new[] { "StreamerProfileId", "GlobalViewerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_streamermanagers_streamerprofiles_StreamerProfileId",
                table: "streamermanagers",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamermanagers_globalviewers_GlobalViewerId",
                table: "streamermanagers",
                column: "GlobalViewerId",
                principalTable: "globalviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 역마이그레이션 로직 (필요시 수동 복구)
            migrationBuilder.DropForeignKey(name: "FK_streamermanagers_globalviewers_GlobalViewerId", table: "streamermanagers");
            migrationBuilder.DropForeignKey(name: "FK_streamermanagers_streamerprofiles_StreamerProfileId", table: "streamermanagers");
            migrationBuilder.DropForeignKey(name: "FK_streameromakases_streamerprofiles_StreamerProfileId", table: "streameromakases");

            migrationBuilder.DropIndex(name: "IX_streamermanagers_StreamerProfileId_GlobalViewerId", table: "streamermanagers");

            migrationBuilder.AddColumn<string>(name: "ChzzkUid", table: "streameromakases", type: "varchar(50)", nullable: false);
            migrationBuilder.AddColumn<string>(name: "StreamerChzzkUid", table: "streamermanagers", type: "varchar(50)", nullable: false);
            migrationBuilder.AddColumn<string>(name: "ManagerChzzkUid", table: "streamermanagers", type: "varchar(50)", nullable: false);
            
            // 데이터 역복구 SQL은 생략 (운영 환경에서는 주의 필요)
            
            migrationBuilder.DropColumn(name: "StreamerProfileId", table: "streameromakases");
            migrationBuilder.DropColumn(name: "GlobalViewerId", table: "streamermanagers");
            migrationBuilder.DropColumn(name: "StreamerProfileId", table: "streamermanagers");
        }
    }
}
