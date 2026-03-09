using LudoServer.Models;
using SignalR.Server.Payments;
namespace SignalR.Server.Interfaces
{
    public interface IPaymentProvider
    {
        CurrencyType Currency { get; }
        Task<string> WithdrawAsync(Player player,string destination,decimal amount,Guid operationId);
        Task<decimal> GetOnChainBalanceAsync(string walletAddress);
        Task<string> SweepAsync(int playerId, decimal amount);
        Task<PlayerWallet> EnsurePlayerWalletExists(int playerId, string addressType);
        Task<List<TokenDeposit>> GetRecentDeposits(string beforeSignature);
    }
}