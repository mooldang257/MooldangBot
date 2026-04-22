using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Refine_v6_2_2_ArchAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "sys_settings",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "sys_settings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "sys_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "sys_settings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "song_list_queues",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "song_list_queues",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "song_list_queues",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "song_list_queues",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "song_book_id",
                table: "song_list_queues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "song_list_queues",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "core_global_viewers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "core_global_viewers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_song_list_queues_song_book_id",
                table: "song_list_queues",
                column: "song_book_id");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalViewer_Nickname",
                table: "core_global_viewers",
                column: "nickname");

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_book_main",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropIndex(
                name: "ix_song_list_queues_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropIndex(
                name: "IX_GlobalViewer_Nickname",
                table: "core_global_viewers");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "sys_settings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "sys_settings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "sys_settings");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sys_settings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "core_global_viewers");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "core_global_viewers");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "song_list_queues",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
