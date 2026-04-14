using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDataProtectionKeysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [물멍]: DB 초기화 스트레스 없는 기지 정화를 위해, 더 이상 사용하지 않는 DataProtectionKeys 테이블을 날려버립니다.
            migrationBuilder.Sql("DROP TABLE IF EXISTS DataProtectionKeys;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // [물멍]: 파일 기반 영속화로 이미 보금자리를 옮겼으므로, 강제로 롤백하더라도 테이블을 복구하지 않습니다.
            // 필요 시 수동으로 생성 가능하므로 비워둡니다.
        }
    }
}
