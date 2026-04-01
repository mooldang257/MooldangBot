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
            // 0. [테이블 이름 변경]
            migrationBuilder.RenameTable(
                name: "StreamerKnowledges",
                newName: "streamerknowledges");

            // 1. [컬럼 추가] Nullable로 추가하여 데이터 이관 준비
            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "broadcastsessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "iamf_vibration_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "iamf_streamer_settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "streamerknowledges",
                type: "int",
                nullable: true);

            // 2. [데이터 무손실 이관 SQL]
            // ChzzkUid 문자열을 기반으로 StreamerProfile의 ID(정수)를 찾아 매핑합니다.
            migrationBuilder.Sql(@"
                UPDATE broadcastsessions s 
                JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid 
                SET s.StreamerProfileId = p.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE iamf_vibration_logs s 
                JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid 
                SET s.StreamerProfileId = p.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE iamf_streamer_settings s 
                JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid 
                SET s.StreamerProfileId = p.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE streamerknowledges s 
                JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid 
                SET s.StreamerProfileId = p.Id;
            ");

            // 3. [잔존 데이터 정화] 프로필이 없는 유령 데이터 대량 삭제 (정규화 준수)
            migrationBuilder.Sql("DELETE FROM broadcastsessions WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM iamf_vibration_logs WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM iamf_streamer_settings WHERE StreamerProfileId IS NULL;");
            migrationBuilder.Sql("DELETE FROM streamerknowledges WHERE StreamerProfileId IS NULL;");

            // 4. [컬럼 속성 업데이트] Not Null로 변경
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "broadcastsessions", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "iamf_vibration_logs", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "iamf_streamer_settings", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamerknowledges", nullable: false);

            // 5. [제약 조건 정화] 기존 PK/컬럼 제거
            migrationBuilder.DropPrimaryKey(name: "PK_StreamerKnowledges", table: "streamerknowledges");
            migrationBuilder.DropPrimaryKey(name: "PK_iamf_streamer_settings", table: "iamf_streamer_settings");

            migrationBuilder.DropColumn(name: "ChzzkUid", table: "broadcastsessions");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "iamf_vibration_logs");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "iamf_streamer_settings");
            migrationBuilder.DropColumn(name: "ChzzkUid", table: "streamerknowledges");

            // 6. [신규 PK/인덱스 설정]
            migrationBuilder.AddPrimaryKey(
                name: "PK_streamerknowledges",
                table: "streamerknowledges",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_iamf_streamer_settings",
                table: "iamf_streamer_settings",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_streamerknowledges_StreamerProfileId_Keyword",
                table: "streamerknowledges",
                columns: new[] { "StreamerProfileId", "Keyword" });

            migrationBuilder.CreateIndex(
                name: "IX_iamf_vibration_logs_StreamerProfileId_CreatedAt",
                table: "iamf_vibration_logs",
                columns: new[] { "StreamerProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_broadcastsessions_StreamerProfileId_IsActive",
                table: "broadcastsessions",
                columns: new[] { "StreamerProfileId", "IsActive" });

            // 7. [외래 키 설정]
            migrationBuilder.AddForeignKey(
                name: "FK_broadcastsessions_streamerprofiles_StreamerProfileId",
                table: "broadcastsessions",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_streamer_settings_streamerprofiles_StreamerProfileId",
                table: "iamf_streamer_settings",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId",
                table: "iamf_vibration_logs",
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
            // 역순 복구 로직 (생략 또는 간단 구현)
            migrationBuilder.DropForeignKey(name: "FK_broadcastsessions_streamerprofiles_StreamerProfileId", table: "broadcastsessions");
            migrationBuilder.DropForeignKey(name: "FK_iamf_streamer_settings_streamerprofiles_StreamerProfileId", table: "iamf_streamer_settings");
            migrationBuilder.DropForeignKey(name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId", table: "iamf_vibration_logs");
            migrationBuilder.DropForeignKey(name: "FK_streamerknowledges_streamerprofiles_StreamerProfileId", table: "streamerknowledges");

            migrationBuilder.DropPrimaryKey(name: "PK_streamerknowledges", table: "streamerknowledges");
            migrationBuilder.DropPrimaryKey(name: "PK_iamf_streamer_settings", table: "iamf_streamer_settings");

            // ChzzkUid 컬럼 복구 및 데이터 역이관 SQL은 복잡하므로 필요시 수동 구현 권장
            // 여기서는 단순 컬럼 복구만 정의
            migrationBuilder.AddColumn<string>(name: "ChzzkUid", table: "broadcastsessions", type: "longtext", nullable: false);
            migrationBuilder.AddColumn<string>(name: "ChzzkUid", table: "iamf_vibration_logs", type: "varchar(50)", maxLength: 50, nullable: false);
            migrationBuilder.AddColumn<string>(name: "ChzzkUid", table: "iamf_streamer_settings", type: "varchar(50)", maxLength: 50, nullable: false);
            migrationBuilder.AddColumn<string>(name: "ChzzkUid", table: "streamerknowledges", type: "varchar(50)", maxLength: 50, nullable: false);

            migrationBuilder.RenameTable(name: "streamerknowledges", newName: "StreamerKnowledges");
        }

    }
}
