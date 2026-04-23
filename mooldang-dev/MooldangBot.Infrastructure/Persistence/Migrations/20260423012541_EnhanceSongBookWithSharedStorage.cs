using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceSongBookWithSharedStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_song_book_main_core_streamer_profiles_streamer_profile_id",
                table: "song_book_main");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_book_main_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_book_main",
                table: "song_book_main");

            migrationBuilder.DropColumn(
                name: "lyrics",
                table: "func_song_streamer_library");

            migrationBuilder.DropColumn(
                name: "lyrics",
                table: "func_song_master_staging");

            migrationBuilder.DropColumn(
                name: "lyrics",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "genre",
                table: "song_book_main");

            migrationBuilder.RenameTable(
                name: "song_book_main",
                newName: "song_books");

            migrationBuilder.RenameColumn(
                name: "usage_count",
                table: "song_books",
                newName: "sing_count");

            migrationBuilder.RenameIndex(
                name: "ix_song_book_main_streamer_profile_id_id",
                table: "song_books",
                newName: "ix_song_books_streamer_profile_id_id");

            migrationBuilder.AddColumn<string>(
                name: "lyrics_url",
                table: "func_song_streamer_library",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "lyrics_url",
                table: "func_song_master_staging",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "album",
                table: "func_song_master_library",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "func_song_master_library",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "lyrics_url",
                table: "func_song_master_library",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mr_url",
                table: "func_song_master_library",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "reference_url",
                table: "func_song_master_library",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_path",
                table: "func_song_master_library",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "func_song_master_library",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "song_books",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.UpdateData(
                table: "song_books",
                keyColumn: "artist",
                keyValue: null,
                column: "artist",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "artist",
                table: "song_books",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<string>(
                name: "album",
                table: "song_books",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "alias",
                table: "song_books",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "song_books",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_requestable",
                table: "song_books",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_sung_at",
                table: "song_books",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lyrics_url",
                table: "song_books",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mr_url",
                table: "song_books",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "pitch",
                table: "song_books",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "proficiency",
                table: "song_books",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "reference_url",
                table: "song_books",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "song_library_id",
                table: "song_books",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_path",
                table: "song_books",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "song_books",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "title_chosung",
                table: "song_books",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "title_vector",
                table: "song_books",
                type: "VECTOR(768)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_books",
                table: "song_books",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_func_song_master_library_album",
                table: "func_song_master_library",
                column: "album");

            migrationBuilder.CreateIndex(
                name: "ix_func_song_master_library_category",
                table: "func_song_master_library",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_alias",
                table: "song_books",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_category",
                table: "song_books",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_is_requestable",
                table: "song_books",
                column: "is_requestable");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_song_library_id",
                table: "song_books",
                column: "song_library_id");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_title",
                table: "song_books",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_song_books_title_chosung",
                table: "song_books",
                column: "title_chosung");

            migrationBuilder.AddForeignKey(
                name: "fk_song_books_core_streamer_profiles_streamer_profile_id",
                table: "song_books",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_books",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_song_books_core_streamer_profiles_streamer_profile_id",
                table: "song_books");

            migrationBuilder.DropForeignKey(
                name: "fk_song_list_queues_song_books_song_book_id",
                table: "song_list_queues");

            migrationBuilder.DropIndex(
                name: "ix_func_song_master_library_album",
                table: "func_song_master_library");

            migrationBuilder.DropIndex(
                name: "ix_func_song_master_library_category",
                table: "func_song_master_library");

            migrationBuilder.DropPrimaryKey(
                name: "pk_song_books",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_alias",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_category",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_is_requestable",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_song_library_id",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_title",
                table: "song_books");

            migrationBuilder.DropIndex(
                name: "ix_song_books_title_chosung",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "lyrics_url",
                table: "func_song_streamer_library");

            migrationBuilder.DropColumn(
                name: "lyrics_url",
                table: "func_song_master_staging");

            migrationBuilder.DropColumn(
                name: "album",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "category",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "lyrics_url",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "mr_url",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "reference_url",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "thumbnail_path",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "func_song_master_library");

            migrationBuilder.DropColumn(
                name: "album",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "alias",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "category",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "is_requestable",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "last_sung_at",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "lyrics_url",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "mr_url",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "pitch",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "proficiency",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "reference_url",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "song_library_id",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "thumbnail_path",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "title_chosung",
                table: "song_books");

            migrationBuilder.DropColumn(
                name: "title_vector",
                table: "song_books");

            migrationBuilder.RenameTable(
                name: "song_books",
                newName: "song_book_main");

            migrationBuilder.RenameColumn(
                name: "sing_count",
                table: "song_book_main",
                newName: "usage_count");

            migrationBuilder.RenameIndex(
                name: "ix_song_books_streamer_profile_id_id",
                table: "song_book_main",
                newName: "ix_song_book_main_streamer_profile_id_id");

            migrationBuilder.AddColumn<string>(
                name: "lyrics",
                table: "func_song_streamer_library",
                type: "TEXT",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "lyrics",
                table: "func_song_master_staging",
                type: "TEXT",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "lyrics",
                table: "func_song_master_library",
                type: "TEXT",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "song_book_main",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AlterColumn<string>(
                name: "artist",
                table: "song_book_main",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<string>(
                name: "genre",
                table: "song_book_main",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "pk_song_book_main",
                table: "song_book_main",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_song_book_main_core_streamer_profiles_streamer_profile_id",
                table: "song_book_main",
                column: "streamer_profile_id",
                principalTable: "core_streamer_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_song_list_queues_song_book_main_song_book_id",
                table: "song_list_queues",
                column: "song_book_id",
                principalTable: "song_book_main",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
