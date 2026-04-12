# Realistic Dev Seed Scenario

## Problem
Running BGE locally in dev produces an empty world: `StarcraftOnlineWorldStateFactory.CreateDevWorldState()` defaults to `playerCount=0`. There are no players, no alliances, no progression — nothing to click around. To exercise any non-trivial UI or gameplay flow a developer has to manually build state every session.

## Goal
When a developer starts the server with default dev settings, they land in a world that already looks mid-game: many players at varied progression tiers, multiple alliances with live state (war, election, invites, posts), global chat history, open market orders, and pending trade offers.

## Configuration
New option `Bge:DevSeedScenario` on `BgeOptions`:
- `empty` — current behavior (no players)
- `realistic` — the new scenario (default)

Only consulted when creating a brand-new dev world. Existing persisted state in `storage/games/default/state.json` always wins; a dev who already has state sees no change.

## Architecture
- `IWorldStateFactory` gains `CreateRealisticDevWorldState()`.
- `StarcraftOnlineWorldStateFactory` implements it by delegating to a new static builder `RealisticDevScenario.Build()` in the same project. Keeps the factory file short.
- `TestWorldStateFactory` throws `NotImplementedException` — tests that want an empty state use the existing parameterised path.
- `Program.cs` reads `Bge:DevSeedScenario` during `ConfigureGameServices`. If set to `realistic`, uses the new method; otherwise falls back to `CreateDevWorldState(0)`.

## Scenario Contents

**Game tick:** starts at 150.

**Players:** 20 total, IDs `devstu#0`…`devstu#19`, all `terran`, distinctive commander names. Four tiers:

| Tier | Count | Minerals | Gas | Land | Key assets | Army highlights |
|------|-------|---------:|----:|-----:|---|---|
| Elite | 2 | 80k | 40k | 120 | CC L3, Factory L3, Spaceport L2, Armory L2, Starport | Siege tanks, wraiths, battlecruisers |
| Strong | 6 | 40k | 20k | 85 | CC L2, Factory L2, Spaceport L1, Armory L1 | Siege tanks, marines, wraiths |
| Average | 8 | 15k | 8k | 55 | CC L1, Factory L1, Armory L1 | Marines, a few siege tanks |
| Struggling | 4 | 3k | 1k | 25 | CC L1 only | Marines, WBFs |

Three players have a `siegetank` unit with `Position` set to another player's id — in-flight hostile movement reflecting the active war.

**Alliances:**

| Name | Members | Leader | State |
|------|--------:|--------|-------|
| Iron Wolves | 5 (both elites + 3 strong) | `devstu#0` Ironclad | Completed election in `ElectionHistory`; ~8 `AlliancePostImmutable` entries |
| Red Sun Pact | 5 (3 strong + 2 average) | `devstu#5` Crimson | 5 posts; ~3 pending members listed as `IsPending=true` |
| Azure Order | 4 (average) | `devstu#10` Vashik | **ActiveElection** with 2 candidates and partial votes |
| Nomad Guild | 4 (average + struggling) | `devstu#14` Dustrunner | 1 pending `AllianceInviteImmutable` targeting an unaffiliated player |

Two players remain unaffiliated.

**Wars:** One `AllianceWarImmutable` — Iron Wolves vs Red Sun Pact, declared ~40 ticks ago.

**Chat:** ~15 `ChatMessageImmutable` entries spread over recent ticks (banter + war taunts).

**Market:** ~6 `MarketOrderImmutable` — mix of buy/sell for minerals and gas at varied prices, from various players.

**Trade offers:** ~3 pending `TradeOfferImmutable` between alliance members.

## Out of scope
- No backfilled notifications or action log.
- No battle history beyond what a freshly declared war naturally contains.
- No UI changes.
- No changes to how persisted state is loaded — only the "first boot" path.

## Testing
New test `RealisticDevScenarioTest` (in `StatefulGameServer.Test`):
1. Builds the scenario via a direct call to the builder.
2. Runs `WorldStateVerifier.Verify(gameDef, state)` — guards against any mismatch between the scenario data and `GameDef` (unknown asset ids, missing resources, etc.).
3. Converts to mutable, registers in a `GameInstance`, runs 5 ticks — ensures queued actions and positioned units don't crash the tick engine.

## Risks
- `WorldStateVerifier` may have invariants the scenario misses (e.g. required resources). Covered by the new test.
- Adding to `IWorldStateFactory` forces every implementer to update; only `TestWorldStateFactory` exists today, so the blast radius is one file.
