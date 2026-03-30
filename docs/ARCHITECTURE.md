# Architecture

## Overview

Browser Game Engine (BGE) is a stateful monolith hosting a multi-game platform for StarCraft-themed browser strategy games. The server manages multiple concurrent game instances, each with its own in-memory `WorldState`. A separate `GlobalState` holds cross-game data (users, game registry, player achievements). All state is serialized periodically to blob storage. The client is a Blazor WebAssembly SPA that communicates via REST API.

## Project Dependency Graph

```
GameDefinition          ← no dependencies (pure data)
GameModel               ← GameDefinition
Persistence             ← GameModel
StatefulGameServer      ← GameDefinition, GameModel, Persistence
Shared (ViewModels)     ← GameDefinition, GameModel
GameDefinition.SCO      ← GameDefinition, GameModel
Persistence.S3          ← Persistence
BlazorClient            ← Shared (ViewModels)
FrontendServer          ← everything (composition root)
```

## Layers

### 1. Game Definition (`BrowserGameEngine.GameDefinition`)
Static, immutable game configuration: player types, resources, units, assets, costs, tick modules. Defines _what exists_ in a game. No logic, no state.

- `IGameDefFactory` — creates a `GameDef` instance
- `GameDefVerifier` — validates a definition at startup

### 2. Game Model (`BrowserGameEngine.GameModel`)
Immutable snapshot types representing per-game world state and cross-game global state: `WorldStateImmutable`, `PlayerImmutable`, `UnitImmutable`, `GlobalStateImmutable`, `GameRecordImmutable`, `PlayerAchievementImmutable`, etc. Used for serialization and as the persistence format.

- `IWorldStateFactory` — creates initial world state for a new game
- `GameRecordImmutable` — game metadata: `GameId`, `Name`, `GameDefType`, `Status` (`Upcoming | Active | Finished`), `StartTime`, `EndTime`, `TickDuration`, `WinnerId`
- `GlobalStateImmutable` — cross-game container: users, game registry, achievements

### 3. Game Definition SCO (`BrowserGameEngine.GameDefinition.SCO`)
The StarCraft Online implementation. Implements `IGameDefFactory` and `IWorldStateFactory` with concrete game data (3 races, units, buildings, costs).

**Rule**: To create a different game, create a new `GameDefinition.XXX` project implementing the same interfaces. The engine is generic; game-specific data belongs here.

### 4. Persistence (`BrowserGameEngine.Persistence`, `BrowserGameEngine.Persistence.S3`)
Blob storage abstraction. Serializes world state and global state to JSON and stores them.

- `IBlobStorage` — `Store`, `Load`, `Exists`
- Implementations: `FileStorage` (local, dev), `S3Storage` (AWS, prod)
- `PersistenceService` — load/store per-game state at `games/{gameId}/state.json`
- `GlobalPersistenceService` — load/store global state at `global/state.json`
- `GameStateJsonSerializer`, `GlobalStateJsonSerializer` — JSON serialization with custom converters
- `MigrationService` — one-time startup migration from `latest.json` (pre-multi-game) to the new blob key scheme

**Blob key scheme:**

| Blob key | Contains |
|---|---|
| `global/state.json` | `GlobalStateImmutable` (users, game registry, achievements) |
| `games/{gameId}/state.json` | `WorldStateImmutable` for that game |

### 5. Stateful Game Server (`BrowserGameEngine.StatefulGameServer`)
Core game logic. Owns mutable in-memory state split between `WorldState` (per-game) and `GlobalState` (cross-game).

**Multi-game runtime objects:**

- `GlobalState` — mutable in-memory mirror of `GlobalStateImmutable`. Holds `Users` (`ConcurrentDictionary`), `Games` (list of `GameRecordImmutable`), and `Achievements` (list of `PlayerAchievementImmutable`). Singleton.
- `GameInstance` — runtime container for one active or upcoming game: `GameRecordImmutable`, `WorldState`, `GameDef`, `GameTickEngine`, and `IWorldStateAccessor`.
- `GameRegistry` — singleton. Holds `GlobalState` and a `ConcurrentDictionary<GameId, GameInstance>`. Provides `GetDefaultInstance()`, `TryGetInstance(gameId)`, `GetAllInstances()`. Populated at startup from persisted state.
- `GameLifecycleEngine` — transitions games between statuses (`Upcoming → Active`, `Active → Finished`), writes `PlayerAchievement` records on finalization, and frees memory for finished games.

**Internal model** (`GameModelInternal/`): Mutable counterparts to the immutable model — `WorldState`, `GlobalState`, `Player`, `PlayerState`, `Unit`. Converted via `ToImmutable()`/`ToMutable()` extensions.

**`IWorldStateAccessor` pattern**: All repositories receive an `IWorldStateAccessor` (not `WorldState` directly). This indirection allows the same repository classes to work against any game's `WorldState`.

```csharp
public interface IWorldStateAccessor {
    WorldState WorldState { get; }
}
```

`SingletonWorldStateAccessor` wraps a fixed `WorldState` instance (used for the default/current registered game in DI).

**Repositories** — read/write separated:
| Read | Write | Concern |
|------|-------|---------|
| `PlayerRepository` | `PlayerRepositoryWrite` | Players, attackability, protection |
| `ResourceRepository` | `ResourceRepositoryWrite` | Resource queries/mutations, trading |
| `AssetRepository` | `AssetRepositoryWrite` | Buildings |
| `UnitRepository` | `UnitRepositoryWrite` | Army units, return timers |
| `ScoreRepository` | — | Per-game land ranking |
| `AllianceRepository` | `AllianceRepositoryWrite` | Alliance CRUD and membership |
| `AllianceScoreRepository` | — | Alliance ranking |
| `MessageRepository` | `MessageRepositoryWrite` | Private messages and battle reports |
| `UpgradeRepository` | `UpgradeRepositoryWrite` | Attack/defense upgrade levels and timers |
| `BuildQueueRepository` | `BuildQueueRepositoryWrite` | Automated build/train queue |
| `ActionQueueRepository` | — | Legacy build queue reads |
| `OnlineStatusRepository` | — | Online/offline status by last-seen time |
| `UserRepository` | `UserRepositoryWrite` | User accounts (global, via `GlobalState`) |
| `ColonizeRepositoryWrite` | — | Land colonization |

**Game tick engine** (`GameTicks/`):
- `GameTickEngine` — checks if ticks are due, advances world and per-player ticks. One instance per `GameInstance`.
- `GameTickModuleRegistry` — discovers and configures `IGameTickModule` implementations
- Modules: `ActionQueueExecutor`, `UnitReturn`, `ResourceGrowthSco`, `NewPlayerProtectionModule`, `UpgradeTimer`, `BuildQueueModule`

**Battle** (`Repositories/Battle/`):
- `IBattleBehavior` — interface for combat resolution
- `BattleBehaviorScoOriginal` — the original SCO formula; supports attack/defense upgrade bonuses and land transfer on victory
- `BattleReportGenerator` — generates structured battle reports delivered as messages

**Commands** (`Commands/`): Record types for player actions (`BuildAssetCommand`, `SendUnitCommand`, `AssignWorkersCommand`, `TradeResourceCommand`, `ColonizeCommand`, etc.)

All repositories, `WorldState`, and `GlobalState` are registered as **singletons** in DI (scoped to the default game for the current single-active-game setup).

### 6. Shared / ViewModels (`BrowserGameEngine.Shared`)
DTOs for API responses. Referenced by both server controllers and Blazor client. Includes game management ViewModels (`GameSummaryViewModel`, `GameDetailViewModel`, `CreateGameRequest`) and leaderboard ViewModels (`LeaderboardEntryViewModel`, `GameResultViewModel`).

### 7. Frontend Server (`BrowserGameEngine.FrontendServer`)
ASP.NET Core host — the composition root. Wires everything together.

**Controllers** (all `[Authorize]` unless noted):
- `AuthenticationController` — GitHub OAuth + dev login
- `PlayerProfileController` — create/read/delete player
- `PlayerManagementController` — admin player operations
- `ResourceController` — resource mutations and 2:1 trading
- `WorkersController` — worker role assignment (mineral/gas/idle)
- `AssetsController`, `UnitsController` — build actions
- `UpgradesController` — attack/defense upgrade research
- `BattleController` — attack flow (send units, resolve battle, land transfer)
- `ColonizeController` — land colonization
- `BuildQueueController` — automated build queue management
- `AlliancesController` — alliance create/join/leave
- `MessagesController` — private messages and battle reports
- `PlayerRankingController`, `AllianceRankingController`, `UnitDefinitionsController` — read-only
- `GamesController` — list games, create game, get game details (`[AllowAnonymous]` for reads)
- `LeaderboardController` — cross-game all-time rankings (`[AllowAnonymous]`)
- `GameInfoController` — game metadata endpoint
- `AdminController` — administrative operations

**Hosted services** (background):
- `GameTickTimerService` (10s) — calls `GameTickEngine.CheckAllTicks()` for each active game instance
- `PersistenceHostedService` (10s) — serializes and stores each active game's `WorldState`
- `GlobalPersistenceHostedService` (10s) — serializes and stores `GlobalState`
- `GameLifecycleService` (60s) — transitions game statuses, finalizes ended games, writes achievements

**Middleware**:
- `CurrentUserMiddleware` — resolves authenticated user to `CurrentUserContext` (scoped)
- `BearerTokenMiddleware` — API key auth for agent/bot clients

**Auth**: Cookie-based with GitHub OAuth2. AJAX requests get 401 instead of redirect. API key auth via `BearerTokenMiddleware`.

### 8. Blazor Client (`BrowserGameEngine.BlazorClient`)
Blazor WebAssembly SPA. Calls the REST API via `HttpClient`. No SignalR — polling only.

- `RedirectIfUnauthorizedHandler` — intercepts 401s, redirects to `/signin`
- `RefreshService` — event bus for cross-component refresh

**Pages**: Index, Base, Units, EnemyBase, SelectEnemy, PlayerRanking, PlayerProfile, CreatePlayer, UnitDefinition, Upgrades, Alliances, AllianceDetail, AllianceRanking, Messages, Players

## Data Flow

```
User action → Blazor Page → HTTP POST → Controller → Write Repository → WorldState (in memory)
                                              ↓
                                    Validates via Read Repository
                                              ↓
Background:  GameTickTimerService → GameTickEngine (per instance) → IGameTickModule(s) → WorldState
Background:  PersistenceHostedService → WorldState.ToImmutable() → JSON → IBlobStorage (games/{id}/state.json)
Background:  GlobalPersistenceHostedService → GlobalState.ToImmutable() → JSON → IBlobStorage (global/state.json)
Background:  GameLifecycleService → GlobalState game records → transitions Upcoming/Active/Finished
```

## Startup Sequence

1. Create `GameDef` from `StarcraftOnlineGameDefFactory`, verify it
2. Choose storage: S3 if `Bge:S3BucketName` is set, else local `FileStorage`
3. Run `MigrationService` (one-time: converts `latest.json` to multi-game blob layout if needed)
4. Load `GlobalState` from `global/state.json`, or create a new empty `GlobalState`
5. For each non-Finished game in `GlobalState.Games`, load its `WorldState` from `games/{gameId}/state.json` and register a `GameInstance` in `GameRegistry`
6. If no game instances exist, create a default `"default"` game instance
7. Register all singletons (GameRegistry, GlobalState, WorldState, repositories, tick engine)
8. Wire tick engines into game instances
9. Start `GameTickTimerService`, `PersistenceHostedService`, `GlobalPersistenceHostedService`, `GameLifecycleService`

## Infrastructure

- **Deployment**: AWS ECS Fargate (512 CPU / 1024 MB) via Terraform (`infra/main.tf`)
- **Storage**: S3 bucket with versioning for game state
- **Secrets**: AWS SSM Parameter Store (GitHub OAuth credentials)
- **Networking**: ALB → ECS on port 8080, health check at `/health`
- **Container**: Multi-stage Docker build, .NET 10 runtime
- **Observability**: Serilog (console), Prometheus metrics, OpenTelemetry tracing, CloudWatch alarms
- **Local dev**: Docker Compose with `Bge__DevAuth=true`

## Configuration

| Key | Purpose | Default |
|-----|---------|---------|
| `Bge:DevAuth` | Enable password-less dev login | `true` |
| `Bge:S3BucketName` | S3 bucket (empty = local files) | `""` |
| `Bge:S3KeyPrefix` | S3 key prefix | `""` |
| `GitHub:ClientId` | OAuth client ID | required |
| `GitHub:ClientSecret` | OAuth client secret | required |

## Rules for Future Development

### Architecture
- **Dependencies flow downward only.** `GameDefinition` and `GameModel` must never reference upper layers.
- **Game-specific logic belongs in `GameDefinition.SCO`**, not in the engine. The engine projects (`GameModel`, `StatefulGameServer`, `Persistence`) should remain game-agnostic.
- **`FrontendServer` is the only composition root.** No other project should wire DI or reference all projects.
- **ViewModels are the API contract.** Controllers return ViewModels. Never expose internal mutable types or `GameModel` immutables directly to the client.
- **To add a new game type**, create a new `GameDefinition.XXX` project. Register its factory in `GameServerExtensions` keyed by the `GameDefType` string.

### State Management
- **Per-game state lives in `WorldState`; cross-game state lives in `GlobalState`.** Do not put user accounts or game registry metadata inside `WorldState`.
- **`IWorldStateAccessor` is the injection point for game-scoped state.** Repositories take `IWorldStateAccessor`, not `WorldState` directly.
- **Read repositories are pure queries. Write repositories mutate.** Keep this separation strict.
- **Thread safety**: `WorldState.Players` uses `ConcurrentDictionary`. `GlobalState.Users` uses `ConcurrentDictionary`. Write repositories use `lock` where needed. The tick engine uses `Interlocked` guards against concurrent execution.
- **Persistence is eventual.** A crash loses up to 10 seconds of state. This is by design.

### Game Lifecycle
- **Games transition:** `Upcoming → Active → Finished`. `GameLifecycleService` drives these transitions automatically based on `StartTime`/`EndTime`.
- **Finished games are evicted from memory.** Only `Active` and `Upcoming` game instances are kept in `GameRegistry`. `Finished` games' world states are persisted to blob storage before eviction.
- **`PlayerAchievement` records are written at finalization.** They capture final rank and score for each player that has a linked user account.

### Game Tick System
- **`IGameTickModule` is the extension point** for per-tick logic. New periodic behavior should be a new module, registered in `GameServerExtensions`.
- **Tick modules are configured via `GameTickModuleDef` properties** in the game definition, not hardcoded.
- **Each `GameInstance` owns one `GameTickEngine`.** The timer service iterates all active instances.

### API
- **All game endpoints require `[Authorize]`** and must check `CurrentUserContext.IsValid`.
- **Game list and leaderboard endpoints are public** (`[AllowAnonymous]`).
- **No SignalR currently.** The client polls. If adding real-time push, use SignalR and keep the REST API as-is.

### Adding Features from the Specification
`docs/SPECIFICATION.md` describes the full original game. When implementing new features:
- Add new commands in `Commands/`
- Add new repositories (read + write) or extend existing ones; always inject `IWorldStateAccessor`
- Add new `IGameTickModule` implementations for tick-based processing; register in `GameServerExtensions`
- Add new ViewModels in `Shared` and new controllers in `FrontendServer`
- Add new Blazor pages in `BlazorClient`

### Testing
- `StatefulGameServer.Test` — unit tests for game logic
- `StatefulGameServer.Benchmarks` — performance benchmarks
- Test game logic through repositories, not controllers
