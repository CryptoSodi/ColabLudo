# Project Gemini: ColabLudo Context Brief

## Project Identity
- **Name:** ColabLudo
- **Owner:** CryptoSodi
- **Focus:** Collaborative Blockchain Gaming (Ludo)

## Technical Stack
- **Languages:** C#, JavaScript (Web components).
- **Frontend:** .NET MAUI (Android-only target).
- **Backend:** ASP.NET Core 8.0/9.0.
- **ORM:** Entity Framework Core.
- **Blockchain:** Solana (Token-2022).
- **Real-time:** SignalR.

## Key Assets & Identifiers
- **LUDC Token Mint:** `JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai`
- **Core Repositories:**
    - `/LudoClient`: MAUI Android App.
    - `/LudoServer`: ASP.NET Core Web API.
    - `/SharedCode`: Common DTOs and Logic.

## Development Constraints
- **Platform:** The mobile application is strictly developed for **Android** using .NET MAUI.
- **State Management:** Hybrid model where the Server maintains the authoritative game state, and Solana handles the value layer.
- **Security:** Use of Solana Mobile Wallet Adapter (MWA) for non-custodial wallet interactions.

## Current Goals
- Integrating `LudoServer` and `SharedCode` into a cohesive monorepo structure.
- Finalizing the real-time SignalR hubs for cross-player communication.
- Implementing the Solana transaction verification logic on the backend.