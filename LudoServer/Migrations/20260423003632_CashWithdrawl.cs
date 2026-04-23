using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoServer.Migrations
{
    /// <inheritdoc />
    public partial class CashWithdrawl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashWithdrawals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    PayoutMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedByAdminId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashWithdrawals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashWithdrawals_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashWithdrawals_Players_ProcessedByAdminId",
                        column: x => x.ProcessedByAdminId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashWithdrawals_PlayerId",
                table: "CashWithdrawals",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashWithdrawals_ProcessedByAdminId",
                table: "CashWithdrawals",
                column: "ProcessedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashWithdrawals");
        }
    }
}
