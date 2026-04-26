# Balance Sim Expansion & Self-Tuning Loop — Design Spec

Date: 2026-04-26 · Branch: `feat/balance-sim-tuning`

## Goal

Expand the BalanceSim infrastructure with more strategies and richer simulation modes,
add a self-tuning loop that proposes per-unit stat changes targeting balanced race win
rates (~33/33/33), then apply the resulting changes to the SCO game definition and
regenerate `docs/balance-report.md`. Deliver as one PR.

## Half A — Simulation Infrastructure

### New strategies

Added to `BotPresets.cs`. All strategy/race combinations must keep the existing
invariant: every unit in `UnitMix` has its prerequisite asset in `BuildOrder`.

| Strategy | Concept | Notes |
|----------|---------|-------|
| `cheese` | Pre-tick-90 all-in with starter-tier units. No tech, minimum workers. | Race-symmetric (zealot/zergling/marine). |
| `contain` | Heavy static defense + ranged poke. Long game, controls space. | Uses turret/sunken/cannon + dragoon/hydralisk/siegetank. |
| `mech`   | Mechanical units (vulture, tank, wraith) for Terran; analogues for others. | Terran-heavy archetype; non-Terran gets a "ground armor" analogue (Lurker/Ultralisk for Zerg, Dragoon/Archon for Protoss). |
| `bio`    | Marine + firebat with academy. Terran-tilted; non-Terran gets a "pure light infantry" analogue. | Zergling-spam for Zerg; Zealot-mass for Protoss. |
| `harass` | Mutalisk/Wraith/Scout hit-and-run. | Air-mobility focused. |

These bring strategy count from 6 → 11. (We may collapse `bio`/`mass` if they end up
identical for non-Terran — final list to be decided during implementation.)

### New / expanded commands

- **`tournament`** — strategy×strategy round-robin per race-pair, N seeds each. Reports
  win matrix per race-pair plus per-race aggregate win rate ± stddev. Subsumes the
  current `balance` command but keeps `balance` as a back-compat alias.
- **`strategy-rank`** — within-race strategy strength matrix. For each race, runs all
  strategies vs all strategies (mirror-race) and reports a strategy dominance table.
  Useful for identifying degenerate strategies (always wins) or noise (always tied).
- **`multiplayer`** — 3-player and 4-player FFA. Extends `PlaythroughRunner` if needed.
  Runs N games with random race+strategy assignment; reports per-race and per-strategy
  win rates. **Cut criterion**: if `PlaythroughRunner` has hard-coded 2-player
  assumptions that are nontrivial to lift, this command ships as a follow-up issue
  rather than blocking the PR.
- All commands gain `--mode quick|full` (quick = ~30s sample for iteration; full =
  ~2-3min for reports). Mode controls game count and end-tick.

### Variance reporting

Existing matchup reports point estimates only. New reports include stddev across seeds
so the tuner can distinguish signal from noise.

## Half B — Self-Tuning Loop (`tune` command)

### Knobs

Per-unit deltas on:

- `Hitpoints` (integer)
- `Attack` (integer)
- `Defense` (integer)
- `Cost.Mineral` and `Cost.Gas` (integer)

Plumbed through `--override` so the tuner doesn't mutate game definitions in-process.
Override syntax extension: `unit.cost.mineral=value`, `unit.cost.gas=value`.

### Fitness

```
fitness = Σ_race (winrate − 1/3)²  +  λ · Σ_unit Σ_stat (delta / original)²
```

The penalty term λ keeps deltas conservative. λ tuned during implementation; start at
0.05.

### Algorithm — coordinate descent

```
state = identity (no overrides)
loop until converged or budget exhausted:
  measure: run quick tournament with current state, compute race winrates
  identify: race with largest winrate deviation from 1/3
  candidates: for each combat unit of that race, propose:
    - if race underpowered: +HP, +ATK, -mineral cost (one at a time, ~10% step)
    - if race overpowered: -HP, -ATK, +mineral cost
  evaluate: run mini-sim (fewer seeds) for each candidate, measure delta in fitness
  pick: best candidate, lock into state
  cap: refuse any cumulative delta beyond ±30% of original stat
  log: append iteration to tuning-log.md
```

Stop conditions:

- All race winrates within ±5% of 33% (converged)
- 15 iterations (max)
- 8-min wall clock (budget)

### Outputs

- `docs/superpowers/specs/2026-04-26-tuning-log.md` — iteration trace
- `tools/balance-tuning/proposed-stat-changes.json` — final delta set
- Final-pass *full* tournament report saved into `docs/balance-report.md`

### Applier

A small static method reads the JSON and rewrites the SCO unit definitions (in
`StarcraftOnlineGameDefFactory.cs` or per-race builder files). Implemented as a manual
edit driven by the JSON for safety — not an automated codegen. Build + tests pass
afterwards.

## Autonomous run plan (20-min budget)

1. ~2 min: build, baseline tournament on current stats
2. ~3 min: implement new strategies + tournament/strategy-rank commands
3. ~2 min: implement `multiplayer` (or cut)
4. ~3 min: implement `tune` loop + cost overrides
5. ~6 min: run `tune` autonomously
6. ~2 min: apply tuned stats, run tests, regenerate report
7. ~2 min: commit, push, open PR

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Multiplayer needs extensive engine changes | Cut from this PR; ship as follow-up. |
| Coordinate descent chases noise | Variance reporting + λ penalty + ±30% cap. |
| Sim is slower than predicted | Quick mode small enough that each iteration ≤30s. |
| Tuner over-deforms a single unit | Cap on cumulative delta + per-iteration step ≤ 10% original. |
| Unit cost overrides not yet plumbed | Extend `--override` parser; trivial change. |

## Out of scope

- Building / asset cost tuning (units only)
- Tech-tree / prerequisite changes
- Map / colonization tuning
- Worker income or resource-rate tuning
- Anything in the React client
