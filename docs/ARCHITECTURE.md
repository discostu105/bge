# Architecture

## Overview

Browser Game Engine (BGE) is a stateful monolith hosting a StarCraft-themed browser strategy game. All game state lives in memory, serialized periodically to blob storage. The client is a Blazor WebAssembly SPA that communicates via REST API.

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
Immutable snapshot types representing world state: `WorldStateImmutable`, `PlayerImmutable`, `UnitImmutable`, etc. Used for serialization and as the persistence format.

- `IWorldStateFactory` — creates initial world state

### 3. Game Definition SCO (`BrowserGameEngine.GameDefinition.SCO`)
The StarCraft Online implementation. Implements `IGameDefFactory` and `IWorldStateFactory` with concrete game data (3 races, units, buildings, costs).

**Rule**: To create a different game, create a new `GameDefinition.XXX` project implementing the same interfaces. The engine is generic; game-specific data belongs here.

### 4. Persistence (`BrowserGameEngine.Persistence`, `BrowserGameEngine.Persistence.S3`)
Blob storage abstraction. Serializes `WorldStateImmutable` to JSON and stores it.

- `IBlobStorage` — `Store`, `Load`, `Exists`
- Implementations: `FileStorage` (local, dev), `S3Storage` (AWS, prod)
- `PersistenceService` — orchestrates load/save of `latest.json`
- `GameStateJsonSerializer` — JSON serialization with custom converters

### 5. Stateful Game Server (`BrowserGameEngine.StatefulGameServer`)
Core game logic. Owns the mutable in-memory `WorldState`.

**Internal model** (`GameModelInternal/`): Mutable counterparts to the immutable model — `WorldState`, `Player`, `PlayerState`, `Unit`. Converted via `ToImmutable()`/`ToMutable()` extensions.

**Repositories** — read/write separated:
| Read | Write | Concern |
|------|-------|---------|
| `PlayerRepository` | `PlayerRepositoryWrite` | Players, attackability |
| `ResourceRepository` | `ResourceRepositoryWrite` | Resource queries/mutations |
| `AssetRepository` | `AssetRepositoryWrite` | Buildings |
| `UnitRepository` | `UnitRepositoryWrite` | Army units |
| `ScoreRepository` | — | Ranking |
| `ActionQueueRepository` | — | Build/train queue |

**Game tick engine** (`GameTicks/`):
- `GameTickEngine` — checks if ticks are due, advances world and per-player ticks
- `GameTickModuleRegistry` — discovers and configures `IGameTickModule` implementations
- Modules: `ActionQueueExecutor`, `UnitReturn`, `ResourceGrowthSco`

**Battle** (`Repositories/Battle/`):
- `IBattleBehavior` — interface for combat resolution
- `BattleBehaviorScoOriginal` — the original SCO formula

**Commands** (`Commands/`): Record types for player actions (`BuildAssetCommand`, `SendUnitCommand`, etc.)

All repositories and `WorldState` are registered as **singletons**.

### 6. Shared / ViewModels (`BrowserGameEngine.Shared`)
DTOs for API responses. Referenced by both server controllers and Blazor client.

### 7. Frontend Server (`BrowserGameEngine.FrontendServer`)
ASP.NET Core host — the composition root. Wires everything together.

**Controllers** (all `[Authorize]`):
- `AuthenticationController` — Discord OAuth + dev login
- `PlayerProfileController` — CRUD player
- `ResourceController`, `AssetsController`, `UnitsController` — game actions
- `BattleController` — attack flow
- `PlayerRankingController`, `UnitDefinitionsController` — read-only

**Hosted services** (background timers, both 10s interval):
- `GameTickTimerService` — calls `GameTickEngine.CheckAllTicks()`
- `PersistenceHostedService` — serializes and stores world state

**Middleware**:
- `CurrentUserMiddleware` — resolves authenticated user to `CurrentUserContext` (scoped)

**Auth**: Cookie-based with Discord OAuth2. AJAX requests get 401 instead of redirect.

### 8. Blazor Client (`BrowserGameEngine.BlazorClient`)
Blazor WebAssembly SPA. Calls the REST API via `HttpClient`. No SignalR — polling only.

- `RedirectIfUnauthorizedHandler` — intercepts 401s, redirects to `/signin`
- `RefreshService` — event bus for cross-component refresh

## Data Flow

```
User action → Blazor Page → HTTP POST → Controller → Write Repository → WorldState (in memory)
                                              ↓
                                    Validates via Read Repository
                                              ↓
Background:  GameTickTimerService → GameTickEngine → IGameTickModule(s) → WorldState
Background:  PersistenceHostedService → WorldState.ToImmutable() → JSON → IBlobStorage
```

## Startup Sequence

1. Create `GameDef` from `StarcraftOnlineGameDefFactory`, verify it
2. Choose storage: S3 if `Bge:S3BucketName` is set, else local `FileStorage`
3. Load existing `latest.json` from storage, or create default world state
4. Register all singletons (WorldState, repositories, tick engine, battle)
5. Start `GameTickTimerService` and `PersistenceHostedService`

## Infrastructure

- **Deployment**: AWS ECS Fargate (512 CPU / 1024 MB) via Terraform (`infra/main.tf`)
- **Storage**: S3 bucket with versioning for game state
- **Secrets**: AWS SSM Parameter Store (Discord credentials)
- **Networking**: ALB → ECS on port 8080, health check at `/health`
- **Container**: Multi-stage Docker build, .NET 10 runtime
- **Observability**: Serilog (console), Prometheus metrics, OpenTelemetry tracing
- **Local dev**: Docker Compose with `Bge__DevAuth=true`

## Configuration

| Key | Purpose | Default |
|-----|---------|---------|
| `Bge:DevAuth` | Enable password-less dev login | `true` |
| `Bge:S3BucketName` | S3 bucket (empty = local files) | `""` |
| `Bge:S3KeyPrefix` | S3 key prefix | `""` |
| `Discord:ClientId` | OAuth client ID | required |
| `Discord:ClientSecret` | OAuth client secret | required |

## Rules for Future Development

### Architecture
- **Dependencies flow downward only.** `GameDefinition` and `GameModel` must never reference upper layers.
- **Game-specific logic belongs in `GameDefinition.SCO`**, not in the engine. The engine projects (`GameModel`, `StatefulGameServer`, `Persistence`) should remain game-agnostic.
- **`FrontendServer` is the only composition root.** No other project should wire DI or reference all projects.
- **ViewModels are the API contract.** Controllers return ViewModels. Never expose internal mutable types or `GameModel` immutables directly to the client.

### State Management
- **All game state is in the singleton `WorldState`.** There is no database. Respect this — don't introduce partial persistence or side-channel state.
- **Read repositories are pure queries. Write repositories mutate.** Keep this separation strict.
- **Thread safety**: `WorldState.Players` uses `ConcurrentDictionary`. Write repositories use `lock` where needed. The tick engine uses `Interlocked` guards against concurrent execution.
- **Persistence is eventual.** A crash loses up to 10 seconds of state. This is by design.

### Game Tick System
- **`IGameTickModule` is the extension point** for per-tick logic. New periodic behavior should be a new module, registered in `GameServerExtensions`.
- **Tick modules are configured via `GameTickModuleDef` properties** in the game definition, not hardcoded.

### API
- **All game endpoints require `[Authorize]`** and must check `CurrentUserContext.IsValid`.
- **No SignalR currently.** The client polls. If adding real-time push, use SignalR and keep the REST API as-is.

### Adding Features from the Specification
`docs/SPECIFICATION.md` describes the full original game. Many features are not yet implemented (upgrades, alliances, messaging, colonization, chat, build queue automation, worker assignment). When implementing these:
- Add new commands in `Commands/`
- Add new repositories or extend existing ones
- Add new `IGameTickModule` implementations for tick-based processing
- Add new ViewModels in `Shared` and new controllers in `FrontendServer`
- Add new Blazor pages in `BlazorClient`

### Testing
- `StatefulGameServer.Test` — unit tests for game logic
- `StatefulGameServer.Benchmarks` — performance benchmarks
- Test game logic through repositories, not controllers
