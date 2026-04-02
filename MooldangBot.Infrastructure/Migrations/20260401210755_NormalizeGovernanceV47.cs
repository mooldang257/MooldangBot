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
            // [v4.7.12] Universal Physical Unification: Governance Domain
            migrationBuilder.Sql(@"
                -- 1. 대상 테이블 물리적 변환 (Collation 일치화)
                -- JOIN 시 충돌을 방지하기 위해 데이터를 건드리기 전에 먼저 변환합니다.
                ALTER TABLE streamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE streameromakases CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE streamermanagers CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE globalviewers CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

                SET @dbname = DATABASE();

                -- 2. [streameromakases] 신규 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE streameromakases ADD StreamerProfileId int NULL; END IF;

                -- 물리적 형식이 통일되었으므로 표준 JOIN 수행
                UPDATE streameromakases o JOIN streamerprofiles p ON o.ChzzkUid = p.ChzzkUid SET o.StreamerProfileId = p.Id;
                DELETE FROM streameromakases WHERE StreamerProfileId IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streameromakases", nullable: false);

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND COLUMN_NAME = 'ChzzkUid');
                IF @exist > 0 THEN ALTER TABLE streameromakases DROP COLUMN ChzzkUid; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'streameromakases' AND CONSTRAINT_NAME = 'FK_streameromakases_streamerprofiles_StreamerProfileId');
                IF @exist > 0 THEN ALTER TABLE streameromakases DROP FOREIGN KEY FK_streameromakases_streamerprofiles_StreamerProfileId; END IF;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_streameromakases_streamerprofiles_StreamerProfileId",
                table: "streameromakases",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 3. [streamermanagers] 테이블 정규화
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE streamermanagers ADD StreamerProfileId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'streamermanagers' AND COLUMN_NAME = 'GlobalViewerId');
                IF @exist = 0 THEN ALTER TABLE streamermanagers ADD GlobalViewerId int NULL; END IF;

                -- Mapping (물리적 일치 상태이므로 오류 없음)
                UPDATE streamermanagers m JOIN streamerprofiles p ON m.StreamerChzzkUid = p.ChzzkUid SET m.StreamerProfileId = p.Id;
                
                -- GlobalViewerId 생성 및 매핑 코드는 유지 (데이터 정합성)
                INSERT IGNORE INTO globalviewers (ViewerUidHash, ViewerUid)
                SELECT UPPER(HEX(SHA2(CONCAT(LOWER(ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256))), 'MIGRATED_MANAGER'
                FROM streamermanagers;

                UPDATE streamermanagers m JOIN globalviewers g ON g.ViewerUidHash = UPPER(HEX(SHA2(CONCAT(LOWER(m.ManagerChzzkUid), 'MooldangBot_Secure_Salt_2026'), 256))) SET m.GlobalViewerId = g.Id;

                DELETE FROM streamermanagers WHERE StreamerProfileId IS NULL OR GlobalViewerId IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(name: "StreamerProfileId", table: "streamermanagers", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "GlobalViewerId", table: "streamermanagers", nullable: false);

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
        }
    }
}
