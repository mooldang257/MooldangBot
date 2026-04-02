using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhilosophyNormalization_v4_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.8.11] Universal Clean Restoration: Logic only (Physical conversion at model level)
            // 0. [테이블 이름 변경]
            migrationBuilder.RenameTable(
                name: "StreamerKnowledges",
                newName: "streamerknowledges");

            // 1. [컬럼 추가]
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", type: "int", nullable: true);

            // 2. [데이터 이관] 전역 Collation이 일치하므로 표준 JOIN 수행
            migrationBuilder.Sql(@"
                UPDATE broadcastsessions s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");
            migrationBuilder.Sql(@"
                UPDATE iamf_vibration_logs s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");
            migrationBuilder.Sql(@"
                UPDATE iamf_streamer_settings s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");
            migrationBuilder.Sql(@"
                UPDATE streamerknowledges s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");

            // 3. [정화]
            migrationBuilder.Sql("DELETE FROM broadcastsessions WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM iamf_vibration_logs WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM iamf_streamer_settings WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM streamerknowledges WHERE StreamerProfileId IS NULL;");

            // 4. [속성 업데이트]
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", nullable: false);

            // 5. [제약 조건 정리]
            migrationBuilder.DropPrimaryKey(name: "PK_StreamerKnowledges", table: "streamerknowledges");
            migrationBuilder.DropPrimaryKey(name: "PK_iamf_streamer_settings", table: "iamf_streamer_settings");

            migrationBuilder.DropColumn(name: "ChzzkUid", table: "broadcastsessions");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "iamf_vibration_logs");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "iamf_streamer_settings");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "streamerknowledges");

            // 6. [신규 PK/인덱스]
            migrationBuilder.AddPrimaryKey(name: "PK_streamerknowledges", table: "streamerknowledges", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_iamf_streamer_settings", table: "iamf_streamer_settings", column: "StreamerProfileId");

            migrationBuilder.CreateIndex(name: "IX_streamerknowledges_StreamerProfileId_Keyword", table: "streamerknowledges", columns: new[] { "StreamerProfileId", "Keyword" });
            migrationBuilder.CreateIndex(name: "IX_iamf_vibration_logs_StreamerProfileId_CreatedAt", table: "iamf_vibration_logs", columns: new[] { "StreamerProfileId", "CreatedAt" });
            migrationBuilder.CreateIndex(name: "IX_broadcastsessions_StreamerProfileId_IsActive", table: "broadcastsessions", columns: new[] { "StreamerProfileId", "IsActive" });

            // 7. [외래 키]
            migrationBuilder.AddForeignKey(name: "FK_broadcastsessions_streamerprofiles_StreamerProfileId", table: "broadcastsessions", column: "StreamerProfileId", principalTable: "streamerprofiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_iamf_streamer_settings_streamerprofiles_StreamerProfileId", table: "iamf_streamer_settings", column: "StreamerProfileId", principalTable: "streamerprofiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId", table: "iamf_vibration_logs", column: "StreamerProfileId", principalTable: "streamerprofiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_streamerknowledges_streamerprofiles_StreamerProfileId", table: "streamerknowledges", column: "StreamerProfileId", principalTable: "streamerprofiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
