using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomCodeToChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomCode",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoomCode",
                table: "ChatMessages");
        }
    }
}
