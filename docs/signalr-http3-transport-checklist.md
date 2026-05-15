# LudoClient Transport Checklist (SignalR Primary, HTTP/3 Fallback)

This document maps each `SharedCode/Network/Client.cs` network method to:
- SignalR hub method name
- Hub base class where method is implemented
- HTTP endpoint fallback
- Controller action class

Scope notes:
- `GoogleAuthentication` stays HTTP-only by design.
- Connection lifecycle helpers (`ConnectAsync`, `DisconnectAsync`, `CreateApiRequest`) are not business endpoint ports.

## Hub Class Chain (for quick lookup)

- `LudoHub` -> `LudoHubPaymentBase`
- `LudoHubPaymentBase` -> `LudoHubWalletBase`
- `LudoHubWalletBase` -> `LudoHubTournamentBase`
- `LudoHubTournamentBase` -> `LudoHubSocialBase`
- `LudoHubSocialBase` -> `LudoHubProfileBase`
- `LudoHubProfileBase` -> `LudoHubNftBase`
- `LudoHubNftBase` -> `LudoHubDailyBonusBase`
- `LudoHubDailyBonusBase` -> `LudoHubChatBase`
- `LudoHubChatBase` -> `LudoHubGameplayBase`
- `LudoHubGameplayBase` -> `LudoHubClockBase`

## Checklist

| Client.cs Method | SignalR Hub Method | Hub Base Class | HTTP Fallback Endpoint | Controller Action |
|---|---|---|---|---|
| `CreateJoinLobbyAsync` | `JoinLobby` | `LudoHubGameplayBase` | `POST api/gameplay/lobbies/join` | `GameplayController.JoinLobby` |
| `SendCommandAsync` | `Send` | `LudoHubGameplayBase` | `POST api/gameplay/commands/send` | `GameplayController.SendCommand` |
| `ReadyAsync` | `Ready` | `LudoHubGameplayBase` | `POST api/gameplay/lobbies/ready` | `GameplayController.Ready` |
| `LeaveCloseLobby` | `LeaveLobby` | `LudoHubGameplayBase` | `POST api/gameplay/lobbies/leave` | `GameplayController.LeaveLobby` |
| `PullCommands` | `PullCommands` | `LudoHubGameplayBase` | `GET api/gameplay/commands/pull` | `GameplayController.PullCommands` |
| `GetLobbyAsync` | `GetLobbyState` | `LudoHubGameplayBase` | `GET api/gameplay/lobbies/state` | `GameplayController.GetLobbyState` |
| `GetActivePublicGamesAsync` | `GetActivePublicGames` | `LudoHubGameplayBase` | `GET api/gameplay/games/active` | `GameplayController.GetActivePublicGames` |
| `SendChatMessageAsync` | `SendChat` | `LudoHubChatBase` | `POST api/gameplay/chat/send` | `GameplayController.SendChat` |
| `PullChatUpdatesAsync` (internal) | `PullChat` | `LudoHubChatBase` | `GET api/gameplay/chat/pull` | `GameplayController.PullChat` |
| `GetAllTournaments` | `GetTournaments` | `LudoHubTournamentBase` | `GET api/tournaments` | `TournamentController.GetAll` |
| `JoinTournament` | `JoinTournament` | `LudoHubTournamentBase` | `POST api/tournaments/{id}/join` | `TournamentController.Join` |
| `GetResultsTournament` | `GetTournamentResults` | `LudoHubTournamentBase` | `GET api/tournaments/{id}/results` | `TournamentController.GetResults` |
| `GetDailyBonus<T>` | `GetDailyBonus` | `LudoHubDailyBonusBase` | `GET api/daily-bonus` | `DailyBonusController.GetDailyBonus` |
| `ClaimTodayBonus<T>` | `ClaimTodayBonus` | `LudoHubDailyBonusBase` | `POST api/daily-bonus/claim` | `DailyBonusController.ClaimTodayBonus` |
| `GetProfile<T>` | `GetProfile` | `LudoHubProfileBase` | `GET api/profile` | `ProfileController.GetProfile` |
| `GetWallet<T>` | `GetWallet` | `LudoHubProfileBase` | `GET api/wallet` | `ProfileController.GetWallet` |
| `RefreshSessionFromApi` | `SyncSession` | `LudoHubProfileBase` | `GET api/session/sync` | `ProfileController.SyncSession` |
| `GetFriends` | `GetFriends` | `LudoHubSocialBase` | `GET api/friends` | `SocialController.GetFriends` |
| `SendFriendRequest` | `SendFriendRequest` | `LudoHubSocialBase` | `POST api/friends/request` | `SocialController.SendFriendRequest` |
| `GetPlayerById` | `GetPlayerById` | `LudoHubSocialBase` | `GET api/players/{playerId}/card` | `SocialController.GetPlayerById` |
| `GetLeaderboard` | `GetLeaderboard` | `LudoHubSocialBase` | `GET api/leaderboard` | `SocialController.GetLeaderboard` |
| `GetTournamentLeaderboard` | `GetTournamentLeaderboard` | `LudoHubSocialBase` | `GET api/leaderboard/tournament/{type}` | `SocialController.GetTournamentLeaderboard` |
| `MintNFT` | `MintNFT` | `LudoHubNftBase` | `POST api/nfts/mint` | `NftController.Mint` |
| `InitiateWithdrawal` | `InitiateWithdrawal` | `LudoHubPaymentBase` | `POST api/payments/withdrawals/initiate` | `PaymentController.InitiateWithdrawal` |
| `GetWalletBalance` | `GetWalletBalance` | `LudoHubPaymentBase` | `GET api/payments/wallet-balance` | `PaymentController.GetWalletBalance` |
| `GetSwapBalances` | `GetSwapBalances` | `LudoHubPaymentBase` | `GET api/payments/swap-balances` | `PaymentController.GetSwapBalances` |
| `BroadcastTransaction` | `BroadcastTransaction` | `LudoHubPaymentBase` | `POST api/payments/transactions/broadcast` | `PaymentController.BroadcastTransaction` |
| `ExecutePreparedSwap` | `ExecutePreparedSwap` | `LudoHubPaymentBase` | `POST api/payments/swap/execute` | `PaymentController.ExecutePreparedSwap` |
| `ConfirmSolanaTransaction` | `ConfirmSolanaTransaction` | `LudoHubPaymentBase` | `POST api/payments/transactions/confirm` | `PaymentController.ConfirmSolanaTransaction` |
| `PrepareAssetSwap` | `PrepareAssetSwap` | `LudoHubPaymentBase` | `POST api/payments/swap/prepare` | `PaymentController.PrepareAssetSwap` |
| `PrepareLudcDeposit` | `PrepareLudcDeposit` | `LudoHubPaymentBase` | `POST api/payments/ludc-deposit/prepare` | `PaymentController.PrepareLudcDeposit` |
| `SubmitManualDeposit` | `SubmitManualDeposit` | `LudoHubPaymentBase` | `POST api/payments/manual-deposits` | `PaymentController.SubmitManualDeposit` |
| `SubmitManualWithdrawal` | `SubmitManualWithdrawal` | `LudoHubPaymentBase` | `POST api/payments/manual-withdrawals` | `PaymentController.SubmitManualWithdrawal` |
| `GetWalletBonusHistory` | `GetWalletBonuses` | `LudoHubWalletBase` | `GET api/wallet-hub/bonuses` | `WalletHubController.GetBonuses` |
| `GetWalletDepositHistory` | `GetWalletDeposits` | `LudoHubWalletBase` | `GET api/wallet-hub/deposits` | `WalletHubController.GetDeposits` |
| `GetWalletWithdrawalHistory` | `GetWalletWithdrawals` | `LudoHubWalletBase` | `GET api/wallet-hub/withdrawals` | `WalletHubController.GetWithdrawals` |
| `GetWalletGameHistory` | `GetWalletGames` | `LudoHubWalletBase` | `GET api/wallet-hub/games` | `WalletHubController.GetGames` |

## HTTP-Only / Non-Ported by Design

| Client.cs Method | Reason | HTTP Endpoint | Controller Action |
|---|---|---|---|
| `GoogleAuthentication` | Explicit exception: auth/login stays HTTP | `POST api/auth/google` | `AuthController.GoogleLogin` |
| `ConnectAsync` | Connection lifecycle bootstrap | N/A | N/A |
| `DisconnectAsync` | Connection lifecycle | N/A | N/A |
| `CreateApiRequest` | HTTP request helper | N/A | N/A |

