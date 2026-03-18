# ColabLudo: Technical Solution Overview

## 1. Project Overview
ColabLudo is a collaborative, blockchain-integrated Ludo gaming platform. It utilizes a monorepo structure to manage a .NET MAUI mobile frontend, an ASP.NET Core backend, and shared logic. The solution integrates the Solana blockchain for in-game currency (LUDC) and tournament rewards.

## 2. Architecture Layers

### A. Frontend (LudoClient)
- **Framework:** .NET MAUI (Optimized for Android).
- **Responsibilities:** - Game UI/UX rendering.
    - Integration with **Solana Mobile Wallet Adapter (MWA)** for transaction signing (planned).
    - Real-time communication via **SignalR**.
    - Player authentication (Google Auth / OTP).

### B. Backend (LudoServer)
- **Framework:** ASP.NET Core Web API.
- **Database:** SQL Server managed via **Entity Framework Core (EF Core)**.
- **Key Modules:**
    - `PlayerController`: Manages user profiles and stats.
    - `TournamentController`: Handles game matchmaking and logic.
    - `ChatMessageController`: Real-time social features.
    - `OTP/GoogleAuth`: Identity management.

### C. Shared Logic (SharedCode)
- **Format:** Class Library (.NET).
- **Contents:** - Data Transfer Objects (DTOs).
    - Shared models (Player, GameState, Tournament).
    - Validation logic shared between Client and Server to ensure data integrity.

## 3. Blockchain Integration
- **Network:** Solana Mainnet/Devnet.
- **Token:** LUDC (Token-2022 Program).
- **Mint Address:** `JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai`.
- **DevNet Mint Address:** `8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW`.
- **Logic:** Transactions for tournament entry fees and reward distributions are initiated by the LudoClient and verified by the LudoServer. Ludo server manages a db ledger of the transactions and monitors the chain for deposit and update the database.

## 4. Communication Flow
1. **Auth:** User authenticates via Google/OTP -> Server issues a JWT.
2. **Game Setup:** Client requests a tournament -> Server creates session in SQL Server.
3. **Payments:** Client signs a Solana transaction for LUDC -> Transaction ID sent to Server for verification.
4. **Gameplay:** Real-time moves broadcasted via SignalR hubs.