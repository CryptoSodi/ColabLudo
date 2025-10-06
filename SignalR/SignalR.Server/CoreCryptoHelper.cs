using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using System.Numerics;
using System.Text.Json;

namespace SignalR.Server
{

    public class CoreCryptoHelper
    {
        private readonly string _rpcUrl;
        private readonly string _storageFile;
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IDataProtector _protector;
        private readonly Dictionary<int, Wallet> _wallets;
        private readonly int _masterUserId;
        private readonly Web3 _web3;

        private const decimal WeiPerCore = 1_000_000_000_000_000_000m; // 1 CORE = 1e18 wei

        string tokenContract = "0x2829090EF104824a1c33882998227cB6C5423688";
        string nftContract = "0xa53a4B971a1f2486F783F674FECD8Bb8d441CAB2";
        public CoreCryptoHelper(IDbContextFactory<LudoDbContext> contextFactory, IHostEnvironment env, IDataProtectionProvider dataProtectionProvider,
            int masterUserId, string rpcUrl, string relativeStoragePath, string protectorKey)
        {
            _rpcUrl = rpcUrl;
            _contextFactory = contextFactory;
            _protector = dataProtectionProvider.CreateProtector(protectorKey);
            _masterUserId = masterUserId;
            _web3 = new Web3(rpcUrl);

            var dataFolder = Path.Combine(env.ContentRootPath, Path.GetDirectoryName(relativeStoragePath) ?? string.Empty);
            Directory.CreateDirectory(dataFolder);
            _storageFile = Path.Combine(env.ContentRootPath, relativeStoragePath);

            if (File.Exists(_storageFile))
                _wallets = JsonSerializer.Deserialize<Dictionary<int, Wallet>>(File.ReadAllText(_storageFile)) ?? new();
            else
                _wallets = new Dictionary<int, Wallet>();

            if (!_wallets.ContainsKey(_masterUserId))
            {
                using var ctx = _contextFactory.CreateDbContext();
                bool playerExists = ctx.Players.Any(p => p.PlayerId == _masterUserId);

                if (!playerExists)
                {
                    ctx.Players.Add(new Player
                    {
                        Name = "AdminCore",
                        Email = "core@ludonft.com",
                        City = "Global",
                        CountryCode = "+1"
                    });
                    ctx.SaveChanges();
                }
                Console.WriteLine("Master : " + GetOrCreateAccount(_masterUserId, true, true).GetAwaiter().GetResult());
            }
            else
            {
                Console.WriteLine("Master : " + GetOrCreateAccount(_masterUserId, true, true).GetAwaiter().GetResult());
                Console.WriteLine("Master Balance : " + GetOnChainBalanceAsync(GetOrCreateAccount(_masterUserId).GetAwaiter().GetResult()).GetAwaiter().GetResult());
                Console.WriteLine("Master Token Balance : " + GetTokenBalanceAsync(GetOrCreateAccount(_masterUserId).GetAwaiter().GetResult()).GetAwaiter().GetResult());
              //  Console.WriteLine("Master MINT Balance : " + MintNFT(_masterUserId, 1));
                //    Console.WriteLine("Master Token Transfer : " + SendTokenAsync(_masterUserId, "0x5db23ACd2F61f83668057eE288C2b81DDA28c983", 10).GetAwaiter().GetResult());
                //    Console.WriteLine("Master Token Balance : " + GetTokenBalanceAsync(GetOrCreateAccount(_masterUserId).GetAwaiter().GetResult()).GetAwaiter().GetResult());
                //
            }
        }
        private void PersistWallets()
        {
            var json = JsonSerializer.Serialize(_wallets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFile, json);
        }

        public async Task<string> MintNFT(int playerId, uint amount)
        {
          string tx1 = await ApproveAsync(playerId);//0x552f29eb89710b02f25d5006cc853e6566d5c679f701ba0723bb80070d97866b
            Console.WriteLine($"APPROVE TX : {tx1}");
            if (!_wallets.TryGetValue(playerId, out var wallet))
                throw new InvalidOperationException("Wallet not found");

            var rawPriv = _protector.Unprotect(wallet.EncryptedPrivateKey);
            var account = new Nethereum.Web3.Accounts.Account(rawPriv);
            var web3 = new Web3(account, _rpcUrl);

            var handler = web3.Eth.GetContractHandler(nftContract);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();

            var mint = new MintFunction
            {
                MintAmount = amount,
                GasPrice = gasPrice
            };
            string tx = await handler.SendRequestAsync(mint);//0x61fc49a0f2958be4215701d62263d79657cb1accf38579ecd13af04f7a71f0cb
            Console.WriteLine($"MINT TX : {tx}");
            return tx;
        }
        public async Task<string> ApproveAsync(int playerId, int decimals = 18)
        {
            if (!_wallets.TryGetValue(playerId, out var wallet))
                throw new InvalidOperationException("Wallet not found");

            var rawPriv = _protector.Unprotect(wallet.EncryptedPrivateKey);
            var account = new Nethereum.Web3.Accounts.Account(rawPriv);
            var web3 = new Web3(account, _rpcUrl);

            var contractHandler = web3.Eth.GetContractHandler(tokenContract);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            // Get the player's full token balance
            var balance = await GetTokenBalanceAsync(wallet.PublicKey, decimals);

            var approve = new ApproveFunction
            {
                Spender = nftContract,
                Value = Web3.Convert.ToWei(balance, decimals),
                GasPrice = gasPrice
            };

            var txHash = await contractHandler.SendRequestAsync(approve);
            return txHash;
        }
        public async Task<decimal> GetTokenBalanceAsync(string playerAddress, int decimals = 18)
        {
            var contractHandler = _web3.Eth.GetContractHandler(tokenContract);

            var balanceOf = new BalanceOfFunction { Owner = playerAddress };
            var balance = await contractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOf);

            return (decimal)balance / (decimal)BigInteger.Pow(10, decimals);
        }
        public async Task<string> SendTokenAsync(int playerId, string toAddress, decimal amount, int decimals = 18)
        {
            if (!_wallets.TryGetValue(playerId, out var wallet))
                throw new InvalidOperationException("Wallet not found");

            var rawPriv = _protector.Unprotect(wallet.EncryptedPrivateKey);
            var account = new Nethereum.Web3.Accounts.Account(rawPriv);
            var web3 = new Web3(account, _rpcUrl);

            // Check CORE balance
            var coreBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            var coreBalanceInEth = Web3.Convert.FromWei(coreBalance);

            // Estimate typical ERC20 gas cost (~0.001 CORE, adjust as needed)
            if (coreBalanceInEth < 0.001m)
            {
                await TopUpGasAsync(account.Address, 0.001m);
            }

            var contractHandler = web3.Eth.GetContractHandler(tokenContract);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            var transfer = new TransferFunction
            {
                To = toAddress,
                TokenAmount = Web3.Convert.ToWei(amount, decimals),
                GasPrice = gasPrice
            };

            var txHash = await contractHandler.SendRequestAsync(transfer);
            Console.WriteLine($"Txs {txHash}");
            return txHash;
        }      
        public async Task<string> TopUpGasAsync(string toAddress, decimal amountInCore)
        {
            if (!_wallets.TryGetValue(_masterUserId, out var masterWallet))
                throw new InvalidOperationException("Master wallet not found");

            var rawPriv = _protector.Unprotect(masterWallet.EncryptedPrivateKey);
            var account = new Nethereum.Web3.Accounts.Account(rawPriv);
            var web3 = new Web3(account, _rpcUrl);

            var txn = await web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, amountInCore);

            return txn.TransactionHash;
        }
        public async Task<string> GetOrCreateAccount(int playerId, bool isMaster = false, bool save = true)
        {
            if (_wallets.TryGetValue(playerId, out var w) && !string.IsNullOrEmpty(w.PublicKey))
                return w.PublicKey;

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKeyHex = ecKey.GetPrivateKeyAsBytes().ToHex();
            var address = ecKey.GetPublicAddress();

            var cipherPriv = _protector.Protect(privateKeyHex);

            _wallets[playerId] = new Wallet
            {
                PlayerId = playerId,
                EncryptedPrivateKey = cipherPriv,
                PublicKey = address,
                IsMaster = isMaster
            };

            await EnsurePlayerWalletExists(playerId, address);
            if (save) PersistWallets();

            return address;
        }
        public async Task<PlayerWallet?> EnsurePlayerWalletExists(int playerId, string address = "none")
        {
            using var ctx = _contextFactory.CreateDbContext();
            var exists = ctx.PlayerWallet.Any(p => p.PlayerId == playerId);
            if (!exists)
            {
                ctx.PlayerWallet.Add(new PlayerWallet
                {
                    PlayerId = playerId,
                    AddressType = "CORE",
                    WalletAddress = address == "none" ? await GetOrCreateAccount(playerId) : address,
                    AvailableBalance = 0m
                });
                await ctx.SaveChangesAsync();
                await SendOnChainAsync(playerId, "Signup Bonus", 10000);
            }

            var pw =  await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            pw.AvailableBalance = await GetTokenBalanceAsync(await GetOrCreateAccount(playerId));
            ctx.Update(pw);
            ctx.SaveChanges();

            return pw;
        }
        public async Task<decimal> GetOnChainBalanceAsync(string address)
        {
           return await GetTokenBalanceAsync(address);
            //var balanceWei = await _web3.Eth.GetBalance.SendRequestAsync(address);
            //return Web3.Convert.FromWei(balanceWei.Value);
        }
        public async Task<string> SendFromMasterAsync(string toAddress, decimal amountInCore)
        {
            if (!_wallets.TryGetValue(_masterUserId, out var master))
                throw new InvalidOperationException("Master wallet not found");

            var rawPriv = _protector.Unprotect(master.EncryptedPrivateKey);
            var account = new Nethereum.Web3.Accounts.Account(rawPriv);
            var web3 = new Web3(account, _rpcUrl);

            var txReceipt = await web3.TransactionManager.SendTransactionAsync(
                account.Address,
                toAddress,
                new Nethereum.Hex.HexTypes.HexBigInteger(Web3.Convert.ToWei(amountInCore))
            );

            return txReceipt;
        }
        public async Task<string> SendOnChainAsync(int playerId, string toAddress, decimal amountInCore)
        {
            string tx = "";
            switch (toAddress)
            {
                case "Game Refund":
                case "Game Won":
                case "Daily Bonus":
                case "Signup Bonus":
                var sub = await GetOrCreateAccount(playerId);
                   tx = await SendTokenAsync(_masterUserId, sub, amountInCore);
                    Console.WriteLine($"TX {toAddress} {tx}");
                return tx;
            }
            tx = await SendTokenAsync(playerId, toAddress, amountInCore);
            Console.WriteLine($"TX {toAddress} {tx}");
            return tx;
        }
        public async Task<bool> DeductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount)
        {
            if (betAmount <= 0)
                return false;
            var sub = await EnsurePlayerWalletExists(playerId);
            var balance = await GetTokenBalanceAsync(sub.WalletAddress);
            if (balance < betAmount)
                return false; // Not enough balance

            bool debited = false;

            using var ctx = _contextFactory.CreateDbContext();

            if (isTournamentGame && tournamentId.HasValue)
            {
                var existingChallenger = await ctx.TournamentChallengers
                    .FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == playerId);

                if (existingChallenger == null)
                {
                    ctx.TournamentChallengers.Add(new TournamentChallenger
                    {
                        PlayerId = playerId,
                        TournamentId = tournamentId.Value,
                        Status = "JOINED",
                        RetryCount = 0
                    });

                    await ctx.SaveChangesAsync();

                    //debited = await OffChainTransaction(playerId, -betAmount, "Tournament Fee", tournamentId.ToString(), false, roomCode);
                    await SendOnChainAsync(playerId, await GetOrCreateAccount(_masterUserId), betAmount);
                    debited = true;
                }
                else if (existingChallenger.Status == "FAILED")
                {
                    existingChallenger.RetryCount++;
                    existingChallenger.Status = "JOINED";

                    await ctx.SaveChangesAsync();
                    await SendOnChainAsync(playerId, await GetOrCreateAccount(_masterUserId), betAmount);

                    //                    debited = await OffChainTransaction(playerId, -betAmount, "Tournament Fee Retry", tournamentId.ToString(), false, roomCode);
                    debited = true;
                }
                else
                {
                    // Already joined and not failed
                    debited = true;
                }
            }
            else
            {
                await SendOnChainAsync(playerId, await GetOrCreateAccount(_masterUserId), betAmount);
                debited = true;
//                debited = await OffChainTransaction(playerId, -betAmount, "Game Fee", "", false, roomCode);
            }

            return debited;
        }
        public async Task<string> SendCoreToExternalWallet(int playerId, string destination, decimal amountInCore)
        {
            try
            {
                // 0) Check total balance (on-chain + off-chain)
                var totalBalance = await GetTokenBalanceAsync(await GetOrCreateAccount(playerId));
                if (totalBalance < amountInCore)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient total balance for {playerId}. Have {totalBalance} CORE, tried {amountInCore} CORE.");
                    return "INSUFFICIENT_FUNDS";
                }
                var txHash = await SendOnChainAsync(playerId, destination.Trim(), amountInCore);
                Console.WriteLine($"Withdrawal of {amountInCore} CORE for {playerId} sent from master. Tx: {txHash}");
                return txHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during withdrawal for {playerId}: {ex.Message}");
                return "ERROR";
            }
        }
        public async Task<List<BigInteger>> GetWalletOfOwnerAsync(int playerId)
        {
            var web3 = new Web3(_rpcUrl);

            var handler = web3.Eth.GetContractHandler(nftContract);

            var sub = await GetOrCreateAccount(playerId);
            var query = new WalletOfOwnerFunction
            {
                Owner = sub
            };

            // Call (no gas needed since it's a view function)
            var tokenIds = await handler.QueryAsync<WalletOfOwnerFunction, List<BigInteger>>(query);
            return tokenIds;
        }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunction : FunctionMessage
        {
            [Parameter("address", "owner", 1)]
            public string Owner { get; set; }
        }
        [Function("transfer", "bool")]
        public class TransferFunction : FunctionMessage
        {
            [Parameter("address", "to", 1)]
            public string To { get; set; }

            [Parameter("uint256", "value", 2)]
            public BigInteger TokenAmount { get; set; }
        }
        [Function("approve", "bool")]
        public class ApproveFunction : FunctionMessage
        {
            [Parameter("address", "spender", 1)]
            public string Spender { get; set; }

            [Parameter("uint256", "value", 2)]
            public BigInteger Value { get; set; }
        }
        [Function("mint")]
        public class MintFunction : FunctionMessage
        {
            [Parameter("uint32", "_mintAmount", 1)]
            public uint MintAmount { get; set; }
        }
        [Function("walletOfOwner", "uint256[]")]
        public class WalletOfOwnerFunction : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public string Owner { get; set; }
        }
        public class Wallet
        {
            /// <summary>User or manager identifier.</summary>
            public int PlayerId { get; set; }

            /// <summary>Encrypted Base58 private key for signing transactions.</summary>
            public string EncryptedPrivateKey { get; set; }

            /// <summary>Base58-encoded public key for deposit/withdrawal.</summary>
            public string PublicKey { get; set; }

            /// <summary>Flag indicating if this wallet is the master (hot) wallet.</summary>
            public bool IsMaster { get; set; }
        }
    }
}