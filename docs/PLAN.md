# BGE Implementation Plan

Cross-reference of the [original 2003 SCO specification](REVERSE-ENGINEERED-SPECIFICATION-2003.md) against the current BGE codebase. Categorizes every spec feature as **done**, **planned** (fits the architecture), or **deferred** (omit for now).

---

## Already Implemented

These features exist and are functional in the current codebase.

| Spec Section | Feature | Notes |
|---|---|---|
| 1.1 | Tick-based game world | Per-player tick engine with pluggable modules. 30s ticks (configurable). |
| 1.2 | Race selection at registration | Terran/Zerg/Protoss choice, permanent. Terran fully defined; Zerg & Protoss partially defined. |
| 2.1 | Two resource types (Minerals, Gas) | Tracked per player. Land is the score resource. |
| 2.2 | Worker roles (mineral / gas / idle) | `MineralWorkers` and `GasWorkers` on player state. `AssignWorkersCommand` + `WorkersController`. |
| 2.3 | Resource income per round | `ResourceGrowthSco` tick module. Full efficiency formula implemented: `efficiency = clamp(land / (workers * factor), 0.2, 100)`. |
| 2.4 | Resource trading | `TradeResourceCommand` — 2:1 exchange rate between minerals and gas. |
| 2.5 | Emergency respawn | `ResourceGrowthSco` tick module. If 0 workers and low resources, auto-grants 1 mineral + 1 gas worker. |
| 3.1-3.2 | Building construction | `BuildAssetCommand` with resource cost, build time countdown, prerequisites. Terran buildings defined. |
| 3.3 | Tech tree prerequisites | Prerequisite chain enforced before building. |
| 4.1 | Instant unit training | `BuildUnitCommand` — costs resources, requires building, no build time. |
| 4.1 | Static defense structures | `IsMobile` flag on `UnitDef`; static units participate in defense only. |
| 4.1 | Troop return timer | `UnitReturn` tick module — surviving attackers get a return timer, unavailable for defense until home. |
| 4.2 | Unit groups & positions | Units tracked as (type, count, position). Position = home or attacking player X. |
| 4.2 | Merge / split unit groups | `MergeUnitsCommand`, `SplitUnitCommand`, `MergeAllUnitsCommand`. |
| 5.1-5.3 | Attack & defense upgrades | Upgrade level + timer on player state. `ResearchUpgradeCommand`, `UpgradeTimer` tick module, `UpgradesController`. |
| 6.1 | Attack restrictions | `AttackablePlayers` query enforces: no protected players, no allies, land-ratio restriction. |
| 6.2 | Deploying troops | `SendUnitCommand` moves units to attack position. |
| 6.3 | 8-iteration battle resolution | `BattleBehaviorScoOriginal` — attacker strikes, defender counter-strikes, lowest-HP-first targeting. Upgrade bonuses applied. |
| 6.4 | Land transfer on victory | After battle resolution, land is transferred proportional to remaining attacker damage. Defender workers captured. |
| 6.5 | Battle reports | `BattleReportGenerator` creates structured reports; delivered via messaging system to both sides. |
| 7 | Colonization | `ColonizeCommand`: spend minerals to gain land. Formula: `cost = max(1, land / 4)` per land, max 24. |
| 8 | New player protection | `ProtectionTicksRemaining` on player state (starts at 480 ticks ≈ 4 hours). `NewPlayerProtectionModule` tick module decrements it. Enforced in attackable-player query. |
| 9.1-9.3 | Alliance system | `Alliance` entity. Create/join/leave commands. `AlliancesController`, `AllianceDetail.razor`. Mutual attack restriction enforced. |
| 9.3 | Alliance view (shared stats) | Alliance detail page shows member stats. `AllianceScoreRepository` for ranking. |
| 10.1 | Private messages | `Message` entity (sender, recipient, subject, body, read flag). `MessagesController` + `Messages.razor`. |
| 11 | Build queue (todo system) | `GameActionQueue` in world state. `BuildQueueModule` tick module auto-executes queued builds/trains. `BuildQueueController`. |
| 12.1 | Player ranking by land | `ScoreRepository` + `PlayerRanking.razor`. Filterable by human/agent. |
| 12.2 | Alliance ranking | `AllianceScoreRepository` + `AllianceRankingController` + `AllianceRanking.razor`. |
| 12.3 | Online status | `OnlineStatusRepository` tracks `last_online` timestamp. Online/offline shown in ranking. |
| 14.1 | User accounts / auth | GitHub OAuth2 + dev login mode. Player creation with race choice. |
| 14.2 | Core player state | Land, minerals, gas, race, tick counters all tracked. |
| 15 | UI pages | Index, Base, Units, UnitDefinition, EnemyBase, SelectEnemy, PlayerRanking, PlayerProfile, CreatePlayer, Upgrades, Alliances, AllianceDetail, AllianceRanking, Messages. |
| 15.1 | Persistent header | `_PlayerResources.razor` shows minerals, gas, land. |
| 16 | Data model | In-memory world state (JSON-serialized to blob storage). Matches spec structure. |
| — | Multi-game platform | `GameRegistry`, `GameInstance`, `GlobalState`, `GameLifecycleEngine`. Multiple concurrent game rounds. `Upcoming → Active → Finished` lifecycle. |
| — | Cross-game leaderboard | `PlayerAchievementImmutable` written on game finalization. `LeaderboardController` + `PlayerRanking.razor` all-time view. |

---

## Planned — Fits the Architecture

These features are specified, not yet implemented, and slot cleanly into the existing engine.

### Core Game Completeness

| Spec Section | Feature | What to Build |
|---|---|---|
| 1.2 | Complete Zerg & Protoss definitions | `GameDefinition.SCO` has Terran fully defined. Port the spec tables for Zerg and Protoss buildings + units. Purely data work. |

### Multi-Game UI

| Feature | What to Build |
|---|---|
| Game list page | `GameList.razor` at `/games` — shows upcoming, active, finished games with join links and time-until-start countdowns. |
| Join game flow | `POST /api/games/{gameId}/players` + `JoinGame.razor` — player name + race selection. Validate user doesn't already have a player in this game. |
| Game-scoped routing | Update all existing Blazor pages to accept `GameId` as a route parameter (`/games/{gameId}/base`, etc.) and pass it in all API calls. Add redirect rules from old routes. |
| Game context in header | Update `_PlayerResources.razor` to show game name and time-to-end. |

### Social & Communication Polish

| Spec Section | Feature | What to Build |
|---|---|---|
| 9.1 | Alliance leader election | Voting mechanism for alliance leader. Leader management of members. Currently leader = creator. |
| 15.1 | Full UI header | Add: time until next round, unread message count, server time. Currently shows resources + land only. |

---

## Deferred — Omit for Now

These features from the spec or era are intentionally excluded. Reasons given for each.

| Feature | Reason |
|---|---|
| Real-time chat (public + alliance) | Adds significant complexity (WebSockets/SignalR). Discord serves this purpose externally. Revisit if player demand warrants it. |
| Forum / MBBS integration | The spec notes this should be dropped. Use external community tools. |
| Photo album, calendar | Unrelated to the game. Omit permanently. |
| Multi-account detection | Complex anti-cheat. Not needed for a small player base. Add if abuse becomes a problem. |
| Admin panel (round management) | `AdminController` covers basic needs. Full admin UI can be built ad-hoc if needed. |
| Email-based registration | GitHub OAuth is simpler and prevents spam. No email infrastructure needed. |
| Spatial map | The original had no spatial map — players interact directly. Keep this design; don't add a map. |

---

## Architecture Notes

**What doesn't need to change:**
- The immutable/mutable state separation handles all new entities cleanly.
- The repository pattern (read/write split) extends naturally to any new entity.
- The tick module system is perfect for any time-based automation.
- The command pattern accommodates every new player action.
- JSON blob persistence works fine at this scale.
- `IWorldStateAccessor` means repositories are already game-instance-agnostic.

**What the multi-game platform changes:**
- **Users are global**, not per-game. `GlobalState` owns user accounts; `WorldState` holds only players (game-scoped characters).
- **Joining a game** requires a dedicated flow: `POST /api/games/{gameId}/players` to create a player, separate from account creation.
- **Game-scoped URL routing** (`/games/{gameId}/...`) is the planned final shape for all game-action pages and APIs, but the current routing still uses flat paths as a compatibility bridge.
- **Multiple tick engines** — one per active `GameInstance`. The timer service iterates all active instances.

---

## Suggested Build Order

```
Zerg & Protoss data         (complete core game)
Game list + join flow       (multi-game UI)
Game-scoped routing         (complete URL migration)
Alliance leader election    (social completeness)
Full UI header              (polish)
```
