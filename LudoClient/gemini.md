# Module Context: LudoClient (MAUI Android)

## Responsibilities
- Rendering the Ludo board and handling player input.
- Managing **Solana Mobile Wallet Adapter (MWA)** sessions (planned).
- Listening to `LudoSignalR` for game events.

## Technical Constraints
- Targets **Android** specifically.
- Uses C# for all logic.
- Must handle lifecycle events (App pause/resume) to maintain SignalR connectivity.

## AI Instructions
- Prioritize high-performance UI rendering for the game board.
- Ensure all wallet-related code follows Solana's best practices for mobile.