using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoServer.Migrations
{
    /// <inheritdoc />
    public partial class addProcessedByAdminToCashDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessedByAdminId",
                table: "CashDeposits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashDeposits_ProcessedByAdminId",
                table: "CashDeposits",
                column: "ProcessedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashDeposits_Players_ProcessedByAdminId",
                table: "CashDeposits",
                column: "ProcessedByAdminId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashDeposits_Players_ProcessedByAdminId",
                table: "CashDeposits");

            migrationBuilder.DropIndex(
                name: "IX_CashDeposits_ProcessedByAdminId",
                table: "CashDeposits");

            migrationBuilder.DropColumn(
                name: "ProcessedByAdminId",
                table: "CashDeposits");
        }
    }
}
