using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LudoClient.SolanaWallet;
using LudoClient.Constants;

namespace LudoClient.Services
{
    public class SolanaWalletService : ISolanaWalletService
    {
        private readonly LudoClient.SolanaWallet.WalletConnection _walletConnection = new LudoClient.SolanaWallet.WalletConnection();

        public bool IsConnected => _walletConnection.MainAddressBase58 != null;
        public string? ConnectedAddress => _walletConnection.MainAddressBase58;
        public string? AuthToken => _walletConnection.AuthToken;
        public double SolBalance => _walletConnection.SolBalance;
        public List<TokenBalance> TokenBalances => _walletConnection.TokenBalances;

        public event Action? RemoteClosed
        {
            add => _walletConnection.RemoteClosed += value;
            remove => _walletConnection.RemoteClosed -= value;
        }

        public async Task<bool> ConnectAsync()
        {
            return await _walletConnection.Connect();
        }

        public async Task<AuthorizationResult?> AuthorizeOrReauthorizeAsync()
        {
            return await _walletConnection.AuthorizeOrReauthorize();
        }

        public async Task RefreshBalancesAsync()
        {
            await _walletConnection.RefreshBalances();
        }

        public async Task<string> SendTokenAsync(string recipient, ulong amount, string mint, int decimals)
        {
            return await _walletConnection.SendToken(recipient, amount, mint, decimals);
        }

        public async Task<string> SendSolAsync(string recipient, ulong lamports)
        {
            return await _walletConnection.SendSol(recipient, lamports);
        }

        public async Task DisconnectAsync()
        {
            await _walletConnection.DisconnectAsync();
        }
    }
}
