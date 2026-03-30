# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

All commands run from the `src/` directory:

```bash
dotnet build                    # Build entire solution
dotnet test                     # Run all tests
dotnet test --filter "FullyQualifiedName~BattleTest"  # Run a single test class
dotnet test --filter "FullyQualifiedName~BattleTest.AttackEnemy_WithUnits_ReturnsResult"  # Single test
dotnet run --project BrowserGameEngine.StatefulGameServer.Benchmarks  # Run benchmarks
```

Local dev with Docker (requires `GITHUB_CLIENT_ID` and `GITHUB_CLIENT_SECRET` env vars):
```bash
docker-compose up
```

CI runs `dotnet build --configuration Release` and `dotnet test` on push/PR to master.

## Architecture

Stateful monolith: per-game world state lives in `WorldState` (one per active game), cross-game state in `GlobalState` (users, game registry, achievements). Both are serialized to blob storage every 10 seconds. A `GameRegistry` singleton manages all active `GameInstance` objects. Blazor WebAssembly client communicates via REST API (no SignalR).

See `docs/ARCHITECTURE.md` for the full architecture reference.

### Project Dependency Graph (dependencies flow downward only)

```
BlazorClient            → Shared (ViewModels only)
FrontendServer          → everything (sole composition root)
StatefulGameServer      → GameDefinition, GameModel, Persistence
Persistence.S3          → Persistence
Persistence             → GameModel
Shared                  → GameDefinition, GameModel
GameDefinition.SCO      → GameDefinition, GameModel
GameModel               → GameDefinition
GameDefinition          → (nothing)
```

### Layer Responsibilities

- **GameDefinition** — Pure immutable game config (resources, units, assets, costs). No logic, no state.
- **GameModel** — Immutable snapshot types (`WorldStateImmutable`, `GlobalStateImmutable`, `GameRecordImmutable`, `PlayerAchievementImmutable`, etc.) for serialization.
- **GameDefinition.SCO** — StarCraft Online concrete game data. To make a different game, create a new `GameDefinition.XXX` project.
- **Persistence** — `IBlobStorage` abstraction. `FileStorage` (local dev) or `S3Storage` (prod). Per-game state at `games/{gameId}/state.json`; global state at `global/state.json`.
- **StatefulGameServer** — Core engine. `GameRegistry` holds all active `GameInstance` objects. Repositories inject `IWorldStateAccessor` to access the appropriate `WorldState`. Read/write repositories are strictly separated. Game tick modules implement `IGameTickModule`.
- **Shared** — ViewModels/DTOs. The API contract between server and client.
- **FrontendServer** — ASP.NET Core host (composition root). Controllers, hosted services, middleware. All game endpoints require `[Authorize]` and must check `CurrentUserContext.IsValid`.
- **BlazorClient** — Blazor WASM SPA. Only references Shared. Uses `RefreshService` event bus for cross-component updates.

### Key Patterns

- **Immutable/mutable duality**: `GameModel` types are immutable snapshots; `StatefulGameServer/GameModelInternal/` has mutable counterparts. Convert via `ToImmutable()`/`ToMutable()`.
- **`IWorldStateAccessor`**: Repositories inject this interface (not `WorldState` directly) so the same class works for any game instance.
- **Read/write repository separation**: Different classes for queries vs mutations. Write repos use `lock` for thread safety.
- **Tick-based simulation**: Each `GameInstance` has its own `GameTickEngine`. `GameTickTimerService` iterates all active instances. New periodic behavior = new `IGameTickModule` registered in `GameServerExtensions`.
- **Commands as records**: Player actions are record types in `Commands/` (e.g., `BuildAssetCommand`, `SendUnitCommand`).
- **Thread safety**: `WorldState.Players` and `GlobalState.Users` use `ConcurrentDictionary`. Write repos use `lock`. Tick engine uses `Interlocked` guards.

### Adding a New Game Feature

1. Add command record in `StatefulGameServer/Commands/`
2. Add or extend repository (read + write); inject `IWorldStateAccessor`, not `WorldState`
3. Add ViewModel in `Shared`
4. Add controller endpoint in `FrontendServer/Controllers/`
5. Add Blazor page in `BlazorClient/Pages/`
6. If tick-based, add `IGameTickModule` and register in `GameServerExtensions`

## Code Style

Governed by `.editorconfig`:
- **Tabs** for C# indentation (4-space width), **spaces** for XML/JSON/YAML (2-space)
- LF line endings, UTF-8, file-scoped namespaces
- `var` only when type is apparent; braces preferred
- Newline before open brace for types and methods only (not `else`/`catch`/`finally`)
- Sort `using` directives with System first

## Configuration

| Key | Purpose |
|-----|---------|
| `Bge:DevAuth` | Enable password-less dev login (default: `true`) |
| `Bge:S3BucketName` | S3 bucket; empty = local `FileStorage` |
| `Bge:S3KeyPrefix` | S3 key prefix |
| `GitHub:ClientId` | GitHub OAuth client ID |
| `GitHub:ClientSecret` | GitHub OAuth client secret |

## Testing

Tests are in `StatefulGameServer.Test` (xUnit). Test game logic through repositories using the `TestGame` helper class, not through controllers.

## Development Workflow (Agent/PR-based)

All changes go through GitHub PRs — never push directly to `master`.

Each task gets an **isolated git worktree** so multiple branches can be active simultaneously:

```bash
# Create worktree for a new task
git worktree add /tmp/bge-work/<branch-name> -b <branch-name> master
cd /tmp/bge-work/<branch-name>

# ... implement, build, test, commit ...

# Push and open PR
git push -u origin <branch-name>
gh pr create --title "<title>" --body "..."

# Clean up worktree after PR is open
cd /home/chris/repos/my/bge
git worktree remove /tmp/bge-work/<branch-name>
```

List all active worktrees: `git worktree list`
