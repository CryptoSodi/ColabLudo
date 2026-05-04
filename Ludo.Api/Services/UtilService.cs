using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using SignalR.Server.Payments;
using System.Security.Cryptography;
using System.Text;

namespace SignalR.Server.Services
{
    public class UtilService(IDbContextFactory<LudoDbContext> _contextFactory, CryptoHelper _crypto, string protectorKey = "CryptoHelper.WalletProtector")
    {
        public async Task<PlayerInfo> CastPlayerToInfoAsync(Player player)
        {
            var pw = await _crypto.EnsurePlayerWalletExists(player.PlayerId, CurrencyType.LUDC);

            return new PlayerInfo
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                Email = player.Email,
                PictureUrl = player.PictureUrl,
                PhoneNumber = player.PhoneNumber,
                Country = player.Country,
                City = player.City,
                CountryCallingCode = player.CountryCallingCode,
                IsOnline = player.IsOnline,
                AuthToken = player.AuthToken,
                GamesPlayed = player.GamesPlayed,
                GamesWon = player.GamesWon,
                GamesLost = player.GamesLost,
                BestWin = player.BestWin,
                TotalWin = player.TotalWin,
                TotalLost = player.TotalLost,
                Score = player.Score,
                Role = player.Role,
                Wallet = new SharedCode.Constants.PlayerWallet
                {
                    PlayerId = pw.PlayerId,
                    AddressType = pw.AddressType,
                    WalletAddress = pw.WalletAddress,
                    AvailableBalance = pw.AvailableBalance,
                    SignupBonus = 10,
                    Transactions = getTransactions(player.PlayerId)
                }
            };
        }
        private ICollection<SharedCode.Constants.WalletTransaction> getTransactions(int playerId)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                List<SharedCode.Constants.WalletTransaction> transactions = ctx.WalletTransaction.Where(p => p.PlayerId == playerId).Select(t => new SharedCode.Constants.WalletTransaction
                {
                    TransactionId = t.TransactionId,
                    txId = t.txId,
                    PlayerId = t.PlayerId,
                    CreatedDate = t.CreatedDate,
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    Type = (SharedCode.Constants.TransactionType)t.Type,
                    Description = t.Description,
                    IsOnChain = t.IsOnChain,
                    RoomCode = t.RoomCode
                }).ToList();
                return transactions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
            return new List<SharedCode.Constants.WalletTransaction>();
        }
        public async Task SetPlayerOnlineState(int playerId, bool isOnline, bool touchLastLogin = false)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = new Player { PlayerId = playerId, IsOnline = isOnline };
                ctx.Players.Attach(player);
                ctx.Entry(player).Property(p => p.IsOnline).IsModified = true;
                if (touchLastLogin)
                {
                    player.LastLogin = DateTime.UtcNow;
                    ctx.Entry(player).Property(p => p.LastLogin).IsModified = true;
                }
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
        }
        public async Task<Player> GetPlayerByID(int PlayerId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            Player sender = ctx.Players.Find(PlayerId);
            if (sender == null)
                throw new HubException("Player not recognized.");

            LudoServer.Models.PlayerWallet wal = await _crypto.EnsurePlayerWalletExists(PlayerId, CurrencyType.LUDC);
            if (wal == null)
                throw new HubException("Player Wallet not Found.");

            sender.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = sender.PlayerId,
                        AddressType = wal.AddressType,
                        WalletAddress = wal.WalletAddress,
                        AvailableBalance = wal.AvailableBalance
                    }
                };
            return sender;
        }
        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(protectorKey));
            aes.IV = new byte[16]; // 16 bytes IV for AES
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
        public string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(protectorKey));
            aes.IV = new byte[16]; // 16 bytes IV for AES
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
