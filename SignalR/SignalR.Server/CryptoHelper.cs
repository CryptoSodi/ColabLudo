using System.Security.Cryptography.X509Certificates;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Programs;

namespace SignalR.Server
{
    public class CryptoHelper
    {
        private readonly IRpcClient _rpcClient;
        private readonly Account _platformAccount;
        private readonly PublicKey _usdcMint;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoHelper"/> class.
        /// </summary>
        /// <param name="rpcEndpoint">Solana RPC endpoint (e.g., https://api.devnet.solana.com).</param>
        /// <param name="platformKeypairBase64">Base64-encoded keypair for the platform fee-payer.</param>
        /// <param name="usdcMintAddress">The USDC token mint address on the target cluster.</param>
        public CryptoHelper(string rpcEndpoint, string platformKeypairBase64, string usdcMintAddress)
        {
            // Initialize the RPC client
            _rpcClient = ClientFactory.GetClient(rpcEndpoint);

            // Load the platform account (fee-payer)
            var secretKey = Convert.FromBase64String(platformKeypairBase64);
            _platformAccount = new Account(secretKey);

            // Set the USDC mint
            _usdcMint = new PublicKey(usdcMintAddress);
        }

        /// <summary>
        /// Derives the Associated Token Account (ATA) for a given user wallet.
        /// </summary>
        /// <param name="userWallet">The user's wallet public key.</param>
        /// <returns>The ATA public key.</returns>
        public PublicKey DeriveUsdcAta(PublicKey userWallet)
        {
            return AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(userWallet, _usdcMint);
        }

        /// <summary>
        /// Ensures the user's USDC ATA exists; if not, creates it on-chain.
        /// </summary>
        /// <param name="userWallet">The user's wallet public key.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureUsdcAtaExistsAsync(PublicKey userWallet)
        {
            var ata = DeriveUsdcAta(userWallet);

            // Check if ATA exists
            var accountInfo = await _rpcClient.GetAccountInfoAsync(ata.Key);
            if (accountInfo.Result.Value != null)
                return;

            // Fetch recent block hash
            var recent = await _rpcClient.GetLatestBlockHashAsync();
            var blockHash = recent.Result.Value.Blockhash;

            // Build transaction to create ATA
            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash)
                .SetFeePayer(_platformAccount.PublicKey)
                .AddInstruction(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        _platformAccount.PublicKey,
                        userWallet,
                        _usdcMint
                    )
                )
                .Build(_platformAccount);

            // Send and optionally confirm
            var sendResult = await _rpcClient.SendTransactionAsync(tx);
            if (!sendResult.WasSuccessful)
            {
                throw new Exception($"Failed to create ATA: {sendResult.Reason}");
            }

            // Optionally confirm transaction
            await _rpcClient.ConfirmTransactionAsync(sendResult.Result, Commitment.Confirmed);
        }
    }
}
