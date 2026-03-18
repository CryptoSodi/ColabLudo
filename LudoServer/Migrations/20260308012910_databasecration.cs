using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoServer.Migrations
{
    /// <inheritdoc />
    public partial class databasecration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SenderPicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Index);
                });

            migrationBuilder.CreateTable(
                name: "MultiPlayers",
                columns: table => new
                {
                    MultiPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomCode = table.Column<int>(type: "int", nullable: true),
                    P1 = table.Column<int>(type: "int", nullable: true),
                    P2 = table.Column<int>(type: "int", nullable: true),
                    P3 = table.Column<int>(type: "int", nullable: true),
                    P4 = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiPlayers", x => x.MultiPlayerId);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoogleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    GamesWon = table.Column<int>(type: "int", nullable: false),
                    GamesLost = table.Column<int>(type: "int", nullable: false),
                    BestWin = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TotalLost = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TotalWin = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerWalletKey",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EncryptedPrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMaster = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerWalletKey", x => new { x.PlayerId, x.PublicKey });
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    TournamentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Winner1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Winner2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Winner3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Prize1 = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Prize2 = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Prize3 = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TournamentState = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.TournamentId);
                });

            migrationBuilder.CreateTable(
                name: "DailyBonus",
                columns: table => new
                {
                    DailyBonusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Day1 = table.Column<bool>(type: "bit", nullable: false),
                    Day2 = table.Column<bool>(type: "bit", nullable: false),
                    Day3 = table.Column<bool>(type: "bit", nullable: false),
                    Day4 = table.Column<bool>(type: "bit", nullable: false),
                    Day5 = table.Column<bool>(type: "bit", nullable: false),
                    Day6 = table.Column<bool>(type: "bit", nullable: false),
                    Day7 = table.Column<bool>(type: "bit", nullable: false),
                    DayCounter = table.Column<int>(type: "int", nullable: false),
                    LastResetDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyBonus", x => x.DailyBonusId);
                    table.ForeignKey(
                        name: "FK_DailyBonus_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FriendsRequests",
                columns: table => new
                {
                    FriendRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendsRequests", x => x.FriendRequestId);
                    table.ForeignKey(
                        name: "FK_FriendsRequests_Players_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FriendsRequests_Players_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerWallet",
                columns: table => new
                {
                    WalletId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WalletAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    IsWithdrawalLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerWallet", x => x.WalletId);
                    table.ForeignKey(
                        name: "FK_PlayerWallet_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    GameType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MultiPlayerId = table.Column<int>(type: "int", nullable: true),
                    TournamentId = table.Column<int>(type: "int", nullable: true),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    IsPractice = table.Column<bool>(type: "bit", nullable: false),
                    BetAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Winner1 = table.Column<int>(type: "int", nullable: true),
                    Winner2 = table.Column<int>(type: "int", nullable: true),
                    Owner = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recording = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_Games_MultiPlayers_MultiPlayerId",
                        column: x => x.MultiPlayerId,
                        principalTable: "MultiPlayers",
                        principalColumn: "MultiPlayerId");
                    table.ForeignKey(
                        name: "FK_Games_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId");
                });

            migrationBuilder.CreateTable(
                name: "TournamentChallengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: true),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentChallengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentChallengers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentChallengers_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId");
                });

            migrationBuilder.CreateTable(
                name: "WalletTransaction",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsOnChain = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoomCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    txId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    PlayerWalletWalletId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransaction", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_WalletTransaction_PlayerWallet_PlayerWalletWalletId",
                        column: x => x.PlayerWalletWalletId,
                        principalTable: "PlayerWallet",
                        principalColumn: "WalletId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyBonus_PlayerId",
                table: "DailyBonus",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendsRequests_ReceiverId",
                table: "FriendsRequests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendsRequests_SenderId",
                table: "FriendsRequests",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_MultiPlayerId",
                table: "Games",
                column: "MultiPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_TournamentId",
                table: "Games",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerWallet_PlayerId",
                table: "PlayerWallet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentChallengers_PlayerId",
                table: "TournamentChallengers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentChallengers_TournamentId",
                table: "TournamentChallengers",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_OperationId",
                table: "WalletTransaction",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_PlayerId_CreatedDate",
                table: "WalletTransaction",
                columns: new[] { "PlayerId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_PlayerWalletWalletId",
                table: "WalletTransaction",
                column: "PlayerWalletWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_Status",
                table: "WalletTransaction",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "DailyBonus");

            migrationBuilder.DropTable(
                name: "FriendsRequests");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "PlayerWalletKey");

            migrationBuilder.DropTable(
                name: "TournamentChallengers");

            migrationBuilder.DropTable(
                name: "WalletTransaction");

            migrationBuilder.DropTable(
                name: "MultiPlayers");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "PlayerWallet");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
