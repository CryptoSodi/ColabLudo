# ColabLudo: Blockchain-Integrated Multiplayer Ludo

ColabLudo is a high-performance, real-time multiplayer Ludo gaming platform built on the .NET ecosystem and integrated with the Solana blockchain. It features a unique "Play-to-Earn" model where players can participate in tournaments using the **LUDC** (Ludo City) token and own in-game assets as NFTs.

## 🏗️ Architecture

The solution is organized as a monorepo with four primary layers:

### 1. **LudoClient (.NET MAUI)**
- **Platform:** Optimized for Android (with support for iOS/Windows).
- **Tech:** SimpleShell for navigation, Community Toolkit for UI, and SoundPool for low-latency audio.
- **Role:** Handles UI/UX, local game state synchronization, and real-time communication via SignalR.

### 2. **SignalR.Server (Real-Time Hub)**
- **Tech:** ASP.NET Core SignalR.
- **Role:** The authoritative source of truth for active games. It manages lobbies, enforces game rules via a server-side `Engine`, and handles real-time player interactions (moves, dice rolls, chat).
- **Payment Layer:** Orchestrates LUDC token transfers and tournament entry fee deductions.

### 3. **LudoServer (Backend API)**
- **Tech:** ASP.NET Core Web API, Entity Framework Core (SQL Server).
- **Role:** Manages user persistence, authentication (Google/OTP), player statistics, and leaderboard data.

### 4. **SharedCode (.NET Standard Library)**
- **Core Engine:** Contains the `Engine.cs` logic shared between the Client (for smooth UI animations) and the SignalR Server (for authoritative validation).
- **Models:** Shared DTOs and constants for cross-layer communication.

---

## 💎 Blockchain Integration (Solana)

ColabLudo utilizes the Solana blockchain for its transparency and high throughput:

- **Currency:** LUDC (Token-2022 Program).
  - **DevNet Mint:** `8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW`
  - **MainNet Mint:** `JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai`
- **Wallet Support:** Integrated with `Solnet` for backend transaction verification. Mobile Wallet Adapter (MWA) integration is currently in progress for the client.
- **Ledger System:** A hybrid model uses an off-chain ledger for instant game results, periodically reconciled with on-chain transactions to ensure security and speed.

---

## 🤖 AI & Training

The repository includes specialized projects for AI development:
- **AiEngine:** A NEAT (NeuroEvolution of Augmenting Topologies) based experiment for training Ludo agents.
- **AiController:** A trainer/controller for running and evaluating AI performance against the core game engine.

---

## 🚀 Getting Started

### Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the **.NET MAUI** workload.
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or Express) for the backend.

### Setup
1. **Clone the Repo**
   ```bash
   git clone https://github.com/CryptoSodi/ColabLudo.git
   ```
2. **Configure Connection Strings**
   Update `appsettings.json` in both `LudoServer` and `SignalR.Server` with your local SQL Server connection string.
3. **Run the Servers**
   - Start `LudoServer` to enable authentication and user services.
   - Start `SignalR.Server` to enable matchmaking and gameplay.
4. **Run the Client**
   - Open `LudoClient.sln` in Visual Studio.
   - Set `LudoClient` as the startup project.
   - Deploy to an Android Emulator or physical device.

---

## 🕹️ Game Modes
- **Practice:** Local play against AI (no cost).
- **Online VS:** Competitive play (2 or 4 players) with LUDC entry fees.
- **Team Mode (2v2):** Red & Yellow vs. Green & Blue.
- **Tournaments:** Large-scale competitions with tiered prize pools.

---

## 🤝 Contributing & Support
We welcome contributions! Please fork the repository and submit a pull request for any features or bug fixes.

- **Developer:** Tassaduq Hussain
- **Email:** [tassaduq009@gmail.com](mailto:tassaduq009@gmail.com)
- **WhatsApp:** [+44 7435 745935](https://wa.me/447435745935)
- **Website:** [www.ludocities.com](https://www.ludocities.com)

---
*MIT License - Copyright (c) 2024 ColabLudo*
