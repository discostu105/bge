# StarCraft Online (SCO) — Game Logic Reference

This document is a reverse-engineered reference of the complete gameplay logic for the
**StarCraft Online (SCO)** game variant hosted by the Browser Game Engine (BGE).
It is derived from source inspection and is intended as a design/mechanics reference.
File paths throughout are relative to `src/` unless otherwise noted.

- Game definition entry point: `BrowserGameEngine.GameDefinition.SCO/StarcraftOnlineGameDefFactory.cs`
- World state factory: `BrowserGameEngine.GameDefinition.SCO/StarcraftOnlineWorldStateFactory.cs`
- Engine: `BrowserGameEngine.StatefulGameServer/`

---

## 1. Core Concepts

| Concept | Value |
|---|---|
| Tick duration | **30 seconds** of wall-clock time |
| Factions | `terran`, `protoss`, `zerg` |
| Resources | `land` (score), `minerals`, `gas` |
| Starting land | 50 |
| Starting minerals | 5,000 |
| Starting gas | 3,000 |
| New-player protection | 480 ticks (~4 hours) |
| Victory condition | **Economic Threshold**: first player to reach 500,000 `land` |

A **tick** is the atomic unit of simulation. Every behavior described below runs as a
tick module. Default tick length is 30 s, configured in the game definition factory.

Sources: `StarcraftOnlineGameDefFactory.cs`, `GameModel/GameSettings.cs`.

---

## 2. Tick Pipeline

Each `GameInstance` owns a `GameTickEngine` running the following modules in order
every tick (from `StatefulGameServer/GameTicks/Modules/` and
`GameServerExtensions` registration):

1. **ActionQueue** — resolve scheduled game actions (e.g. build completions).
2. **ResourceGrowthSco** — compute mineral/gas income from workers (see §5).
3. **ResourceHistory** — push a snapshot into each player's resource history log.
4. **NewPlayerProtection** — decrement `ProtectionTicksRemaining`.
5. **UpgradeTimer** — tick attack/defense upgrade research (see §8).
6. **BuildQueue** — pop and execute the next affordable queue entry (see §6).
7. **VictoryCondition** — check for economic threshold win (see §10).
8. **GameFinalization** — finalize and archive game when a winner is decided.

All ticks are deterministic. The only non-determinism is the tie-breaking ordering
applied during combat damage distribution.

---

## 3. Player State

Per-player state (`StatefulGameServer/GameModelInternal/PlayerState.cs`) is the source
of truth for an individual player within a game:

| Field | Description |
|---|---|
| `Resources` | `Dict<ResourceDefId, decimal>` — current `land`/`minerals`/`gas` |
| `Assets` | Buildings owned (at most one per type) |
| `Units` | Unit groups, each with `Position` (home or enemy id) and `ReturnTimer` |
| `MineralWorkers`, `GasWorkers` | Worker assignment (subset of `wbf` count) |
| `ProtectionTicksRemaining` | Newbie shield countdown |
| `AttackUpgradeLevel`, `DefenseUpgradeLevel` | 0–3 |
| `UpgradeBeingResearched`, `UpgradeResearchTimer` | Current upgrade job |
| `BuildQueue` | Priority-ordered build queue |
| `Messages`, `Notifications`, `BattleReports`, `ResourceHistory` | Logs & inbox |
| `TutorialCompleted` | UX flag |

There is **no map**: "position" is just the id of the player the units are visiting,
or `null` for "home". Players are not tiles; they are nodes in an abstract mesh where
anyone can send units at anyone.

---

## 4. Factions, Buildings, Units

SCO ships three playable factions. Every faction has its own distinct building
prerequisite chain and unit roster, but the underlying stat model is identical.

### 4.1 Buildings (Assets)

Buildings are one-per-player; attempting to build a duplicate throws
`AssetAlreadyBuiltException`. Each building has a tick-based build time, a resource
cost, HP (used only for certain defense structures that behave like stationary units),
and a prerequisite list.

#### Terran

| Building | Cost | HP | Build (ticks) | Prereq |
|---|---|---|---|---|
| Kommandozentrale (`commandcenter`) | 400 M | 1200 | 30 | — |
| Kaserne (`barracks`) | 150 M | 1000 | 10 | commandcenter |
| Fabrik (`factory`) | 200 M + 100 G | 1250 | 40 | barracks |
| Waffenfabrik (`armory`) | 100 M + 50 G | 750 | 30 | factory |
| Raumhafen (`spaceport`) | 150 M + 100 G | 1300 | 40 | factory |
| Akademie (`academy`) | 150 M | 600 | 30 | barracks |
| Wissenschaftliches Institut (`sciencefacility`) | 100 M + 150 G | 850 | 50 | spaceport |
| Raketenturm (`missileturret`, defense unit) | 100 M | 135 | — | commandcenter + academy |

#### Zerg

| Building | Cost | HP | Build (ticks) | Prereq |
|---|---|---|---|---|
| Hive (`hive`) | 400 M | 1500 | 40 | — |
| Spawning Pool (`spawningpool`) | 150 M | 750 | 15 | hive |
| Evolution Chamber (`evolutionchamber`) | 75 M | 500 | 40 | spawningpool |
| Hydralisk Den (`hydraliskden`) | 100 M + 50 G | 850 | 20 | spawningpool |
| Ultralisk Cavern (`ultraliskcavern`) | 150 M + 200 G | 500 | 50 | hydraliskden |
| Spire (`spire`) | 200 M + 150 G | 600 | 40 | spawningpool |
| Greater Spire (`greaterspire`) | 100 M + 150 G | 800 | 60 | spire |
| Queen's Nest (`queensnest`) | 150 M + 100 G | 400 | 30 | spawningpool |
| Defiler Mound (`defilermond`) | 100 M + 100 G | 400 | 45 | ultraliskcavern |
| Sunken Colony / Spore Colony (defense units) | 175 M / 125 M | 180 / 200 | — | evolutionchamber |

#### Protoss

| Building | Cost | HP | Build (ticks) | Prereq |
|---|---|---|---|---|
| Nexus (`nexus`) | 400 M | 1000 | 20 | — |
| Gateway (`gateway`) | 150 M | 500 | 10 | nexus |
| Forge (`forge`) | 200 M | 550 | 15 | gateway |
| Cybernetics Core (`cyberneticscore`) | 200 M | 500 | 18 | gateway |
| Robotics Facility (`roboticsfacility`) | 200 M + 200 G | 500 | 20 | forge |
| Stargate (`stargate`) | 150 M + 150 G | 600 | 30 | roboticsfacility |
| Observatory (`observatory`) | 50 M + 100 G | 250 | 24 | roboticsfacility |
| Templar Archives (`templararchives`) | 150 M + 200 G | 500 | 36 | cyberneticscore |
| Citadel of Adun (`citadelofadun`) | 150 M + 100 G | 450 | 24 | cyberneticscore |
| Fleet Beacon (`fleetbeacon`) | 300 M + 200 G | 500 | 40 | stargate |
| Arbiter Tribunal (`arbitertribunal`) | 200 M + 150 G | 500 | 36 | templararchives |
| Photon Cannon (`photoncannon`, defense unit) | 150 M | 100 HP + 100 shields | — | forge |

### 4.2 Units

Unit training via `BuildUnitCommand` is **instant**: there is no training timer and no
population/supply cap. The only gate is the prerequisite asset(s) and the cost.
Unit identity exists at the *group* level — each group has a single `UnitDefId`, a
count, a `Position`, and a `ReturnTimer`. Groups can be split and merged.

Key per-unit stats: `Attack`, `Defense`, `Hitpoints`, `Speed` (travel/return ticks),
optional Protoss `Shields`, and `AttackBonuses[0..2]` / `DefenseBonuses[0..2]` (bonus
per upgrade level). `Mobile=false` on defense structures pins them to their owner.

#### Terran units

| Unit | Cost | Atk | Def | HP | Spd | A-bonus (L1/2/3) | D-bonus |
|---|---|---|---|---|---|---|---|
| WBF (worker) | 50 M | 0 | 1 | 60 | 8 | 0/0/0 | 0/0/0 |
| Space Marine | 45 M | 2 | 4 | 60 | 7 | 1/2/3 | 1/2/3 |
| Firebat | 50 M + 25 G | 9 | 6 | 50 | 7 | 2/4/6 | 1/2/3 |
| Vulture | 75 M | 8 | 2 | 70 | 5 | 2/4/6 | 1/2/3 |
| Siege Tank | 125 M + 100 G | 10 | 40 | 130 | 9 | 2/4/6 | 3/6/9 |
| Wraith | 200 M + 100 G | 36 | 14 | 230 | 5 | 3/6/9 | 2/4/6 |
| Battlecruiser | 300 M + 300 G | 70 | 45 | 500 | 11 | 5/10/15 | 4/8/12 |
| Missile Turret (static) | 100 M | 0 | 12 | 135 | — | 0 | 0 |

#### Zerg units

| Unit | Cost | Atk | Def | HP | Spd | A-bonus | D-bonus |
|---|---|---|---|---|---|---|---|
| Drone (worker) | 50 M | 0 | 1 | 40 | 8 | 0 | 0 |
| Zergling | 40 M | 3 | 1 | 25 | 6 | 1/1/2 | 1/1/1 |
| Hydralisk | 75 M + 50 G | 15 | 5 | 80 | 7 | 1/2/3 | 1/1/2 |
| Lurker | 100 M + 100 G | 12 | 26 | 195 | 10 | 1/1/2 | 2/4/6 |
| Ultralisk | 250 M + 200 G | 45 | 30 | 450 | 10 | 2/4/6 | 2/4/6 |
| Mutalisk | 200 M + 25 G | 20 | 26 | 120 | 7 | 1/2/3 | 2/4/6 |
| Guardian | 100 M + 200 G | 50 | 35 | 200 | 12 | 4/8/12 | 2/4/6 |
| Devourer | 75 M + 225 G | 30 | 30 | 295 | 9 | 2/4/6 | 2/4/6 |
| Queen | 100 M + 100 G | 8 | 10 | 120 | 6 | 1/2/3 | 1/2/3 |
| Scourge | 25 M + 75 G | 40 | 0 | 25 | 4 | 0 | 0 |
| Defiler | 50 M + 150 G | 5 | 15 | 80 | 8 | 0 | 1/2/3 |
| Sunken Colony (static) | 175 M | 0 | 24 | 180 | — | 0 | 2/4/6 |
| Spore Colony (static) | 125 M | 0 | 18 | 200 | — | 0 | 2/4/6 |

#### Protoss units

Protoss additionally have **shields** — a second HP pool used in combat.

| Unit | Cost | Atk | Def | HP | Shields | Spd | A-bonus | D-bonus |
|---|---|---|---|---|---|---|---|---|
| Probe (worker) | 50 M | 0 | 1 | 20 | 20 | 8 | 0 | 0 |
| Zealot | 125 M | 5 | 6 | 80 | 60 | 7 | 1/2/3 | 1/2/3 |
| Dragoon | 150 M + 50 G | 12 | 18 | 100 | 80 | 7 | 1/2/3 | 1/2/3 |
| High Templar | 50 M + 150 G | 6 | 8 | 40 | 40 | 8 | 1/2/3 | 1/2/3 |
| Archon | 120 M + 300 G | 28 | 42 | 10 | 350 | 8 | 2/4/6 | 2/4/6 |
| Dark Templar | 125 M + 100 G | 30 | 30 | 40 | 40 | 6 | 2/4/6 | 2/4/6 |
| Reaver | 275 M + 100 G | 65 | 0 | 100 | 80 | 12 | 4/8/12 | 0 |
| Observer | 25 M + 25 G | 0 | 0 | 20 | 20 | 4 | 0 | 0 |
| Corsair | 150 M + 100 G | 18 | 12 | 100 | 80 | 5 | 2/4/6 | 1/2/3 |
| Scout | 300 M + 150 G | 28 | 49 | 150 | 100 | 6 | 2/4/6 | 4/8/12 |
| Arbiter | 100 M + 350 G | 15 | 20 | 150 | 150 | 8 | 1/2/3 | 2/4/6 |
| Carrier | 550 M + 300 G | 80 | 55 | 300 | 150 | 12 | 4/8/12 | 4/8/12 |
| Photon Cannon (static) | 150 M | 0 | 20 | 100 | 100 | — | 0 | 2/4/6 |

---

## 5. Economy

Source: `GameTicks/Modules/ResourceGrowthSco.cs`.

### 5.1 Workers and efficiency

Workers are the **worker unit** of each faction (`wbf`, `drone`, `probe`), but they
are counted collectively — the total pool is split between *mineral* and *gas*
assignment via `AssignWorkersCommand`. Both per-worker income *and* worker cost
are balanced symmetrically across races.

Each tick, for each resource type:

```
base_income        = 10
per_worker_base    = 4
efficiency_factor  = 0.03 (minerals) | 0.06 (gas)
efficiency         = clamp(land / (workers * efficiency_factor), 0.2, 100.0)
worker_income      = workers * per_worker_base * efficiency / 100
income             = base_income + worker_income
```

Intuitively: **land is the divisor that makes workers diminishing-returns**. The
more workers relative to your land, the lower the efficiency multiplier, floored at
20%. This is the only reason players need land beyond scoring.

### 5.2 Emergency respawn

If a player has zero workers *and* fewer than 50 of either resource, one mineral and
one gas worker are auto-granted to prevent soft-locks.

### 5.3 Storage and base income

There is no storage cap. The `+10`/`+10` base income ensures every player has a
trickle even with no workers assigned.

### 5.4 Land

Land is gained by:

- **Colonizing** via `ColonizeCommand` (spends minerals for land).
- **Winning battles** — see §7.3.

Land also counts as the victory resource.

---

## 6. Build Queue & Construction

Source: `Repositories/Asset/AssetRepositoryWrite.cs`, `BuildQueue/*`.

Commands:

- `AddToQueueCommand(playerId, type, defId, count)` — append entry.
- `RemoveFromQueueCommand` / `ReorderQueueCommand` — manage priorities.
- `BuildAssetCommand` / `BuildUnitCommand` — also used programmatically.

Every tick the **BuildQueue** module pops the highest-priority entry and attempts it:

- Check prerequisites (other buildings must already exist).
- Check affordability; if unaffordable the entry stays in queue.
- On success the cost is deducted and either:
  - units spawn immediately (instant training), or
  - the building is scheduled on the action queue with `dueTick = now + buildTimeTicks`,
    and the ActionQueue module materializes the asset when the tick is reached.

---

## 7. Combat

Source: `Repositories/Battle/BattleBehaviorScoOriginal.cs`,
`BattleReportGenerator.cs`.

Battles are triggered when `SendUnitCommand` dispatches units to an enemy player
id. They resolve **immediately and deterministically** (no travel delay on the
way *out*; the travel cost is paid as a `ReturnTimer` on the way home).

### 7.1 Round structure

Combat runs for at most **8 rounds**. Each round:

1. **Attacker phase.** Sum every attacker unit's effective attack:
   `unit.Attack + (upgradeLevel > 0 ? unit.AttackBonuses[level-1] : 0)`
   multiplied by group count. This is distributed as damage to defenders.
2. **Defender phase.** Same calculation with defense stats, dealt to attackers.
3. **Damage distribution.** Target units are ordered by
   `(total HP asc, defense asc, unit def id asc)` and consumed greedily:
   - if attackPoints ≥ group total HP, the group is annihilated and attackPoints
     is reduced by that HP.
   - Otherwise `survivors = floor(remainingHP / unitHP)`, any leftover HP
     creates one partially damaged unit, and attackPoints reaches zero.

Protoss units' shield pool absorbs damage before HP, per group.

### 7.2 Outcome

- Attacker wins: defender wiped out, attacker still has survivors.
- Defender wins: symmetric.
- Draw: both sides wiped out.

### 7.3 Spoils of war (on attacker victory)

- **Land transfer.** `percent = clamp(remainingAttackDamage / defenderLand * 12, 1%, 50%)`.
  Defender loses `defenderLand * percent`, attacker gains the same amount.
- **Workers captured.** `workersLost = totalWorkers * percent / 2` (i.e. half the
  land-percentage).
- **Pillage.** 10% of each of the defender's resources, capped at 5,000 per resource.
- **Survivors return.** Each attacking unit group gets `ReturnTimer = unit.Speed`;
  the `UnitState` module decrements the timer until the group is "home" again (Position null).

### 7.4 New player protection

While `ProtectionTicksRemaining > 0` a player cannot be targeted — the
`AttackIneligibilityReason` check refuses the command (see
`NewPlayerProtectionModule.cs`).

### 7.5 Battle reports

Every resolved battle produces a `BattleReport` for both players, stored on
`PlayerState.BattleReports` and echoed as notifications/messages. Reports contain:

- initial forces on both sides,
- per-round casualty/surviving snapshots,
- final land transferred, workers captured, resources stolen,
- attacker/defender identity and race strings.

---

## 8. Upgrades

Source: `Repositories/Upgrades/UpgradeRepositoryWrite.cs`.

Two upgrade tracks exist: **Attack** and **Defense**, each with 3 levels. Only one
upgrade (of either type) may be in progress at a time.

| Level | Cost | Research time |
|---|---|---|
| 1 | 150 M + 100 G | 10 ticks |
| 2 | 250 M + 150 G | 10 ticks |
| 3 | 400 M + 200 G | 10 ticks |

The level number is used to index the per-unit `AttackBonuses` / `DefenseBonuses`
arrays at combat time (see §7.1). Bonuses are therefore per-unit and carefully tuned
per faction — the listing in §4.2 is the full bonus table.

---

## 9. Markets, Trading & Messaging

### 9.1 Global market

- `CreateMarketOrderCommand` — offer N of resource A for M of resource B.
- `AcceptMarketOrderCommand` — any player can fulfill an open order.
- `CancelMarketOrderCommand` — retract your own order.

### 9.2 Direct trade offers

- `CreateTradeOfferCommand` — directed at a specific recipient, with a note.
- `AcceptTradeOfferCommand` / `DeclineTradeOfferCommand` / `CancelTradeOfferCommand`.

### 9.3 Messaging & chat

- `SendMessageCommand` — private in-game mail.
- `MarkMessageReadCommand`.
- `PostChatMessageCommand` — global chat.
- `PostAllianceChatCommand` — alliance-only chat.

---

## 10. Victory, Game Lifecycle & Finalization

Source: `GameTicks/Modules/VictoryConditionModule.cs`, `GameFinalization` module.

- Only the **Economic Threshold** condition is implemented. Threshold defaults to
  500,000 land.
- Each tick, VictoryCondition checks every player's land. If any meet the threshold,
  `GameLifecycleEngine.FinalizeGameEarlyAsync()` is invoked, which triggers the
  GameFinalization module next tick to archive the instance and compute standings.
- Finalization archives per-game state and computes an end-of-game ranking of the
  players involved. There is no cross-game leaderboard or XP/achievement system.

---

## 11. Alliances, Elections & Wars

Source: `GameModelInternal/Alliance.cs`.

### 11.1 Structure

- Each alliance has a leader, members (possibly pending approval), a password,
  an MOTD, and a message-board style post feed.
- Membership commands: `CreateAllianceCommand`, `JoinAllianceCommand`,
  `InvitePlayerToAllianceCommand`, `Accept/DeclineAllianceInviteCommand`,
  `LeaveAllianceCommand`, `AcceptMemberCommand`, `RejectMemberCommand`,
  `KickMemberCommand`, `SetAlliancePasswordCommand`, `SetAllianceMessageCommand`.

### 11.2 Elections

Alliances run timed elections for leadership:

- `StartElectionCommand(nominationDuration, votingDuration)` opens a contest.
- `NominateForElectionCommand` / `WithdrawNominationCommand` — self-nominate.
- `CastElectionVoteCommand` — one vote per member.
- `CancelElectionCommand` — abort in progress.

### 11.3 Alliance war

- `DeclareAllianceWarCommand` opens a war between alliances.
- `ProposeAlliancePeaceCommand` / `AcceptAlliancePeaceCommand` end it.

The war record itself is metadata; combat still happens player-vs-player through
§7, but belonging to a warring alliance is what surfaces the opponent in the UI.

---

## 12. Command Catalogue

Every player action is a record in `StatefulGameServer/Commands/Commands.cs`.
Grouped summary:

- **Build & military.** `BuildAssetCommand`, `BuildUnitCommand`, `MergeUnitsCommand`,
  `SplitUnitCommand`, `SendUnitCommand`, `ReturnUnitsHomeCommand`, `MergeAllUnitsCommand`.
- **Economy.** `HarvestResourceCommand`, `AssignWorkersCommand`, `ColonizeCommand`,
  `TradeResourceCommand`.
- **Research.** `ResearchUpgradeCommand`.
- **Markets & trade.** `CreateMarketOrderCommand`, `AcceptMarketOrderCommand`,
  `CancelMarketOrderCommand`, `CreateTradeOfferCommand`, `AcceptTradeOfferCommand`,
  `DeclineTradeOfferCommand`, `CancelTradeOfferCommand`.
- **Build queue.** `AddToQueueCommand`, `RemoveFromQueueCommand`, `ReorderQueueCommand`.
- **Messaging & chat.** `SendMessageCommand`, `MarkMessageReadCommand`,
  `PostChatMessageCommand`, `PostAllianceChatCommand`.
- **Alliance.** `CreateAllianceCommand`, `JoinAllianceCommand`,
  `InvitePlayerToAllianceCommand`, `AcceptAllianceInviteCommand`,
  `DeclineAllianceInviteCommand`, `LeaveAllianceCommand`, `AcceptMemberCommand`,
  `RejectMemberCommand`, `KickMemberCommand`, `SetAlliancePasswordCommand`,
  `SetAllianceMessageCommand`.
- **Elections.** `StartElectionCommand`, `NominateForElectionCommand`,
  `WithdrawNominationCommand`, `CastElectionVoteCommand`, `CancelElectionCommand`.
- **Alliance war.** `DeclareAllianceWarCommand`, `ProposeAlliancePeaceCommand`,
  `AcceptAlliancePeaceCommand`.
- **Profile.** `ChangePlayerNameCommand`.

---

## 13. Key Constants (cheat sheet)

| Constant | Value | Source |
|---|---|---|
| Tick duration | 30 s | `StarcraftOnlineGameDefFactory.cs` |
| Base mineral / gas income per tick | 10 / 10 | `ResourceGrowthSco.cs` |
| Per-worker base rate | 4 | `ResourceGrowthSco.cs` |
| Mineral / gas efficiency factor | 0.03 / 0.06 | `ResourceGrowthSco.cs` |
| Efficiency clamp | 0.2 – 100.0 | `ResourceGrowthSco.cs` |
| Emergency respawn threshold | 50 | `ResourceGrowthSco.cs` |
| Max battle rounds | 8 | `BattleBehaviorScoOriginal.cs` |
| Land transfer formula | `clamp(dmg / defenderLand * 12, 1%, 50%)` | battle behavior |
| Workers captured | `land% / 2` of defender workers | battle behavior |
| Pillage | 10% per resource, cap 5,000 | battle behavior |
| Upgrade levels | 3 (Attack / Defense each) | `UpgradeRepositoryWrite.cs` |
| Upgrade research time | 10 ticks per level | `UpgradeRepositoryWrite.cs` |
| Upgrade costs L1/2/3 | 150+100, 250+150, 400+200 | `UpgradeRepositoryWrite.cs` |
| New player protection | 480 ticks | `GameSettings.cs` |
| Victory threshold (default) | 500,000 land | `VictoryConditionModule.cs` |

---

## 14. File Index

| Area | File |
|---|---|
| Game definition (factions/buildings/units) | `BrowserGameEngine.GameDefinition.SCO/StarcraftOnlineGameDefFactory.cs` |
| Initial world state | `BrowserGameEngine.GameDefinition.SCO/StarcraftOnlineWorldStateFactory.cs` |
| Tick modules | `BrowserGameEngine.StatefulGameServer/GameTicks/Modules/` |
| Commands | `BrowserGameEngine.StatefulGameServer/Commands/Commands.cs` |
| Battle behavior | `BrowserGameEngine.StatefulGameServer/Repositories/Battle/BattleBehaviorScoOriginal.cs` |
| Battle report generator | `BrowserGameEngine.StatefulGameServer/BattleReportGenerator.cs` |
| Resource growth | `BrowserGameEngine.StatefulGameServer/GameTicks/Modules/ResourceGrowthSco.cs` |
| Upgrades | `BrowserGameEngine.StatefulGameServer/Repositories/Upgrades/UpgradeRepositoryWrite.cs` |
| Asset building | `BrowserGameEngine.StatefulGameServer/Repositories/Asset/AssetRepositoryWrite.cs` |
| Build queue | `BrowserGameEngine.StatefulGameServer/Repositories/BuildQueue/BuildQueueRepositoryWrite.cs` |
| Market | `BrowserGameEngine.StatefulGameServer/Repositories/Market/MarketRepository.cs` |
| Alliance data | `BrowserGameEngine.StatefulGameServer/GameModelInternal/Alliance.cs` |
| Player state | `BrowserGameEngine.StatefulGameServer/GameModelInternal/PlayerState.cs` |
| Unit state | `BrowserGameEngine.StatefulGameServer/GameModelInternal/UnitState.cs` |
| Battle report types | `BrowserGameEngine.StatefulGameServer/GameModelInternal/BattleReport.cs` |
| Game settings | `BrowserGameEngine.GameModel/GameSettings.cs` |
| Cost/resource primitives | `BrowserGameEngine.GameDefinition/Cost.cs`, `UnitDef.cs`, `AssetDef.cs` |
