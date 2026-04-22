using System;
using System.Collections.Generic;
using Solnet.Programs;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace LudoClient.SolanaWallet
{
    public static class SolanaTokenService
    {
        public static readonly PublicKey TOKEN_2022_PROGRAM_ID = new PublicKey("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");
        public static readonly PublicKey STANDARD_TOKEN_PROGRAM_ID = new PublicKey("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");
        public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");

        // LUDC Mints
        public const string LUDC_MINT_MAINNET = "JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai";
        public const string LUDC_MINT_DEVNET = "8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW";

        // USDC Mints
        public const string USDC_MINT_MAINNET = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
        public const string USDC_MINT_DEVNET = "4zMMC9srtvS2PPSatZ9LUnTNDK8B6kG58wEYuVfCXU1n";

        public static TransactionInstruction CreateTransferCheckedInstruction(
            PublicKey source,
            PublicKey mint,
            PublicKey destination,
            PublicKey owner,
            ulong amount,
            byte decimals)
        {
            var data = new List<byte> { 12 }; // TransferChecked opcode
            var amountBytes = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(amountBytes, amount);
            data.AddRange(amountBytes);
            data.Add(decimals);

            return new TransactionInstruction
            {
                ProgramId = TOKEN_2022_PROGRAM_ID,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(source, false),
                    AccountMeta.ReadOnly(mint, false),
                    AccountMeta.Writable(destination, false),
                    AccountMeta.ReadOnly(owner, true)
                },
                Data = data.ToArray()
            };
        }

        public static TransactionInstruction CreateAssociatedTokenAccountInstruction(
            PublicKey payer,
            PublicKey owner,
            PublicKey mint)
        {
            var ata = FindAssociatedTokenAddress(owner, mint);
            
            return new TransactionInstruction
            {
                ProgramId = ASSOCIATED_TOKEN_PROGRAM_ID,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(payer, true),
                    AccountMeta.Writable(ata, false),
                    AccountMeta.ReadOnly(owner, false),
                    AccountMeta.ReadOnly(mint, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(TOKEN_2022_PROGRAM_ID, false)
                },
                Data = Array.Empty<byte>()
            };
        }

        public static PublicKey FindAssociatedTokenAddress(PublicKey owner, PublicKey mint)
        {
            return FindAssociatedTokenAddress(owner, mint, TOKEN_2022_PROGRAM_ID);
        }

        public static PublicKey FindAssociatedTokenAddress(PublicKey owner, PublicKey mint, PublicKey tokenProgramId)
        {
            if (!PublicKey.TryFindProgramAddress(
                new[] { owner.KeyBytes, tokenProgramId.KeyBytes, mint.KeyBytes },
                ASSOCIATED_TOKEN_PROGRAM_ID,
                out var ata,
                out _))
            {
                throw new Exception("Could not find associated token address");
            }
            return ata;
        }
    }
}
