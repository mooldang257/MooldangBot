using MooldangBot.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouletteMetaDataManualFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 💡 시니어 파트너 조언: 기존에 존재하는 컬럼/테이블로 인한 충돌 방지를 위해 신규 필드만 추가
            migrationBuilder.AddColumn<int>(
                name: "RouletteId",
                table: "roulettelogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RouletteName",
                table: "roulettelogs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_roulettelogs_RouletteId",
                table: "roulettelogs",
                column: "RouletteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roulettelogs_RouletteId",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "RouletteId",
                table: "roulettelogs");

            migrationBuilder.DropColumn(
                name: "RouletteName",
                table: "roulettelogs");
        }
    }
}
