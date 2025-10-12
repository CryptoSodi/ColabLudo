using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SignalR.Server
{
    public class CryptoHelper
    {
        public SolCryptoHelper solCryptoHelper;
        string protectorKey;
        public CryptoHelper(IDbContextFactory<LudoDbContext> contextFactory, IHostEnvironment env, IDataProtectionProvider dataProtectionProvider,
            int masterUserId, string network = "MainNetBeta", string relativeStoragePath = "wallets.json", string protectorKey = "CryptoHelper.WalletProtector")
        {
            this.protectorKey = protectorKey;
            solCryptoHelper = new SolCryptoHelper(contextFactory, env, dataProtectionProvider, masterUserId, network, relativeStoragePath, protectorKey);
        }
        public bool OffChainTransaction(int playerId, decimal solAmount, String description, String txId = "", bool IsOnChain = false, String RoomCode = "")
        {
            return solCryptoHelper.OffChainTransaction(playerId, solAmount, description, txId, IsOnChain, RoomCode).GetAwaiter().GetResult();
        }
        public PlayerWallet? EnsurePlayerWalletExists(int playerId)
        {
            return solCryptoHelper.EnsurePlayerWalletExists(playerId, "none").GetAwaiter().GetResult();
        }
        public bool deductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount)
        {
            return solCryptoHelper.DeductGameFee(playerId, tournamentId, roomCode, isTournamentGame, betAmount).GetAwaiter().GetResult();
        }
        internal string SendSolToExternalWallet(Player player, string destination, decimal amountInSol)
        {
            return solCryptoHelper.SendSolToExternalWallet(player, destination, amountInSol).GetAwaiter().GetResult();
        }
        internal void SweepAllSubAccounts()
        {
            solCryptoHelper.SweepAllSubAccountsAsync().GetAwaiter().GetResult();
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