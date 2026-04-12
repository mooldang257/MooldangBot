using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MooldangBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationFieldsToSongQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cost",
                table: "song_list_queues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cost_type",
                table: "song_list_queues",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost",
                table: "song_list_queues");

            migrationBuilder.DropColumn(
                name: "cost_type",
                table: "song_list_queues");
        }
    }
}
