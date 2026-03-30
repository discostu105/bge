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
| 15 | UI pages | Index, Base, Units, UnitDefinition, EnemyBase, SelectEnemy, PlayerRanking, CreatePlayer, Upgrades, Alliances, AllianceDetail, AllianceRanking, Messages, Players, GameLobby, PlayerHistory, Summary, Results, PublicProfile. |
| 15.1 | Full UI header | `_PlayerResources.razor`: minerals, gas, land, server time, next-tick countdown, unread message badge. `MainLayout`: game name, status, time-to-end banner, ending-soon and finished alerts. |
| 16 | Data model | In-memory world state (JSON-serialized to blob storage). Matches spec structure. |
| — | Multi-game platform | `GameRegistry`, `GameInstance`, `GlobalState`, `GameLifecycleEngine`. Multiple concurrent game rounds. `Upcoming → Active → Finished` lifecycle. |
| — | Game auto-finalization | `GameLifecycleEngine` transitions `Active → Finished` on `EndTime`, writes `PlayerAchievement` records. `GameLifecycleService` polls every 60s. |
| — | Game list & season schedule | `/games` — Season Schedule view with Bootstrap cards (upcoming, active, finished). (BGE-217) |
| — | Game lobby & join flow | `GameLobby.razor` at `/games/{gameId}` — player name + race selection, `POST /api/games/{gameId}/players`. (BGE-136) |
| — | Game-scoped routing | All game-action pages use `/games/{gameId}/...` routes. (BGE-162) |
| — | Game context in header | `MainLayout` game-context-banner: game name, status, time-to-end. End-of-game countdown banner. (BGE-212) |
| — | Post-game summary | `/games/{id}/summary` — per-player final stats and score. (BGE-224) |
| — | Game results screen | `/games/{id}/results` — final standings with auto-redirect. (BGE-165) |
| — | Cross-game leaderboard | All-time rankings via `PlayerAchievementImmutable`. `LeaderboardController` + all-time view. (BGE-163) |
| — | Player public profile | `/games/{id}/profile/{playerId}` — public per-player page with cross-game stats. (BGE-213) |
| — | Player game history | `PlayerHistory.razor` — per-player game history with stats. (BGE-192) |
| — | Discord webhook notifications | `DiscordWebhookNotificationService` fires on game start/end via `IGameNotificationService`. (BGE-241) |
| — | In-app notification center | Bell icon in navbar with unread badge. Per-player ring buffer (20 items). `GET/DELETE /api/notifications`. Fires on game start, game end, and incoming attack. (BGE-267) |
| — | Admin cheat endpoints | `AdminController` (DevAuth-gated): set-resources, grant-building, grant-units, reset-player, force-tick, world-state dump, player list. |
| — | State snapshots | `AdminController`: save-snapshot, load-snapshot, list snapshots, delete snapshot. |
| — | Configurable tick speed | `AdminController`: set-tick-duration, pause-ticks, resume-ticks. |
| — | Action feed / log | `IActionLogger` ring buffer in `StatefulGameServer`. `GET /api/admin/action-log`. |
| — | 10s auto-refresh | All game-state pages poll every 10s via `PeriodicTimer`. (BGE-189) |
| — | Bot client support | `BearerTokenMiddleware` + API key auth. Bot quickstart guide in `docs/BOT-CLIENT.md`. |

---

## Planned — Fits the Architecture

These features are specified, not yet implemented, or have PRs pending merge.

### Core Game Completeness

| Spec Section | Feature | What to Build |
|---|---|---|
| 1.2 | Complete Zerg & Protoss definitions | `GameDefinition.SCO` has Terran fully defined. Port the spec tables for Zerg and Protoss buildings + units. Purely data work. |

### Multi-Game Registration & Season Flow

| Feature | What to Build | Status |
|---|---|---|
| Player registration UI | Dedicated registration page with name/race form. (BGE-157) | PR #64 open, pending merge |
| Join game UI improvements | Polished join-game flow with validation and error states. (BGE-158) | PR #60 open, pending merge |
| Season subscription + auto-join | Users subscribe to a season; auto-enrolled in the next game start. (BGE-269) | PR #85 open, pending merge |

### Social & Communication Polish

| Spec Section | Feature | What to Build |
|---|---|---|
| 9.1 | Alliance leader election | Voting mechanism for alliance leader. Leader management of members. Currently leader = creator. |

### Developer Tooling

| Feature | What to Build |
|---|---|
| Balance simulation CLI | Standalone console project (`tools/BalanceSim`) — run combat simulations without the server. Critical before completing Zerg/Protoss data. |

---

## Deferred — Omit for Now

These features from the spec or era are intentionally excluded.

| Feature | Reason |
|---|---|
| Real-time chat (public + alliance) | Adds significant complexity (WebSockets/SignalR). Discord serves this purpose externally. Revisit if player demand warrants it. |
| Forum / MBBS integration | The spec notes this should be dropped. Use external community tools. |
| Photo album, calendar | Unrelated to the game. Omit permanently. |
| Multi-account detection | Complex anti-cheat. Not needed for a small player base. Add if abuse becomes a problem. |
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
- **Game-scoped URL routing** (`/games/{gameId}/...`) is the implemented final shape for all game-action pages and APIs.
- **Multiple tick engines** — one per active `GameInstance`. The timer service iterates all active instances.

---

## Suggested Build Order

```
Merge BGE-157/158 PRs            (player registration + join UI)
Merge BGE-269 PR                 (season subscription + auto-join)
Balance simulation CLI           (before Zerg/Protoss data)
Zerg & Protoss data              (complete core game)
Alliance leader election         (social completeness)
```
