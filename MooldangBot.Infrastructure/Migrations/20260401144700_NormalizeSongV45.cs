using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSongV45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [v4.5.10] Universal Physical Unification: Song Domain
            migrationBuilder.Sql(@"
                -- 정렬 규칙 통일을 위해 대상 테이블 물리적 변환
                ALTER TABLE songqueues CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE songbooks CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE songlistsessions CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE steamerprofiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                ALTER TABLE globalviewers CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

                SET @dbname = DATABASE();
                
                -- 1. 기존 인덱스 제거 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_ChzzkUid');
                IF @exist > 0 THEN DROP INDEX IX_songqueues_ChzzkUid ON songqueues; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND INDEX_NAME = 'IX_songqueues_ChzzkUid_Id');
                IF @exist > 0 THEN DROP INDEX IX_songqueues_ChzzkUid_Id ON songqueues; END IF;

                -- 2. 새 컬럼 추가 방어
                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'GlobalViewerId');
                IF @exist = 0 THEN ALTER TABLE songqueues ADD GlobalViewerId int NULL; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE songqueues ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songlistsessions' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE songlistsessions ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;

                SET @exist = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songbooks' AND COLUMN_NAME = 'StreamerProfileId');
                IF @exist = 0 THEN ALTER TABLE songbooks ADD StreamerProfileId int NOT NULL DEFAULT 0; END IF;

                -- 3. 데이터 매핑 (물리적 형식이 통일되었으므로 순수 JOIN 수행)
                SET @has_sq = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'songqueues' AND COLUMN_NAME = 'ChzzkUid');
                SET @has_sp = (SELECT COUNT(*) FROM streamerprofiles);
                IF @has_sq > 0 AND @has_sp > 0 THEN
                    UPDATE songqueues s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                    UPDATE songlistsessions s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                    UPDATE songbooks s JOIN streamerprofiles p ON s.ChzzkUid = p.ChzzkUid SET s.StreamerProfileId = p.Id;
                END IF;

                -- 4. 정합성 정화
                IF @has_sp > 0 THEN
                    SET @min_streamer = (SELECT MIN(Id) FROM streamerprofiles);
                    UPDATE songqueues SET StreamerProfileId = @min_streamer WHERE StreamerProfileId = 0 OR StreamerProfileId IS NULL;
                    UPDATE songlistsessions SET StreamerProfileId = @min_streamer WHERE StreamerProfileId = 0 OR StreamerProfileId IS NULL;
                    UPDATE songbooks SET StreamerProfileId = @min_streamer WHERE StreamerProfileId = 0 OR StreamerProfileId IS NULL;
                END IF;
            ");

            // 이후 인덱스 생성 및 FK 설정은 표준 메서드로 진행
            migrationBuilder.CreateIndex(
                name: "IX_songqueues_GlobalViewerId",
                table: "songqueues",
                column: "GlobalViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_songqueues_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_songqueues_streamerprofiles_StreamerProfileId",
                table: "songqueues",
                column: "StreamerProfileId",
                principalTable: "streamerprofiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_songbooks_streamerprofiles_StreamerProfileId",
                table: "songbooks",
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
