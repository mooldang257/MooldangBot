using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhilosophyResilience_v4_9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId",
                table: "iamf_vibration_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_iamf_parhos_cycles",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_iamf_genos_registry",
                table: "iamf_genos_registry");

            migrationBuilder.DropColumn(
                name: "ParhosId",
                table: "iamf_parhos_cycles");

            migrationBuilder.AddColumn<string>(
                name: "DelYn",
                table: "streamerprofiles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "N")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MasterUseYn",
                table: "streamerprofiles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "Y")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "iamf_scenarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "CycleId",
                table: "iamf_parhos_cycles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "iamf_parhos_cycles",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "iamf_parhos_cycles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "iamf_genos_registry",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "StreamerProfileId",
                table: "iamf_genos_registry",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // [v4.9] 존재의 보존: 관리자 계정 생성 및 기존 데이터 이관 SQL
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO streamerprofiles (Id, ChzzkUid, ChannelName, DelYn, MasterUseYn, OmakaseCommand, SongCommand, AttendanceCommands, AttendanceReply, PointCheckCommand, PointCheckReply, IsBotEnabled, IsOmakaseEnabled, OmakaseCount, OmakasePrice, SongPrice, PointPerChat, PointPerDonation1000, PointPerAttendance)
                VALUES (1, 'SYSTEM_ADMIN', 'SystemAdmin', 'N', 'Y', '!물마카세', '!신청', '출석', '{닉네임}님 출석 고마워요!', '!포인트', '🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다! (누적 출석: {출석일수}일)', 0, 1, 0, 1000, 0, 1, 10, 10);
            ");

            migrationBuilder.Sql("UPDATE iamf_scenarios SET StreamerProfileId = 1 WHERE StreamerProfileId = 0;");
            migrationBuilder.Sql("UPDATE iamf_parhos_cycles SET StreamerProfileId = 1 WHERE StreamerProfileId = 0;");
            migrationBuilder.Sql("UPDATE iamf_genos_registry SET StreamerProfileId = 1 WHERE StreamerProfileId = 0;");
            migrationBuilder.Sql("UPDATE streamerprofiles SET DelYn = 'N', MasterUseYn = 'Y' WHERE DelYn = '' OR MasterUseYn = '';");

            migrationBuilder.AddPrimaryKey(
                name: "PK_iamf_parhos_cycles",
                table: "iamf_parhos_cycles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_iamf_genos_registry",
                table: "iamf_genos_registry",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_iamf_scenarios_StreamerProfileId",
                table: "iamf_scenarios",
                column: "StreamerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_iamf_parhos_cycles_StreamerProfileId_CycleId",
                table: "iamf_parhos_cycles",
                columns: new[] { "StreamerProfileId", "CycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iamf_genos_registry_StreamerProfileId",
                table: "iamf_genos_registry",
                column: "StreamerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_genos_registry_streamerprofiles_StreamerProfileId",
                table: "iamf_genos_registry",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_parhos_cycles_streamerprofiles_StreamerProfileId",
                table: "iamf_parhos_cycles",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_scenarios_streamerprofiles_StreamerProfileId",
                table: "iamf_scenarios",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId",
                table: "iamf_vibration_logs",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_iamf_genos_registry_streamerprofiles_StreamerProfileId",
                table: "iamf_genos_registry");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_parhos_cycles_streamerprofiles_StreamerProfileId",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_scenarios_streamerprofiles_StreamerProfileId",
                table: "iamf_scenarios");

            migrationBuilder.DropForeignKey(
                name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId",
                table: "iamf_vibration_logs");

            migrationBuilder.DropIndex(
                name: "IX_iamf_scenarios_StreamerProfileId",
                table: "iamf_scenarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_iamf_parhos_cycles",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropIndex(
                name: "IX_iamf_parhos_cycles_StreamerProfileId_CycleId",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_iamf_genos_registry",
                table: "iamf_genos_registry");

            migrationBuilder.DropIndex(
                name: "IX_iamf_genos_registry_StreamerProfileId",
                table: "iamf_genos_registry");

            migrationBuilder.DropColumn(
                name: "DelYn",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "MasterUseYn",
                table: "streamerprofiles");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "iamf_scenarios");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "iamf_parhos_cycles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "iamf_genos_registry");

            migrationBuilder.DropColumn(
                name: "StreamerProfileId",
                table: "iamf_genos_registry");

            migrationBuilder.AlterColumn<int>(
                name: "CycleId",
                table: "iamf_parhos_cycles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "ParhosId",
                table: "iamf_parhos_cycles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_iamf_parhos_cycles",
                table: "iamf_parhos_cycles",
                column: "CycleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_iamf_genos_registry",
                table: "iamf_genos_registry",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_iamf_vibration_logs_streamerprofiles_StreamerProfileId",
                table: "iamf_vibration_logs",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
