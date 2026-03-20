using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAvatarImageAndLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriberAvatarUrl",
                table: "avatarsettings");

            migrationBuilder.DropColumn(
                name: "Tier2AvatarUrl",
                table: "avatarsettings");

            migrationBuilder.RenameColumn(
                name: "NormalAvatarUrl",
                table: "avatarsettings",
                newName: "WalkingImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "InteractionImageUrl",
                table: "avatarsettings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StopImageUrl",
                table: "avatarsettings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InteractionImageUrl",
                table: "avatarsettings");

            migrationBuilder.DropColumn(
                name: "StopImageUrl",
                table: "avatarsettings");

            migrationBuilder.RenameColumn(
                name: "WalkingImageUrl",
                table: "avatarsettings",
                newName: "NormalAvatarUrl");

            migrationBuilder.AddColumn<string>(
                name: "SubscriberAvatarUrl",
                table: "avatarsettings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Tier2AvatarUrl",
                table: "avatarsettings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
