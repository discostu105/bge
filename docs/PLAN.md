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
| 2.3 | Resource income per round | `ResourceGrowthSco` tick module. Current formula is a simple stub (workers x 1.2), not the full spec formula. |
| 3.1-3.2 | Building construction | `BuildAssetCommand` with resource cost, build time countdown, prerequisites. Terran buildings defined. |
| 3.3 | Tech tree prerequisites | Prerequisite chain enforced before building. |
| 4.1 | Instant unit training | `BuildUnitCommand` — costs resources, requires building, no build time. |
| 4.2 | Unit groups & positions | Units tracked as (type, count, position). Position = home or attacking player X. |
| 4.2 | Merge / split unit groups | `MergeUnitsCommand`, `SplitUnitCommand`, `MergeAllUnitsCommand`. |
| 6.2 | Deploying troops | `SendUnitCommand` moves units to attack position. |
| 6.3 | 8-iteration battle resolution | `BattleBehaviorScoOriginal` — attacker strikes, defender counter-strikes, lowest-HP-first targeting. |
| 12.1 | Player ranking by land | `ScoreRepository` + `PlayerRanking` page. |
| 14.1 | User accounts / auth | Discord OAuth2 + dev login mode. Player creation with race choice. |
| 14.2 | Core player state | Land, minerals, gas, race, tick counters all tracked. |
| 15 | UI pages | Index, Base, Units, UnitDefinition, EnemyBase, SelectEnemy, PlayerRanking, PlayerProfile, CreatePlayer. |
| 15.1 | Persistent header | `_PlayerResources.razor` shows minerals, gas, land. |
| 16 | Data model | In-memory world state (JSON-serialized to blob storage). Matches spec structure. |

---

## Planned — Fits the Architecture

These features are specified, not yet implemented, and slot cleanly into the existing engine. Ordered roughly by priority (foundational mechanics first, social features later).

### Phase 1: Complete the Core Economic Loop

| Spec Section | Feature | What to Build |
|---|---|---|
| 2.2 | Worker roles (mineral / gas / idle) | Add `MineralWorkers` and `GasWorkers` fields to player state. Build a worker-assignment command. Workers are already units; this adds role tracking. |
| 2.3 | Full resource income formula | Replace the `ResourceGrowthSco` stub with the real formula: efficiency = land / (workers * factor), clamped. This is the heart of the economic tension. |
| 2.5 | Emergency respawn | If a player has 0 workers and low resources, auto-grant 1 mineral + 1 gas worker. Add to tick processing. |
| 6.4 | Land transfer on victory | After `BattleBehaviorScoOriginal` resolves, calculate percent from remaining attacker damage, transfer land. Also capture/destroy defender workers. |
| 4.1 | Static defense structures | Missile Turret / Sunken Colony / Photon Cannon: units with `is_mobile = false`, 0 attack, participate in defense only. The unit framework supports this; just needs a flag and combat filter. |
| 4.1 | Troop return timer | `UnitReturn` tick module exists as a stub. Implement: after battle, surviving attackers get `return_timer = speed`, decremented each tick, unavailable for defense until home. |
| 1.2 | Complete Zerg & Protoss definitions | `GameDefinition.SCO` has Terran fully defined. Port the spec tables for Zerg and Protoss buildings + units. Purely data work. |

### Phase 2: Upgrades & Colonization

| Spec Section | Feature | What to Build |
|---|---|---|
| 5.1-5.3 | Attack & defense upgrades | Add upgrade level + timer to player state. New `ResearchUpgradeCommand`. New tick module to decrement timer and apply level. Per-unit bonus table (a1-a3, d1-d3) added to unit definitions. Modify combat to add bonuses. |
| 7 | Colonization | New `ColonizeCommand`: spend minerals to gain land. Formula: `cost = max(1, land / 4)` per land, max 24. Simple command + API endpoint. |
| 6.1 | Attack restrictions | Enforce: no attacking protected players, no attacking allies, no attacking players < 50% your land. Partially implied by `AttackablePlayers` query — needs full enforcement. |
| 8 | New player protection | Add `Protected` counter to player state (start at ~32). Tick module decrements. Filter from attackable list. Show in ranking. |

### Phase 3: Social & Communication

| Spec Section | Feature | What to Build |
|---|---|---|
| 10.1 | Private messages | New `Message` entity (sender, recipient, subject, body, read flag). Battle reports delivered as messages. API + Blazor page. |
| 6.5 | Battle reports | Generate structured report from `BattleResult` and deliver via messaging system. Both sides get a report. |
| 9.1-9.3 | Alliance system | New `Alliance` entity. Join/leave/create commands. Password-based join. Voting for leader. Leader can manage members. Alliance ranking formula. Mutual attack restriction. |
| 9.3 | Alliance view (shared stats) | Opt-in detailed stat sharing within alliance. Read-only endpoint for allies. |

### Phase 4: Automation & Polish

| Spec Section | Feature | What to Build |
|---|---|---|
| 11 | Build queue (todo system) | `GameActionQueue` already exists in the world state. Extend to support queued unit training + priority ordering. Auto-execute when resources and prerequisites are met during tick. |
| 2.4 | Resource trading | New `TradeResourceCommand`: 2:1 exchange rate between minerals and gas. Simple command. |
| 15.1 | Full UI header | Add: time until next round, unread message count, server time. Currently shows resources + land only. |
| 12.2 | Alliance ranking | Rank by `avg_land + total_land / 12`. New API endpoint + Blazor page. |
| 12.3 | Online status | Track `last_online` timestamp. Show online/offline in ranking (threshold ~8 minutes). |

---

## Deferred — Omit for Now

These features from the spec or era are intentionally excluded. Reasons given for each.

| Feature | Reason |
|---|---|
| Real-time chat (public + alliance) | Adds significant complexity (WebSockets/SignalR). Discord serves this purpose externally. Revisit if player demand warrants it. |
| Forum / MBBS integration | The spec notes this should be dropped. Use external community tools. |
| Photo album, calendar | Unrelated to the game. Omit permanently. |
| Multi-account detection | Complex anti-cheat. Not needed for a small player base. Add if abuse becomes a problem. |
| Admin panel (round management) | The tick engine is automatic. Admin tools can be built ad-hoc as CLI or direct state manipulation. |
| Email-based registration | Discord OAuth is simpler and prevents spam. No email infrastructure needed. |
| Spatial map | The original had no spatial map — players interact directly. Keep this design; don't add a map. |

---

## Architecture Notes

**What doesn't need to change:**
- The immutable/mutable state separation handles all new entities cleanly.
- The repository pattern (read/write split) extends naturally to messages, alliances, upgrades.
- The tick module system is perfect for upgrades, protection timers, and unit returns.
- The command pattern accommodates every new player action.
- JSON blob persistence works fine at this scale.

**What may need adjustment:**
- **Worker roles** require a new dimension on player state (not just total worker unit count, but assignment to mineral/gas/idle). Could be tracked as player-level fields rather than unit-level, since reassignment is free.
- **Upgrade bonuses in combat** require `BattleBehaviorScoOriginal` to look up player upgrade levels and per-unit bonus tables. The combat module currently has no concept of upgrades.
- **Messages** are the first entity not tied to a single player's state. They span two players. The world state model may need a top-level messages collection or per-player inbox.
- **Alliances** are a shared entity referenced by multiple players. Similar to messages — needs a top-level collection in world state.

---

## Suggested Build Order

```
Phase 1 (core loop)     ──→  Phase 2 (depth)     ──→  Phase 3 (social)     ──→  Phase 4 (polish)

Worker roles                  Upgrades                  Messages                  Build queue
Full income formula           Colonization              Battle reports            Resource trading
Emergency respawn             Attack restrictions        Alliances                 Full UI header
Land transfer on victory      New player protection      Alliance view             Alliance ranking
Static defenses                                                                   Online status
Troop return timer
Zerg & Protoss data
```

Each phase builds on the previous. Phase 1 completes the economic game loop so the game is actually playable end-to-end. Phase 2 adds strategic depth. Phase 3 makes it social. Phase 4 is quality-of-life.
