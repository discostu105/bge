# Race Balance Report

Generated: 2026-04-05 | BalanceSim v2 | All three races (Terran, Zerg, Protoss)

## Executive Summary

**Protoss is critically underpowered.** Across 54 simulations (3 tiers × 3 upgrade levels × 6 directional matchups), Protoss wins zero decisive battles against either Terran or Zerg. The root cause is that Protoss units are individually expensive with low HP-per-cost, meaning equal-budget Protoss armies have far fewer total hitpoints. The simultaneous-damage battle system heavily favors armies with more units and higher aggregate HP.

**Terran is the strongest race**, winning or drawing every simulation against Zerg and decisively winning 83% of simulations against Protoss. Zerg is the second strongest, competitive with Terran but dominant over Protoss.

Resource income is identical across all three races — balance differences are purely unit-stat driven.

## Unit Overview

| Unit | Race | Cost | Total | Atk | Def | HP | Atk Bonus (1/2/3) | Def Bonus (1/2/3) |
|------|------|------|------:|----:|----:|---:|:------------------:|:-----------------:|
| Space Marine | Terran | 45m | 45 | 2 | 4 | 60 | 1/2/3 | 1/2/3 |
| Firebat | Terran | 50m+25g | 75 | 9 | 6 | 50 | 2/4/6 | 1/2/3 |
| Vulture | Terran | 75m | 75 | 8 | 2 | 70 | 2/4/6 | 1/2/3 |
| Siege Tank | Terran | 125m+100g | 225 | 10 | 40 | 130 | 2/4/6 | 3/6/9 |
| Wraith | Terran | 200m+100g | 300 | 36 | 14 | 230 | 3/6/9 | 2/4/6 |
| Battlecruiser | Terran | 300m+300g | 600 | 70 | 45 | 500 | 5/10/15 | 4/8/12 |
| Zergling | Zerg | 40m | 40 | 3 | 1 | 25 | 1/1/2 | 1/1/1 |
| Hydralisk | Zerg | 75m+50g | 125 | 15 | 5 | 80 | 1/2/3 | 1/1/2 |
| Lurker | Zerg | 100m+100g | 200 | 12 | 26 | 195 | 1/1/2 | 2/4/6 |
| Mutalisk | Zerg | 200m+25g | 225 | 20 | 26 | 120 | 1/2/3 | 2/4/6 |
| Ultralisk | Zerg | 250m+200g | 450 | 45 | 30 | 450 | 2/4/6 | 2/4/6 |
| Guardian | Zerg | 100m+200g | 300 | 50 | 35 | 200 | 4/8/12 | 2/4/6 |
| Zealot | Protoss | 125m | 125 | 5 | 6 | 80 | 1/2/3 | 1/2/3 |
| Dragoon | Protoss | 150m+50g | 200 | 12 | 18 | 100 | 1/2/3 | 1/2/3 |
| Dark Templar | Protoss | 125m+100g | 225 | 30 | 30 | 40 | 2/4/6 | 2/4/6 |
| Archon | Protoss | 120m+300g | 420 | 28 | 42 | 10 | 2/4/6 | 2/4/6 |
| Corsair | Protoss | 150m+100g | 250 | 18 | 12 | 100 | 2/4/6 | 1/2/3 |
| Scout | Protoss | 300m+150g | 450 | 28 | 49 | 150 | 2/4/6 | 4/8/12 |
| Carrier | Protoss | 550m+300g | 850 | 80 | 55 | 300 | 4/8/12 | 4/8/12 |

*Only combat units used in simulations are listed. Workers, static defense, and support units omitted.*

## Army Compositions

### Tier 1 — Early Game (~1000 resources)

| Race | Army | Units | Total Cost |
|------|------|------:|-----------:|
| Terran | spacemarine:22 | 22 | 990 |
| Zerg | zergling:25 | 25 | 1000 |
| Protoss | zealot:8 | 8 | 1000 |

### Tier 2 — Mid Game (~3100 resources)

| Race | Army | Units | Total Cost |
|------|------|------:|-----------:|
| Terran | spacemarine:15, firebat:10, siegetank:5, vulture:8 | 38 | 3150 |
| Zerg | zergling:15, hydralisk:12, lurker:5 | 32 | 3100 |
| Protoss | zealot:8, dragoon:6, darktemplar:4 | 18 | 3100 |

### Tier 3 — Late Game (~6000 resources)

| Race | Army | Units | Total Cost |
|------|------|------:|-----------:|
| Terran | spacemarine:20, siegetank:5, wraith:5, battlecruiser:3, vulture:8 | 41 | 5925 |
| Zerg | zergling:20, hydralisk:10, ultralisk:3, guardian:5, mutalisk:5 | 43 | 6025 |
| Protoss | zealot:10, dragoon:8, archon:2, carrier:1, scout:2, corsair:2 | 25 | 5940 |

**Cost deviation:** All tiers within ~1% across races.

**Key observation:** At equal cost, Protoss fields significantly fewer units (8/18/25) than Terran (22/38/41) or Zerg (25/32/43). This results in dramatically lower total HP.

| Tier | Terran HP | Zerg HP | Protoss HP |
|------|----------:|--------:|-----------:|
| 1 | 1320 | 625 | 640 |
| 2 | 2610 | 2310 | 1400 |
| 3 | 5060 | 4250 | 2420 |

## Battle Results Matrix

Each cell shows the outcome for the **row race as attacker** vs the column race as defender. Results format: **W** (attacker wins = all defenders killed), **L** (defender wins = all attackers killed), **D** (draw = both sides have survivors).

### Tier 1 — Early Game

| Attacker ↓ \ Defender → | Terran | Zerg | Protoss |
|--------------------------|:------:|:----:|:-------:|
| **Terran** | — | D / D / **W** | D / D / **W** |
| **Zerg** | D / **L** / **L** | — | D / **W** / **W** |
| **Protoss** | **L** / **L** / **L** | D / D / D | — |

*Columns: upgrade 0/0, 1/1, 3/3*

### Tier 2 — Mid Game

| Attacker ↓ \ Defender → | Terran | Zerg | Protoss |
|--------------------------|:------:|:----:|:-------:|
| **Terran** | — | D / D / D | D / **W** / **W** |
| **Zerg** | D / D / **L** | — | **W** / **W** / **W** |
| **Protoss** | **L** / **L** / **L** | **L** / **L** / **L** | — |

### Tier 3 — Late Game

| Attacker ↓ \ Defender → | Terran | Zerg | Protoss |
|--------------------------|:------:|:----:|:-------:|
| **Terran** | — | D / **W** / **W** | **W** / **W** / **W** |
| **Zerg** | D / D / D | — | **W** / **W** / **W** |
| **Protoss** | **L** / **L** / **L** | **L** / **L** / **L** | — |

## Win Rate Summary

Counting across all 54 simulations (18 per matchup pair). A decisive result counts as 1.0 for the winner; a draw counts as 0.5 for each side.

| Matchup | Decisive Wins | Draws | Score | Win Rate |
|---------|:---:|:---:|:---:|:---:|
| **Terran vs Zerg** | T:6, Z:0 | 12 | T:12, Z:6 | **T 67% — Z 33%** |
| **Terran vs Protoss** | T:15, P:0 | 3 | T:16.5, P:1.5 | **T 92% — P 8%** |
| **Zerg vs Protoss** | Z:14, P:0 | 4 | Z:16, P:2 | **Z 89% — P 11%** |

### Overall Race Power Ranking

| Race | Avg Win Rate | Assessment |
|------|:-----------:|:----------:|
| Terran | **79%** | Overpowered |
| Zerg | **61%** | Slightly strong |
| Protoss | **10%** | Critically underpowered |

## Resource Efficiency Analysis

Cost exchange ratios from key matchups (attacker resources lost : defender resources lost):

| Matchup | Tier 1 (0/0) | Tier 2 (0/0) | Tier 3 (0/0) |
|---------|:---:|:---:|:---:|
| T attacks Z | 90:520 (5.8x T) | 1335:1975 (1.5x T) | 1500:5575 (3.7x T) |
| Z attacks T | 920:225 (4.1x T) | 2700:1245 (2.2x T) | 3175:4725 (1.5x Z) |
| T attacks P | 180:500 (2.8x T) | 930:2900 (3.1x T) | 675:5940 (8.8x T) |
| P attacks T | 1000:135 (7.4x T) | 3100:750 (4.1x T) | 5940:1050 (5.7x T) |
| Z attacks P | 360:750 (2.1x Z) | 725:3100 (4.3x Z) | 1050:5940 (5.7x Z) |
| P attacks Z | 125:480 (3.8x P) | 3100:1100 (2.8x Z) | 5940:2050 (2.9x Z) |

**Key finding:** Terran achieves 3-9x cost efficiency against Protoss at all tiers. Protoss only achieves favorable trades as attacker against Zerg at Tier 1 (3.8x), but this reverses at higher tiers.

## Upgrade Scaling Analysis

### How upgrades affect outcomes

| Matchup | 0/0 Result | 1/1 Result | 3/3 Result | Trend |
|---------|:----------:|:----------:|:----------:|:-----:|
| T1: T→Z | Draw | Draw | T wins | Upgrades favor Terran |
| T1: Z→T | Draw | T wins | T wins | Upgrades amplify Terran advantage |
| T1: Z→P | Draw | Z wins | Z wins | Upgrades favor Zerg |
| T2: T→P | Draw | T wins | T wins | Upgrades favor Terran |
| T2: Z→T | Draw | Draw | T wins | Upgrades favor defender |
| T3: T→Z | Draw | T wins | T wins | Upgrades favor Terran |

**Pattern:** Terran benefits most from upgrades due to higher per-unit upgrade bonuses on Space Marines and Siege Tanks. Zerg's upgrade bonuses on Zerglings (+1/+1/+2 atk, +1/+1/+1 def) are the weakest in the game, but their numerical advantage compensates at lower upgrade levels.

Protoss has strong per-unit upgrade bonuses but too few units for them to matter — the army is wiped before upgrades create meaningful differential.

## Imbalance Flags

### CRITICAL: Protoss vs Terran — 92% Terran win rate

Protoss loses every single decisive engagement against Terran across all tiers and upgrade levels. At Tier 3, Terran loses only 540-1050 resources while destroying the entire 5940-cost Protoss army.

**Root causes:**
1. **HP deficit:** Protoss army has 2420 total HP vs Terran's 5060 at Tier 3 (2.1x gap)
2. **Unit count deficit:** 25 Protoss units vs 41 Terran — fewer units means less total damage output
3. **Archon is nearly useless:** 10 HP for 420 cost means it dies in the first combat round, contributing almost nothing
4. **Protoss units are overcosted for their durability**

### CRITICAL: Protoss vs Zerg — 89% Zerg win rate

Same fundamental problem — Zerg fields nearly twice as many units with more total HP.

### MODERATE: Terran vs Zerg — 67% Terran win rate

Terran's advantage is driven by superior HP efficiency. Space Marines (60 HP for 45 cost = 1.33 HP/cost) outclass Zerglings (25 HP for 40 cost = 0.63 HP/cost) by over 2x in HP efficiency. Siege Tanks (130 HP, 40 def for 225 cost) are the most durable unit per cost in the game.

## Proposed Tuning Changes

### Priority 1: Fix Protoss HP crisis

The core problem is HP-per-cost. Protoss units need significantly more HP to be competitive:

| Unit | Current HP | Proposed HP | Rationale |
|------|:---------:|:----------:|-----------|
| **Archon** | 10 | **350** | Most extreme case. 10 HP on a 420-cost unit is unplayable. 350 HP makes it a tanky mid-tier unit (0.83 HP/cost) |
| **Dark Templar** | 40 | **120** | High attack (30) + high def (30) justifies moderate HP. 120 makes it a glass cannon that survives 2+ rounds |
| **High Templar** | 40 | **100** | Support unit shouldn't be a 1-hit kill at 200 cost |
| **Zealot** | 80 | **120** | Core infantry needs to be comparable to Marine HP efficiency (currently 0.64 HP/cost → 0.96) |
| **Dragoon** | 100 | **160** | Protoss core ranged unit needs to compete with Hydralisk (80 HP but half the cost) |
| **Probe** | 20 | **40** | Worker HP gap (20 vs 60 for WBF) is extreme |

### Priority 2: Nerf Terran durability slightly

| Unit | Current Stat | Proposed Stat | Rationale |
|------|:-----------:|:------------:|-----------|
| **Space Marine** | 60 HP | **50 HP** | Slight reduction to close the gap with other T1 units |
| **Siege Tank** | 40 def | **30 def** | Slightly less tanky to give attackers more counterplay |

### Priority 3: Buff Zerg upgrade scaling

| Unit | Current Atk Bonus | Proposed Atk Bonus | Rationale |
|------|:-----------------:|:-----------------:|-----------|
| **Zergling** | 1/1/2 | **1/2/3** | Linear scaling like other races' basic units |
| **Hydralisk** | 1/2/3 | **2/3/5** | Core ranged unit needs stronger scaling to keep pace |

## Methodology Notes

- **Battle system:** Simultaneous damage per round. Each unit attacks one random enemy. Attacker "wins" only if ALL defenders are killed; defender "wins" only if ALL attackers are killed. Otherwise it's a draw with both sides having survivors.
- **Deterministic:** No randomness — same inputs always produce same outputs. "Win rate" is across composition matchups, not Monte Carlo.
- **54 simulations total:** 3 tiers × 6 directional matchups × 3 upgrade configs (0/0, 1/1, 3/3)
- **Cost equalization:** Army costs within ~1% per tier across all three races
- **No economic factors:** Simulations are pure combat; build time, tech tree, and economic ramp are not modeled
- **Asymmetric system:** The attacker-wins-only-if-all-die rule favors defenders, so all matchups were run in both directions
