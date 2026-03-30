using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoServer.Migrations
{
    /// <inheritdoc />
    public partial class tournamentAutoRepeatFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRepeatable",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRepeatable",
                table: "Tournaments");
        }
    }
}
