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
            // [v4.8.12] Universal Physical Unification: Philosophy & Session Domain
            migrationBuilder.Sql(@"
                -- 정렬 규칙 통일을 위해 대상 테이블 물리적 변환
                -- JOIN 연산 시 Illegal mix of collations 오류를 원천 차단합니다.
                ALTER TABLE broadcastsessions CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE iamf_vibration_logs CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE iamf_streamer_settings CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE StreamerKnowledges CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE streamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
            ");

            // 0. [테이블 이름 변경]
            migrationBuilder.RenameTable(
                name: "StreamerKnowledges",
                newName: "streamerknowledges");

            // 1. [컬럼 추가]
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", type: "int", nullable: true);

            // 2. [데이터 이관] 물리적 형식이 통일되었으므로 표준 JOIN 수행
            migrationBuilder.Sql(@"
                UPDATE broadcastsessions s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE iamf_vibration_logs s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE iamf_streamer_settings s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                UPDATE streamerknowledges s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
            ");

            // 3. [정화]
            migrationBuilder.Sql("DELETE FROM broadcastsessions WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM streamerknowledges WHERE StreamerProfileId IS NULL;");

            // 4. [속성 업데이트 및 제약 조건]
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", nullable: false);

            migrationBuilder.DropColumn(name: "ChzzkUid", table: "broadcastsessions");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "streamerknowledges");

            migrationBuilder.AddForeignKey(
                name: "FK_broadcastsessions_streamerprofiles_StreamerProfileId",
                table: "broadcastsessions",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_streamerknowledges_streamerprofiles_StreamerProfileId",
                table: "streamerknowledges",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
