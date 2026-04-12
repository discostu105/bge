# SCO Simplification Plan

Goal: **keep game logic simple and close to the original StarCraft-Online browser
game**. Strip gamification and overgrown sub-systems that have accreted on top of
the core loop (economy → army → attack → land → score). This document is a design
plan; it is the target state the codebase should converge on, not a description of
current behavior.

Cross-reference: the current-state reference is `docs/SCO_GAME_LOGIC.md`.

---

## 1. Victory Condition — Time-Based Ranking

### Current behavior
- `VictoryConditionModule` ends the game as soon as any player reaches
  `EconomicThreshold` (default 500,000 land).
- `GameLifecycleEngine.FinalizeGameEarlyAsync()` is invoked mid-run.

### Target behavior
- Every game has a fixed **end time** (`Game.EndsAt`, wall-clock or `EndTick`).
- **No early finalization.** The game runs until that tick regardless of any
  player's score.
- At the end tick, players are ranked by their `land` value (primary) with
  tiebreaker `minerals + gas` then player id. That ordered list **is** the result.
- Top-N placements are reported in battle/end-of-game summaries. There is no
  "winner" concept beyond "rank 1".

### Code changes
- Delete `VictoryConditionModule` from the tick pipeline.
- Replace with `GameEndTimeModule` (or fold into `GameFinalizationModule`) that:
  - Checks `currentTick >= game.EndTick`.
  - If true, freezes further commands, produces ranking, archives the instance.
- Remove `EconomicThreshold` settings; add `EndTick` / `Duration` to `GameSettings`.
- Keep `land` as the score resource — only the trigger changes.
- Remove all "won the game" notifications/messages that fire before end tick.

### Notes
- Player-state freezing at end-tick should block all commands (all command
  handlers already have a natural "game finalized" gate via `GameLifecycleEngine`;
  reuse it).
- New-player protection still applies while the game is live.

---

## 2. Espionage — Remove Entirely

### Current behavior
- `SpyCommand`, `SpyMissionCommand`, `SpyMission` tick module.
- `SpyRepositoryWrite`, noise (±30% / ±20%), per-target cooldowns, detection rolls.
- `PlayerState.SpyCooldowns`, `SpyAttemptLogs`, `LastSpyResults`, `SpyMissions`.
- Tech nodes `counter-intel-basic`, `-advanced`, `-mastery` and
  `TechEffectType.CounterIntelDetection`.

### Target behavior
- **No espionage of any kind.** Removed from the game.

### Code changes
- Remove the `SpyMission` module from the tick pipeline.
- Delete `Repositories/Spy/` entirely.
- Delete `SpyCommand`, `SpyMissionCommand`, and any spy DTOs in
  `Commands/Commands.cs` and `Shared/` ViewModels.
- Strip `PlayerState`: `SpyCooldowns`, `SpyAttemptLogs`, `LastSpyResults`,
  `SpyMissions`, `SpyMissionType`.
- Remove the counter-intel tech nodes (covered by §3 anyway).
- Remove frontend pages/components: spy report, spy target picker, spy log.
- Migration: on deserializing old `WorldState` / `PlayerState` blobs, drop
  the removed fields. Because SCO games are short-lived and state is JSON,
  missing fields will simply not round-trip on next save.

### What's lost
- No hidden-information mechanic. Players will be attacking blind unless a
  lightweight "public stats" page is added. That is **acceptable** — the
  original SCO-style game expected attackers to scout visually, and the
  simpler model is the point.

---

## 3. Research — Collapse to Attack / Defense Upgrades Only

### Current behavior
- Two **parallel** research systems:
  - **Upgrade system** (`ResearchUpgradeCommand`, `UpgradeRepositoryWrite`) —
    Attack / Defense, 3 levels each, 10 ticks per level.
  - **Tech tree** (`ResearchTechCommand`, `TechRepositoryWrite`,
    `TechNodeDef`) — 14+ nodes across 3 tiers covering economy boosts,
    combat boosts, counter-intel.

### Target behavior
- **Only the upgrade system survives.** Attack and Defense, 3 levels each,
  applied via the existing per-unit `AttackBonuses` / `DefenseBonuses` arrays.
- Everything labelled "tech" is **deleted** — no economy boosts, no
  counter-intel, no tier tree, no prerequisites graph.
- Keep upgrades exactly as they are today (levels, costs, times, bonus tables).
  See §16 of `SCO_GAME_LOGIC.md` for the current numbers — they stay unchanged.

### Code changes
- Delete the following entirely:
  - `ResearchTechCommand`, `Repositories/Tech/`, `TechResearchModule` (tick
    module) and its registration in `GameServerExtensions`.
  - `TechNodeDef`, `TechEffectType`, and the per-game tech list that
    `StarcraftOnlineGameDefFactory` builds (the whole "Tier 1/2/3" section).
  - `PlayerState.UnlockedTechs`, `TechBeingResearched`, `TechResearchTimer`.
  - All tech-related ViewModels and React pages (`pages/Tech*`, if any).
- **Keep**: `ResearchUpgradeCommand`, `UpgradeTimerModule`,
  `UpgradeRepositoryWrite`, `AttackUpgradeLevel` / `DefenseUpgradeLevel` /
  `UpgradeBeingResearched` / `UpgradeResearchTimer`.
- Combat code: delete the `+ tech.AttackBonus` / `+ tech.DefenseBonus` terms
  in `BattleBehaviorScoOriginal`; keep the upgrade-level term.
- Economy code: delete the `(1 + sum_of_matching_tech_bonuses)` multiplier in
  `ResourceGrowthSco` — income becomes exactly `base + worker_income`.

### Notes
- This removes ~350 lines from `StarcraftOnlineGameDefFactory.cs` and all of
  the `Tech*` repository.
- It also collapses the "one-at-a-time research" contention: with tech gone,
  only the upgrade slot exists, so players have a single, legible research
  decision at any moment.

---

## 4. Achievements & Gamification — Remove

### Current behavior
- `Achievements/MilestoneCatalogue.cs` defines ~22 milestones across Bronze /
  Silver / Gold / Legendary tiers.
- Cross-game global state tracks XP, levels, win-streaks, milestone unlocks.
- Tick modules and `GameFinalizationModule` award milestones at game end.
- `GlobalState.Users` carries per-user achievement records.

### Target behavior
- **No achievements, no XP, no levels, no tiers, no win-streaks, no
  cross-game "progression" concept.** The only cross-game persistence is
  the player's identity, display name, and (optionally) a history of games
  played — nothing that grants a gameplay advantage or rewards retention.

### Code changes
- Delete `Achievements/` entirely, including `MilestoneCatalogue.cs`,
  milestone awarding logic, and any `IAchievementAwarder` interfaces.
- `GlobalState`: drop `Users[].Achievements`, `Users[].Xp`, `Users[].Level`,
  `Users[].WinStreak`, `Users[].Milestones`.
- Delete achievement ViewModels in `Shared` and achievement pages/components
  in `ReactClient/src/pages/`.
- Remove achievement references from `GameFinalizationModule` and any
  post-battle hooks.
- Leaderboard: keep **only** an end-of-game ranking per finished game (see §1).
  Do not aggregate across games. If a "games played" list per user is useful,
  it's a read-only history, not a score.

### What's lost
- No retention/engagement loop beyond "games are fun". Intentional — the user
  wants the pre-gamification feel of the original.

---

## 5. Summary — What's Left After the Cuts

The simplified SCO game loop is:

1. **Economy.** Workers + land → minerals + gas. (No tech multipliers.)
2. **Build.** Queue → prerequisites → HP/cost/time → buildings & units. Unchanged.
3. **Upgrade.** Attack 0→3, Defense 0→3. Unchanged.
4. **Combat.** 8-round deterministic battles with upgrade bonuses, land
   transfer, pillage, worker capture. Unchanged except for removing the
   tech-bonus term.
5. **Diplomacy.** Markets, direct trades, alliances, alliance wars, chat,
   messaging. Unchanged.
6. **End.** Game runs to a fixed end tick, then freezes and emits a final
   ranking by `land`.

Everything removed:

- Early victory by threshold.
- All espionage.
- All tech research (economy boosts, combat boosts, counter-intel).
- All achievements, XP, levels, streaks, milestone tiers, cross-game progression.

---

## 6. Suggested Execution Order

Do these as **separate PRs** so each is independently reviewable and revertable.
Each PR should compile, pass tests, and leave the game playable.

| # | PR | Touches |
|---|---|---|
| 1 | Remove achievements | `Achievements/`, `GlobalState`, finalization module, Shared + ReactClient achievement pages |
| 2 | Remove espionage | `Repositories/Spy/`, `SpyMission` tick module, spy commands, `PlayerState` spy fields, spy UI |
| 3 | Remove tech tree | `Repositories/Tech/`, `TechResearch` module, `TechNodeDef`, `TechEffectType`, tech data in `StarcraftOnlineGameDefFactory`, tech bonus terms in combat & resource-growth, tech UI |
| 4 | Time-based victory | `VictoryConditionModule` → `GameEndTimeModule`, `GameSettings.EndTick`, finalization path, end-of-game ranking screen |

Tests that need to be updated or deleted live in `StatefulGameServer.Test` — any
test named `*Tech*`, `*Spy*`, `*Achievement*`, or `*VictoryThreshold*` is either
rewritten for the new rules or deleted.

---

## 7. Migration of Persisted State

Both `WorldState` and `GlobalState` are JSON blobs in `games/{id}/state.json`
and `global/state.json`. The cuts above remove fields, not rename them, so
round-tripping old blobs through the new code will simply drop the removed
fields on next save. No bespoke migration code is needed provided:

- Deserialization is tolerant of unknown/missing fields (the default for the
  serializer in use — verify on `PlayerState` in particular).
- A one-time test loads a pre-change blob and saves it post-change without
  errors.

If any live games are in progress at cutover, they should be finalized early
under the old rules before deploying the new code, to avoid mid-game rule
changes.
