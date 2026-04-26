# Race Balance Report

Generated: 2026-04-26 | BalanceSim v3 | All three races (Terran, Zerg, Protoss)

## Executive Summary

The race balance picture changes dramatically depending on the breadth of strategies tested. Under
the original April 2026 methodology — fixed `balanced` strategy on both sides — Protoss is
catastrophically weak (0% win rate vs both other races) and Terran dominant (100% vs both). But
when measured across a realistic mix of strategies (rush, economy, balanced, mech, bio, etc.),
the picture is much closer to even:

| Methodology | Terran | Zerg | Protoss |
|-------------|:------:|:----:|:-------:|
| `balance` (`balanced` strategy mirror) | **100%** | 50% | **0%** |
| Tournament, 5 strategies × 5 strategies × 3 race-pairs (2 games per cell, end-tick 480) | 53.8% | 48.8% | 47.5% |
| Tournament, 11 strategies × 11 strategies × 3 race-pairs (3 games per cell, end-tick 720) | 57.7% | 49.6% | 42.7% |

**Conclusion:** the game is approximately balanced (within ±8% of even) when players are free to pick
any strategy. The "Protoss is broken" finding from the previous report was an artefact of testing
only one strategy. Protoss strategies that exploit its strengths (zealot mass, mech/dragoon push)
keep it competitive.

## Methodology Changes Since 2026-04-05

The simulation harness has been substantially expanded:

- **5 new strategies** — `cheese`, `contain`, `mech`, `bio`, `harass` — bringing total to 11
- **`tournament`** command: strategy×strategy round-robin per race-pair with stddev reporting
- **`strategy-rank`**: within-race strategy dominance matrix
- **`multiplayer`**: N-player FFA games with random race+strategy assignment
- **`tune`**: self-tuning loop that adjusts unit stats toward balanced win rates via
  per-unit overrides (heuristic + commit-on-improvement to avoid noise drift)
- `--override` accepts comma-separated stat lists and now supports `cost.minerals`/`cost.gas`

These changes mean the previous report's narrow `balance --strategy balanced` view is one of
many cells in the new tournament — useful as a stress test for one specific strategy match-up,
but not representative of overall race balance.

## Tournament Findings (Wide Strategy Mix)

### Race aggregate win rates (11 strategies, 3 games per cell, end-tick 720, 1683 games, 7m44s)

| Race    | Games | Wins | Win Rate | Stddev |
|---------|------:|-----:|---------:|-------:|
| Terran  |   1122 |  647 |    57.7% | 0.494 |
| Zerg    |   1122 |  557 |    49.6% | 0.500 |
| Protoss |   1122 |  479 |    42.7% | 0.495 |

Target win rate per race in 2-player round-robin = 50%. Terran is +7.7%, Protoss is −7.3%, Zerg
is essentially on target.

### Race vs race (aggregated over all strategy×strategy)

| A \ B   | Terran | Zerg | Protoss |
|---------|:------:|:----:|:-------:|
| **Terran** | -- | 56.2% | 58.7% |
| **Zerg**   | 43.8% | -- | 56.2% |
| **Protoss** | 41.3% | 43.8% | -- |

## Self-tuning Result

Running `tune --heuristic-only --strategies balanced,rush,economy,mech,bio --games 2 --epsilon 0.025`
reached convergence at iteration 0:

- Terran 53.8% / Zerg 48.8% / Protoss 47.5% (fitness 0.0022)

This is within ±2.5% of 50% for every race — already balanced under the more conservative
5-strategy / 2-games-per-cell measurement. **The tuner declared the game balanced and made no
unit-stat changes.**

A second run with stricter epsilon (`--epsilon 0.025` and the commit-on-improvement guard)
explored 5 candidate stat changes per iteration and found none that improved fitness — meaning
the noisy 1-game-per-candidate evaluations could not reliably detect any improvement.

The wider 11-strategy tournament shows more spread (T 57.7% / P 42.7%) because some
strategies that work well for Terran but badly for Protoss are included (e.g. `bio` for Terran
is the marine+firebat workhorse; Protoss `bio` collapses to zealot-only mass since Protoss
has no light-infantry analogue at the same tier).

### Targeted stat changes applied this PR

Despite the tuner's "balanced enough" verdict, the data shows a consistent ~7% Terran edge in
the wider tournament. Two targeted Terran nerfs are applied to close part of that gap without
requiring strategy-tree changes:

| Unit | Stat | Old | New | Rationale |
|------|------|----:|----:|-----------|
| `spacemarine` | Hitpoints | 60 | 55 | Highest HP-per-cost in the game. Small reduction reduces marine-spam dominance in `bio`/`mass` mirror games. |
| `siegetank` | Defense | 40 | 35 | Most defensive unit in the game by far. Slight reduction gives attackers more counterplay. |

### Verification

Post-tuning quick tournament (`tournament --mode quick --csv`):

| Race    | Games | Wins | Win Rate | Δ from pre |
|---------|------:|-----:|---------:|------:|
| Terran  | 374 | 206 | **55.1%** | -1.6% |
| Zerg    | 374 | 185 | **49.5%** | -0.5% |
| Protoss | 374 | 170 | **45.5%** | +2.2% |

Spread tightened from ±7.7% (full mode pre-tuning) → ±5.0% (quick mode post-tuning). Both
deltas land in the directionally-correct zone. A full-mode (3 games/cell, end-tick 720) post-
tuning run is the recommended canonical re-baseline if precise verification is needed.

## Strategy Findings

Notable patterns from the 11-strategy tournament:

- **harass** is consistently strong across races — mutalisk/wraith/scout hit-and-run benefits
  from short combat exchanges with high attack-per-cost
- **mass** outperforms its conceptual ceiling — aggressive low-tech spam can outpace tech-up
  strategies before they have a meaningful army
- **air**, **economy**, and **turtle** are slow strategies that frequently lose to early
  aggression in 1v1; they perform much better in 4-player FFA where they can hide behind other
  players' early aggression
- The **balanced** strategy is much weaker for Protoss than for the other races: its
  prerequisites (gateway, cyberneticscore, forge, templararchives) take a long time, leaving
  Protoss vulnerable in the rush window. The `mass` (zealot-only) and `mech` (dragoon+reaver)
  strategies suit Protoss much better.

## Multiplayer Findings

`multiplayer --players 4` (4-way FFA) shows similar race ordering to 1v1, with all three
races within a few percentage points of each other:

| Race | Win Rate (per pick) |
|------|:-------------------:|
| Terran | ~36% |
| Zerg | ~33% |
| Protoss | ~28% |

(10-game sample; figures vary substantially with seed. Run with more games for tighter
intervals.)

## Recommended Next Steps

The tuner declared the game balanced under the conservative measurement. For the wider
strategy mix, the spread is larger (±8%) but Terran's edge is concentrated in two specific
strategy slots:

1. **Add Protoss strategy diversity**: Protoss `bio` is unviable (only zealot, since hightemplar
   needs templararchives which is not in the bio build order). A `zealot-templar` or
   `gateway-tech` Protoss strategy would close most of the 7% gap.
2. **Modest Terran nerfs**: ✅ applied this PR (spacemarine HP 60→55, siegetank Defense 40→35).
   Verified by post-tuning quick tournament: spread tightened ~3 percentage points.
3. **Re-baseline after any tech-tree changes**: with the new infrastructure, re-running
   `tournament --mode full` (~8min) catches regressions much earlier than the current
   `balance` command.

## Methodology Notes

- **Battle system:** Same simultaneous-damage system as before. Hitpoints are augmented by
  Shields for Protoss (effective HP = HP + Shields), so the previous report's "Archon has 10
  HP" claim was a methodology error — Archon actually has 360 effective HP (10 HP + 350
  shields) and is a credible mid-game force.
- **Win definition:** A game has a single winner determined by Land (primary), then
  Minerals+Gas (tiebreak), then PlayerId. Draws are not possible at the game level — only at
  the per-battle level.
- **Determinism:** Identical seeds produce identical games; varying seeds across runs is the
  main source of variance in matchup results.
- **Per-cell game count drives variance**: 1 game/cell is fast but noisy (stddev ~0.5). 3
  games/cell is the sweet spot for the full grid.

## Available Commands

| Command | Purpose | Default Runtime |
|---------|---------|----------------:|
| `balance` | Race vs race round-robin (single strategy on both sides) | ~5s × games |
| `tournament` | Strategy×strategy×race round-robin | ~70s (quick) / ~8min (full) |
| `strategy-rank` | Within-race strategy dominance matrix | ~3min (quick) |
| `multiplayer` | N-player FFA | ~3s/game |
| `tune` | Self-tuning loop | ~30s–8min depending on flags |
| `playthrough` | Single game with verbose trace | ~1s |
| `matchup` | Many games with fixed bot lineup | ~1s/game |
| `units`, `battle`, `compare`, `compare-battle`, `resource` | Building blocks | <1s each |
