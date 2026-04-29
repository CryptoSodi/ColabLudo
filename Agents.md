# LudoClient Repo: Agent Notes

This repository contains a .NET MAUI game client (`LudoClient`) plus server-side components used during development (`LudoServer`, `SignalR`, `Ludo.Api`, and worker-style utilities). The project is asset-heavy and uses a mix of MAUI XAML views and native Android views (handlers) for performance-sensitive lists.

## Quick Orientation

- `LudoClient/`
  - .NET MAUI app (Android + Windows targets).
  - UI pages: `*.xaml` + code-behind `*.xaml.cs`.
  - Reusable controls: `LudoClient/ControlView/`.
  - Android native handlers/layouts: `LudoClient/Platforms/Android/`.
  - MAUI image assets: `LudoClient/Resources/Images/` (not Android `Resources/drawable`).
- `SharedCode/`
  - Shared DTOs/constants/network wrapper (`SharedCode/Network/Client.cs` etc.).
- `LudoServer/`
  - EF Core models and DB context used by server-side pieces.
- `SignalR/SignalR.Server/`
  - Gameplay SignalR server / hub host (also used as startup for migrations).
- `Ludo.Api/`
  - HTTP API endpoints for non-gameplay features (daily bonus, profile sync, wallet tabs, social, tournaments, etc.).

## Build / Compile

From repo root (`C:\repos\LudoClient`) in PowerShell:

- Android compile:
  - `dotnet msbuild LudoClient\LudoClient.csproj /t:Compile /p:TargetFramework=net9.0-android /p:Configuration=Release /p:Restore=false`
- Windows compile (on Windows only):
  - `dotnet msbuild LudoClient\LudoClient.csproj /t:Compile /p:TargetFramework=net9.0-windows10.0.19041.0 /p:Configuration=Release /p:Restore=false`
- API build:
  - `dotnet build Ludo.Api\Ludo.Api.csproj`
- SignalR server build:
  - `dotnet build SignalR\SignalR.Server\SignalR.Server.csproj`

Notes:
- This repo is frequently in a “moving” state; keep changes scoped and re-run compile for the target you touched.
- Avoid broad dependency upgrades unless explicitly requested.

## Database & EF Core

- `LudoServer` owns EF Core models and migrations.
- `SignalR.Server` is commonly used as the EF startup project.
- Typical pattern:
  - `dotnet ef migrations add <Name> --project LudoServer\LudoServer.csproj --startup-project SignalR\SignalR.Server\SignalR.Server.csproj --context LudoServer.Data.LudoDbContext`
  - `dotnet ef database update --project LudoServer\LudoServer.csproj --startup-project SignalR\SignalR.Server\SignalR.Server.csproj --context LudoServer.Data.LudoDbContext`

If DB drift occurs (manual table edits), prefer generating an explicit SQL script for the delta rather than guessing the DB state.

## Architecture Conventions

### SignalR vs API

- SignalR is reserved for gameplay / real-time match coordination.
- Non-gameplay flows are being migrated to HTTP API (`Ludo.Api`) for stability and scaling:
  - daily bonus
  - session/profile sync
  - wallet/deposit/withdraw tabs and history
  - tournaments listing/join/results
  - social/friends endpoints

When adding new client flows:
- Prefer API endpoints for CRUD / “business actions”.
- Keep SignalR use minimal and only where latency-sensitive real-time messaging is required.

### Session Sync

For frequent keep-alive or balance refresh:
- Prefer lightweight sync endpoints (small DTO) rather than returning full `PlayerInfo` with transactions.

For profile/wallet detail screens:
- It is acceptable to fetch full `PlayerInfo` (includes transactions) on page open.

## UI / Performance Conventions

### Asset-first UI

- Most UI uses image-backed “capsules” and themed assets.
- Reuse existing assets and page shells (e.g., Tournament/Leaderboard container patterns) instead of inventing new styles.

### Performance-sensitive lists

Heavy list rows in MAUI XAML can cause jank on Android. Preferred pattern:
- Create a MAUI “proxy” view type and an Android handler that inflates an XML layout:
  - Example: `LudoClient/Platforms/Android/NativeTournamentCard.cs` + `Resources/layout/item_tournament_detail.xml`.
- For wallet/history lists, use the same approach:
  - An XML-backed native row view with dynamic detail sections built in C#.

When porting a MAUI control to Android-native:
- Keep the shared control as a fallback for non-Android.
- Keep behavior parity (expand/collapse, field truncation, status mapping).
- Register handlers in `LudoClient/MauiProgram.cs` under `#if ANDROID`.

### Where to edit visual design

- Cross-platform visuals: `LudoClient/Resources/Images/**` and MAUI XAML.
- Android-only visuals: `LudoClient/Platforms/Android/Resources/layout/**` and `.../Resources/drawable/**`.

Avoid duplicating assets unnecessarily. If a MAUI image is needed in Android native layout:
- Either add an Android platform drawable variant, or create a drawable-backed approximation that matches the style.

## Logging

- Add concise logs around network calls and state transitions.
- Avoid log spam in tight loops; log summaries and exceptional cases.

## Change Discipline

- Do not “clean up” unrelated warnings while working on a task.
- Avoid large refactors; prefer small, testable patches.
- If you must change shared DTO shape, update both API + client together.

## Token / Scope Rules

- Do not scan the whole repository unless the user explicitly asks.
- Start from the exact files or folders mentioned by the user.
- Inspect a maximum of 3 files first, then ask before expanding.
- Prefer search/grep over opening full files.
- Read only the relevant sections of files.
- Do not open generated files, build outputs, binaries, assets, or large logs unless required.
- Do not run full builds unless the user asks.
- When running commands, keep output short and show only relevant errors.
- Summarize logs instead of pasting full output.
- Avoid subagents unless the task clearly requires them.

## Hard Limits (Token Safety)

- Hard limit: inspect maximum 3 files unless user explicitly allows more.
- Hard limit: do not read files larger than 500 lines unless necessary.
- If scope is unclear, ASK instead of exploring.
- Prefer targeted edits over full-file rewrites.

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- For cross-module "how does X relate to Y" questions, prefer `graphify query "<question>"`, `graphify path "<A>" "<B>"`, or `graphify explain "<concept>"` over grep — these traverse the graph's EXTRACTED + INFERRED edges instead of scanning files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
