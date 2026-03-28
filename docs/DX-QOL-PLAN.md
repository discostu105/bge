# Developer Experience & QOL Plan

Tooling and convenience features to make developing BGE faster. Ordered by impact.

---

## 1. Admin Cheat Endpoints

**What:** An `AdminController` gated behind `DevAuth` that wraps existing repository write operations, skipping affordability checks.

**Endpoints:**

| Endpoint | Purpose |
|---|---|
| `POST /api/admin/set-resources` | Set minerals/gas/land to arbitrary values |
| `POST /api/admin/grant-building` | Instantly complete a building (skip build time) |
| `POST /api/admin/grant-units` | Add units without cost |
| `POST /api/admin/reset-player` | Wipe player back to starter state |
| `POST /api/admin/force-tick?count=N` | Advance the world N ticks and process all players |
| `GET /api/admin/world-state` | Dump full world state JSON (already serialized for persistence) |

**Why:** Every test scenario currently requires playing through the game loop manually or restarting the app. These endpoints let you set up any game state in seconds via curl or Postman.

**Effort:** Low. All write operations exist in repositories already. The controller is a thin wrapper.

**Guard:** Only available when `Bge:DevAuth = true`. Returns 404 in production.

---

## 2. State Snapshots (Save/Load)

**What:** Named snapshots of the full world state, persisted alongside the main game state.

**Endpoints:**

| Endpoint | Purpose |
|---|---|
| `POST /api/admin/save-snapshot?name=x` | Serialize current world state to `snapshots/x.json` |
| `POST /api/admin/load-snapshot?name=x` | Replace world state from snapshot |
| `GET /api/admin/snapshots` | List available snapshots |
| `DELETE /api/admin/snapshot?name=x` | Delete a snapshot |

**Why:** Set up a complex game state once (two players, specific armies, specific upgrade levels), save it, and reload it every time you need to retest. Eliminates the #1 time sink in manual testing.

**Effort:** Low. The persistence layer already serializes/deserializes world state to JSON. This writes to a different path.

---

## 3. Configurable Tick Speed at Runtime

**What:** Admin endpoints to change tick duration, pause, and resume without restarting.

**Endpoints:**

| Endpoint | Purpose |
|---|---|
| `POST /api/admin/set-tick-duration?seconds=N` | Change tick interval (e.g. 5s for fast dev, 300s for slow observation) |
| `POST /api/admin/pause-ticks` | Freeze world time |
| `POST /api/admin/resume-ticks` | Unfreeze |

**Why:** The 30s tick is fine for gameplay but awkward for development. When building the resource income formula, you want 2s ticks. When debugging a battle, you want ticks paused. Currently tick duration is baked into `GameDef` at startup.

**Effort:** Low. Make `TickDuration` mutable (or add an override field). The `GameTickTimerService` already checks `IsTickDue()` each cycle — it respects whatever duration is set.

---

## 4. Balance Simulation CLI

**What:** A standalone console project (`tools/BalanceSim`) that runs combat simulations without starting the game server.

**Usage:**
```
dotnet run --project tools/BalanceSim -- \
  --attacker "50 Marine, 10 SiegeTank" \
  --defender "30 Zealot, 5 Dragoon, 3 PhotonCannon" \
  --attacker-upgrades atk=2,def=1 \
  --defender-upgrades atk=1,def=3
```

**Output:**
```
ATTACKER WINS
  Attacker losses: 38 Marine, 4 SiegeTank
  Defender losses: 30 Zealot, 5 Dragoon, 3 PhotonCannon (wiped)
  Remaining attacker damage: 412
  Land transfer: ~8%
```

**Why:** Critical before adding Zerg and Protoss. The combat system is deterministic and pure — `BattleBehaviorScoOriginal` has no side effects. Simulating thousands of matchups catches balance problems that manual playtesting never will.

**Effort:** Medium. Wire up `BattleBehaviorScoOriginal` with `GameDef` data, parse CLI args, format output. Could also support batch mode (CSV of matchups in, results out).

---

## 5. Multi-Player Dev Login Page

**What:** Replace the plain dev login text field with a page showing all existing players as clickable buttons, plus a "create new player" form.

**Why:** During multiplayer testing you switch players constantly (open tab as player A, incognito as player B). Seeing who exists and clicking to log in is faster than remembering player IDs.

**Effort:** Low. One Blazor component. Query `GET /api/admin/world-state` or a lighter `GET /api/admin/players` endpoint for the player list.

---

## 6. Bot Players

**What:** An `IBotStrategy` interface with a simple implementation that runs each tick per bot player.

**Behavior:**
1. Build workers if few exist
2. Walk up the tech tree (build next prerequisite)
3. Train affordable units
4. Attack the weakest non-allied, non-protected player when army is large enough
5. Colonize when idle

**Why three things at once:**
- **Populate the world** — a ranking with 1 real player is meaningless. 15 bots make the game feel alive.
- **Soak testing** — let 20 bots run for a few hours. Find deadlocks, resource leaks, dead-end states, and edge cases no manual test will hit.
- **Balance feedback** — if one race's bots consistently dominate, the balance is off.

**Effort:** Medium-high. The bot doesn't need to be smart — random-with-priorities is fine. The effort is in wiring it into the tick loop and making it configurable (how many bots, which races).

**Guard:** Bots are identifiable (name prefix or flag) so they can be excluded from real rankings if desired.

---

## 7. Action Feed

**What:** An in-memory ring buffer (last ~500 entries) of game actions, exposed at `GET /api/admin/action-log`.

**Format:**
```
12:03:15  player-1  BuildAsset      Barracks (150M)
12:03:18  player-1  BuildUnit       Marine x10 (450M)
12:03:22  player-2  SendUnits       → player-1 (25 Zealot)
12:03:22  player-2  Attack          → player-1: WIN, +12 land
12:03:30  [tick]    ResourceGrowth  player-1: +48M, +32G
```

**Why:** When something breaks, the first question is "what happened?" Serilog captures everything but is noisy and hard to scan. A structured game-action feed answers "what sequence of player actions and tick events led to this state?" in seconds.

**Effort:** Low. Add an `IActionLogger` that commands and tick modules call with a one-liner. Store in a `ConcurrentQueue` with a size cap.

---

## Not Recommended

| Idea | Why Skip |
|---|---|
| Admin panel UI | High maintenance, must be updated alongside every feature. Admin endpoints + curl/Postman cover the same need with zero UI cost. Build a UI only if non-developers need admin access. |
| Player impersonation | Dev-login already lets you log in as any player. Use two browser windows (one incognito) for multiplayer testing. A separate impersonation system adds auth complexity for no gain. |
| Feature flags | Single deployment, single developer. Just push code. Feature flags are for teams shipping to large user bases. |
| E2E browser tests | Flaky, slow, high maintenance. Unit tests on game logic (which already exist) are far more valuable per effort. |
| Replay system | Large effort for a debugging tool. State snapshots solve the same "reproduce this scenario" need. |
| Analytics / telemetry | Prometheus metrics already exist. Detailed player analytics are premature until there are real players to analyze. |
| GraphQL | REST is fine at this scale. GraphQL adds tooling overhead and a learning curve for zero benefit with a single frontend consumer. |

---

## Build Order

```
1. Admin cheat endpoints ─── unlocks everything else
2. State snapshots ───────── reproducible test scenarios
3. Tick speed control ────── smooth dev loop
4. Balance simulation CLI ── critical before Zerg/Protoss data
5. Dev login page ────────── small convenience
6. Bot players ───────────── soak testing, world population
7. Action feed ───────────── debugging aid
```

Items 1-3 are foundational and should be built before starting on the game feature phases in [PLAN.md](PLAN.md). Item 4 should be done before completing Zerg & Protoss definitions. Items 5-7 are nice-to-haves at any point.
