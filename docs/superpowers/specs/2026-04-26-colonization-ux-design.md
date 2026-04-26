# Colonization UX redesign — per-tile cost, no batch cap

**Date:** 2026-04-26
**Status:** Draft, pending implementation plan

## Problem

Today's colonization is capped at 24 tiles per call (`ColonizeDialog.tsx:22`, `ColonizeRepositoryWrite.cs:24`). The cap was the mechanism that made colonization progressively expensive: cost per tile is `max(1, currentLand / 4)` and is **flat within a batch**, so larger goals (e.g. 240 land) require repeating the dialog 10 times — each repeat priced higher than the last.

The cap delivers progressive cost but at heavy UX expense. The goal of this redesign is to keep — and smooth — the progressive-cost mechanic while reducing it to a single user action.

## Goal

A player who wants to colonize N tiles does it in **one** dialog interaction, sees the cumulative cost up front, and pays a price that grows continuously with land owned (no within-batch flat-rate discount).

## Non-goals

- Changing the underlying cost coefficient (`/4`) or any other game balance constant.
- Adding cooldowns, queues, or any time-based pacing of colonization.
- Reworking the Base page's "X minerals per new tile" hint.

## Design

### Cost mechanic (server)

Replace flat-batch pricing with per-tile integral pricing. Colonizing N tiles starting at L land costs:

```
totalCost = Σ_{k=0..N−1} max(1, (L + k) / 4)
```

Computed exactly in `decimal`. Closed-form for the common case `L ≥ 4`:

```
totalCost = N · (2L + N − 1) / 8
```

For `L < 4` (early game, where the floor `max(1, …)` matters), iterate the first `4 − L` terms at cost 1 each, then closed-form the remainder.

**Code locations:**

- `BrowserGameEngine.StatefulGameServer/Repositories/Resources/ColonizeRepositoryWrite.cs`
  - Drop the `1..24` argument check; require `Amount ≥ 1`.
  - Add `public static decimal GetCostForTiles(decimal currentLand, int amount)` implementing the formula above.
  - `Colonize(ColonizeCommand)`:
    1. Compute `totalCost = GetCostForTiles(currentLand, command.Amount)`.
    2. Call `resourceRepositoryWrite.DeductCost(playerId, mineralsId, totalCost)` — throws `CannotAffordException` if insufficient (existing behaviour).
    3. Add `command.Amount` land.
  - `GetCostPerLand(currentLand)` stays — used by `/api/resources` for the Base page hint.

### Server API

`POST /api/colonize/colonize?amount=N` — unchanged signature; server-side range check loosened to `N ≥ 1`.

**New:** `GET /api/colonize/preview?amount=N` returning:

```json
{
  "amount": N,
  "totalCost": <decimal>,
  "firstTileCost": <decimal>,
  "lastTileCost": <decimal>,
  "maxAffordable": <int>
}
```

- `firstTileCost = max(1, L/4)`, `lastTileCost = max(1, (L + N − 1)/4)`.
- `maxAffordable` = largest M such that `GetCostForTiles(L, M) ≤ minerals`. Solved via the quadratic inverse plus a one-step floor adjustment for the floor case.
- Endpoint is read-only and cheap; no rate limiting required beyond standard auth.

The endpoint keeps the cost formula authoritative on the server. The client never re-implements it.

`PlayerResourcesViewModel.ColonizationCostPerLand` is **unchanged** and still reflects "next single tile" cost (`GetCostPerLand(currentLand)`), keeping the Base-page hint and the polled `/api/resources` payload lean.

### UI — `ColonizeDialog.tsx`

Replace the current stepper-only control with a slider + stepper combo:

1. **Title + description** — unchanged.
2. **Slider** with range `[1, maxAffordable]`. Logarithmic scale when `maxAffordable > 100`, linear otherwise.
3. **`NumberStepper`** — same component, `max={maxAffordable}`, no presets array. Slider and stepper share the `amount` state; either control updates the other.
4. **"Max affordable (N)"** ghost button (kept) — sets `amount = maxAffordable`.
5. **Cost panel** (replaces the flat "Total" row):
   ```
   Total                    <minerals icon> 4,200
   Per tile        25 → 47  (avg 35)
   ```
   Border tint stays green (affordable) / red (not affordable).
6. **Cancel / Colonize +N** buttons — unchanged.

Data flow:
- On dialog open and on every `amount` change (debounced ~150 ms), the dialog calls `/api/colonize/preview?amount=<amount>`. The response drives Total, per-tile range, affordability, and `maxAffordable`.
- While debouncing, the panel shows the previous response's numbers (no flicker to zero).
- Drop the `PRESETS` array and `MAX_PER_CALL` constant.

### Bot

`BrowserGameEngine.BalanceSim/GameSim/Bots/ConfigurableBot.cs:93` — the bot currently clamps `amount` to 24 and uses flat-batch math. Update to:

1. Compute `maxAffordableUnderReserve = largest M such that GetCostForTiles(L, M) ≤ (minerals − MineralReserve)`.
2. Cap by `LandTarget − currentLand`.
3. If `M ≥ 1`, call `Colonize(new ColonizeCommand(playerId, M))`; else skip.

This removes the bot's per-tick 24-tile cap. The bot still converges to `LandTarget`; the path differs slightly because each colonize call now pays the smooth integral instead of the discrete-batch flat rate.

## Trade-offs and alternatives considered

- **Inline the formula in TypeScript** instead of adding a `/preview` endpoint — rejected. The formula is something we expect to tune; duplicating it across server and client invites drift. Single round-trip per slider drag is cheap.
- **Soft cap (e.g. max +50% of current land per call)** — rejected. The quadratic cost is already sharply self-regulating; a soft cap adds mechanic complexity without solving a real problem.
- **Time/rate cooldown between calls** — rejected. Different mechanic, more invasive, not motivated by the user's stated goal.

## Tests

Add unit tests in `BrowserGameEngine.StatefulGameServer.Test`:

- `GetCostForTiles`: closed-form correctness for `L ≥ 4`; floor handling for `L = 0..3`; `N = 1` matches `GetCostPerLand`; large-N stays positive and monotonic.
- `Colonize`: succeeds for `N` well above 24; throws on insufficient minerals without mutating state; deducts the cumulative cost (not `N · GetCostPerLand(L)`).
- Preview endpoint: `maxAffordable` round-trips — `GetCostForTiles(L, maxAffordable) ≤ minerals < GetCostForTiles(L, maxAffordable + 1)`.

## Migration / compatibility

- No persisted state changes. `WorldState` and `GlobalState` schemas unaffected.
- API: `POST /api/colonize/colonize?amount=N` widens the accepted range; existing clients that only send `1..24` keep working. The new `GET /api/colonize/preview` is additive.
- The `colonizationCostPerLand` field on `PlayerResourcesViewModel` is unchanged and still consumed by the Base-page hint at `Base.tsx:390`.
