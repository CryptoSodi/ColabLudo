using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace SignalR.Server
{
    public class CryptoHelper
    {
        public SolCryptoHelper solCryptoHelper;
        private IDbContextFactory<LudoDbContext> _contextFactory;        
        string protectorKey;

        public CryptoHelper(IDbContextFactory<LudoDbContext> contextFactory, IHostEnvironment env, IDataProtectionProvider dataProtectionProvider,
            int masterUserId, string network = "MainNetBeta", string protectorKey = "CryptoHelper.WalletProtector")
        {
            this.protectorKey = protectorKey;
            _contextFactory = contextFactory;
            solCryptoHelper = new SolCryptoHelper(contextFactory, env, dataProtectionProvider, masterUserId, network, protectorKey);
        }
        public async Task<bool> OffChainTransaction(int playerId, decimal amount, string description, string txId = "", bool isOnChain = false, string roomCode = "")
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync(); // ✅ FIX

            var wallet = await ctx.PlayerWallet.FirstAsync(p => p.PlayerId == playerId);

            // Block during withdrawal
            if (wallet.IsWithdrawalLocked)
                return false;

            solCryptoHelper.ApplyOffChainLedger(ctx, wallet, amount, description, roomCode, isOnChain, txId);

            await ctx.SaveChangesAsync();
            await tx.CommitAsync(); // ✅ Now this works
            return true;
        }

        public PlayerWallet? EnsurePlayerWalletExists(int playerId)
        {
            return solCryptoHelper.EnsureWalletAsync(playerId).GetAwaiter().GetResult();
        }
        public bool deductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount)
        {
            return solCryptoHelper.DeductGameFee(playerId, tournamentId, roomCode, isTournamentGame, betAmount).GetAwaiter().GetResult();
        }
        internal string Withdraw(Player player, string destination, decimal amountInSol)
        {
            var operationId = Guid.NewGuid();            
            return solCryptoHelper.Withdraw(player, destination, amountInSol, operationId).GetAwaiter().GetResult();
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

        public async Task<String> MintNFT(int playerid, int amount)
        {
            return await solCryptoHelper.MintNFT(playerid, amount);
        }
    }
}