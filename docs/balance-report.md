# Race Balance Report

Generated: 2026-04-05 | BalanceSim v1 | All three races (Terran, Zerg, Protoss)

## Executive Summary

**Protoss is critically underpowered.** Late-game matchups show 8-10x cost efficiency favoring both Terran and Zerg over Protoss. The root cause is extremely low HP values on key Protoss units (Archon: 10 HP, Dark Templar: 40 HP, High Templar: 40 HP) that don't justify their cost. Terran is the strongest race across all game phases.

Resource income is identical across all three races — no economic differentiation exists.

## Battle Simulation Results

### Early Game (~1000 resources, no upgrades)

| Matchup | Armies | Winner | Exchange Ratio |
|---------|--------|--------|---------------:|
| T vs Z | 20 Marines vs 25 Lings | Both survive, T favored | 5.33x |
| T vs P | 20 Marines vs 8 Zealots | Both survive, T favored | 1.67x |
| Z vs P | 25 Lings vs 8 Zealots | Both survive, Z favored | 2.08x |

**Analysis:** Terran marines are extremely cost-efficient early game. Zealots are overcosted at 125m for their combat output. Zerglings trade well against Protoss ground.

### Mid Game (~3000 resources, Level 1 upgrades)

| Matchup | Armies | Winner | Exchange Ratio |
|---------|--------|--------|---------------:|
| T vs Z | Marine/Firebat/Tank vs Ling/Hydra | T wins | 2.26x |
| T vs P | Marine/Firebat/Tank vs Zealot/Dragoon | Roughly even | 0.98x |
| Z vs P | Ling/Hydra vs Zealot/Dragoon | Z wins | 2.55x |

**Analysis:** Mid-game is the most balanced for Protoss, with TvP being nearly even. Dragoon defense (19 at L1) provides good survivability. Zerg still dominates Protoss.

### Late Game (~5000-6000 resources, Level 2 upgrades)

| Matchup | Armies | Winner | Exchange Ratio |
|---------|--------|--------|---------------:|
| T vs Z | Tank/Wraith/BC vs Hydra/Ultra/Guardian | T wins | **2.61x** |
| T vs P | Tank/Wraith/BC vs Dragoon/Reaver/Carrier | T wins | **9.83x** |
| Z vs P | Hydra/Ultra/Guardian vs Dragoon/Reaver/Carrier | Z wins | **8.85x** |
| P vs T (reverse) | Dragoon/Reaver/Carrier vs Tank/Wraith/BC | P loses | 0.41x |
| P vs Z (reverse) | Dragoon/Reaver/Carrier vs Hydra/Ultra/Guardian | P loses | 0.49x |

**Analysis:** Late-game Protoss is catastrophically weak. Exchange ratios of 8-10x indicate a fundamental stat problem, not a composition issue. Even when Protoss attacks (gaining initiative), they lose badly.

## Resource Economy

Resource income is identical across all three races at all tech levels. No race has an economic advantage or differentiation.

| Metric | Terran | Zerg | Protoss |
|--------|--------|------|---------|
| M/tick (10 workers) | 50 | 50 | 50 |
| G/tick (5 workers) | 30 | 30 | 30 |
| Total M at tick 100 | 5000 | 5000 | 5000 |

## Critical Imbalances Identified

### 1. Protoss HP Values Are Too Low (CRITICAL)

Several Protoss units have HP far below what their cost warrants:

| Unit | Cost | HP | HP/Cost | Comparable Unit | HP | HP/Cost |
|------|------|----|---------|-----------------|----|---------| 
| Archon | 120m+300g | **10** | 0.02 | Ultralisk (250m+200g) | 450 | 1.00 |
| Dark Templar | 125m+100g | **40** | 0.18 | Lurker (100m+100g) | 195 | 0.98 |
| High Templar | 50m+150g | **40** | 0.20 | Defiler (50m+150g) | 80 | 0.40 |
| Reaver | 275m+100g | **100** | 0.27 | Siege Tank (125m+100g) | 130 | 0.58 |
| Carrier | 550m+300g | **300** | 0.35 | Battlecruiser (300m+300g) | 500 | 0.83 |

The Archon is the worst offender: 420 total resource cost for 10 HP. It dies to a single zergling attack round.

### 2. Protoss Lacks Shields Mechanic (MEDIUM)

In the original StarCraft, Protoss units have regenerating shields on top of HP, which compensates for lower base HP. BGE has no shield mechanic implemented. Without shields, Protoss HP values need to be significantly higher to compensate.

### 3. Reaver Has 0 Defense (LOW)

The Reaver (275m+100g) has 65 attack but **0 defense** at all upgrade levels. This makes it a glass cannon that dies instantly to any focus fire, with no defensive scaling from upgrades.

## Proposed Tuning Changes

### Priority 1: Increase Protoss HP (addresses #1)

| Unit | Current HP | Proposed HP | Rationale |
|------|-----------|-------------|-----------|
| Archon | 10 | **350** | Premium unit at 420 resources should be durable; shields lore |
| Dark Templar | 40 | **120** | Stealth assassin but shouldn't be paper-thin |
| High Templar | 40 | **80** | Spellcaster; match Defiler parity |
| Probe | 20 | **40** | Match Drone HP for worker parity |
| Reaver | 100 | **200** | Expensive siege unit needs survivability |
| Carrier | 300 | **450** | Flagship unit should approach BC durability |
| Corsair | 100 | **150** | Air fighter needs to survive engagements |
| Scout | 150 | **250** | Expensive air unit (450 total cost) |
| Observer | 20 | **40** | Scouting unit survivability |
| Photon Cannon | 100 | **200** | Static defense should be durable |

### Priority 2: Add Reaver Base Defense

| Unit | Current Def | Proposed Def | Def Bonus |
|------|------------|-------------|-----------|
| Reaver | 0 | **10** | 2/4/6 |

### Priority 3: Consider Shields Mechanic (Future)

Implement a `ShieldPoints` field on Protoss units that regenerates between battles. This would be the proper long-term fix that matches lore and enables more nuanced balance tuning. Defer to a separate task.

## Methodology

- **Tool:** `BrowserGameEngine.BalanceSim` CLI
- **Compositions:** Representative armies at 3 cost tiers (early ~1000m, mid ~3000m+g, late ~5000-6000m+g)
- **Upgrades:** Level 0 (early), Level 1 (mid), Level 2 (late)
- **Matchups:** All 6 pairwise combinations (3 forward + 3 reverse for late game)
- **Metric:** Exchange ratio = (defender resources lost) / (attacker resources lost). >1 favors attacker.
