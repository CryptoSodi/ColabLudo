# Module Context: LudoSignalR (Real-Time Engine)

## Responsibilities
- **Game State Sync:** Broadcasting moves, dice rolls, and turn timers instantly.
- **Lobby Management:** Grouping players into real-time rooms based on `TournamentId` and game id.
- **Live Chat:** Facilitating high-speed messaging between players.

## Technical Constraints
- **Protocol:** SignalR / WebSockets.

## AI Instructions
- Optimize for high concurrency and minimal latency.
- Ensure robust connection handling (reconnect/disconnect logic).