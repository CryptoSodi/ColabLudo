using System.Collections.Generic;
using System.Threading.Tasks;
using LudoClient.SolanaWallet;

namespace LudoClient.Services
{
    public interface ISolanaWalletService
    {
        bool IsConnected { get; }
        string? ConnectedAddress { get; }
        string? AuthToken { get; }
        double SolBalance { get; }
        List<TokenBalance> TokenBalances { get; }

        Task<bool> ConnectAsync();
        Task<AuthorizationResult?> AuthorizeOrReauthorizeAsync();
        Task RefreshBalancesAsync();
        Task<string> SendTokenAsync(string recipient, ulong amount, string mint, int decimals);
        Task<string> SendSolAsync(string recipient, ulong lamports);
        Task DisconnectAsync();

        event Action? RemoteClosed;
    }
}
