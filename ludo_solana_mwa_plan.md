# Implementation Plan: Solana Mobile Wallet Adapter (MWA) Integration

## 1. Goal
Integrate the Solana Mobile Wallet Adapter (MWA) into the .NET MAUI Android application to enable real wallet connections and secure transaction signing for deposits and withdrawals.

## 2. Technical Stack
- **SDK:** `com.solanamobile:mobile-wallet-adapter-clientlib:2.0.7`
- **Solana Logic:** `Solnet.Rpc`, `Solnet.Wallet`, `Solnet.Programs`
- **SignalR:** `DashboardHub` (Server) and `DashboardClient` (Client)

## 3. Detailed Steps

### Step 1: Configuration & Dependencies
- **LudoClient.csproj**:
    - Add `<AndroidMavenLibrary>` entries for Solana MWA SDK and its core dependencies.
    - Add NuGet packages: `Solnet.Rpc`, `Solnet.Wallet`, `Solnet.Programs`.

### Step 2: SignalR Dashboard Client
- Create `DashboardClient.cs` in `SharedCode/Network` to connect specifically to the `DashboardHub`.
- Expose this client through `GlobalConstants.DashboardClient`.

### Step 3: Android MWA Bridge
- Implement `MwaService.cs` in `LudoClient/Platforms/Android/` using Java interop/bindings to:
    - Establish a session via `MobileWalletAdapterClient`.
    - Handle `Authorize` (to get the wallet address).
    - Handle `SignTransactions` (to sign built transactions).

### Step 4: Deposit Flow (AddCashDialogFragment)
- **Connect**: Use `MwaService.Authorize` to link Phantom/Solflare.
- **Sign Transfer**:
    1. Fetch deposit metadata from `DashboardClient.PrepareLudcDeposit`.
    2. Build a Solana Token-2022 transfer transaction using `Solnet`.
    3. Sign the transaction via `MwaService.SignTransactions`.
    4. Verify the transaction signature with `DashboardClient.ConfirmSolanaTransaction`.

### Step 5: Withdrawal Flow (WithdrawDialogFragment)
- **Connect**: Link the wallet.
- **Initiate**: Call `DashboardClient.InitiateWithdrawal` with the user's Phantom address and the desired amount.

### Step 6: UI/UX Refinement
- Update native Android layouts and tab state management to provide real-time connection and transaction feedback.

## 4. Risks & Considerations
- **Transitive Dependencies**: Manually resolving Maven dependencies in .NET MAUI requires care.
- **Intent Redirection**: Ensuring the wallet app correctly returns to the Ludo app after signing.
- **Solana Network**: Ensuring compatibility between mainnet/devnet clusters.

## 5. Approval
Please confirm if this plan aligns with your expectations.
