# Module Context: SharedCode (DTOs & Models)

## Responsibilities
- Providing a single source of truth for data structures used by Client, Server, and SignalR.
- Defining **DTOs** (Data Transfer Objects) for Hub messages.
- Storing shared constants like the **LUDC Token Mint address**.
- **CoreEngine:** Facilitating Game logic.

## Maintenance Rules
- **No Dependencies:** Avoid UI or DB-specific libraries.
- **JSON Compatibility:** Ensure all classes are compatible with `System.Text.Json`.

## AI Instructions
- Maintain strict property naming conventions for seamless serialization across services.