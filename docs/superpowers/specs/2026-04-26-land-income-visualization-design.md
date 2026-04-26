# Land → Income Visualization

**Date:** 2026-04-26
**Status:** Approved

## Problem

Players see Land as just a number on the resource bar. Nothing in the UI explains
that Land scales worker efficiency — so they undervalue colonization and don't
know whether their worker assignment is well-matched to their territory.

## Goals

Surface the Land → income relationship in two places:

1. **Always visible** — passive nudge on the Base page Land card.
2. **Decision-time** — live income preview in the Worker Assignment dialog.

Keep it simple: no formulas, no percentages on the Land card; ratios and
sweet-spot targets only.

Out of scope: changing balance, new tutorials, dedicated economy page.

## Design

### Backend (single source of truth)

Constants currently live as private fields in
`StatefulGameServer/GameTicks/Modules/ResourceGrowthSco.cs`. Extract them into a
public type so the API layer can read them without re-declaring values.

**New type** — `BrowserGameEngine.Shared.EconomyFormulaViewModel`:

```csharp
public record EconomyFormulaViewModel {
    public required decimal BaseIncomePerTick { get; init; }              // 10
    public required decimal MaxIncomePerWorker { get; init; }             // 4
    public required decimal MineralSweetSpotLandPerWorker { get; init; }  // 3
    public required decimal GasSweetSpotLandPerWorker { get; init; }      // 6
    public required decimal MinEfficiency { get; init; }                  // 0.002
}
```

Sweet-spot values are derived from the existing efficiency factors:
`sweetSpot = factor × efficiencyMax = factor × 100`.

**Wiring** — extend `PlayerResourcesViewModel` with a `Formula` property and
populate it in `ResourcesController.Get`. No new endpoint; the React client
already polls `/api/resources` from both surfaces.

### Frontend

**Shared helper** — `src/ReactClient/src/lib/economy.ts`:

```ts
export interface EconomyFormula {
  baseIncomePerTick: number
  maxIncomePerWorker: number
  mineralSweetSpotLandPerWorker: number
  gasSweetSpotLandPerWorker: number
  minEfficiency: number
}

export function workerIncome(workers: number, land: number,
                             sweetSpot: number, formula: EconomyFormula): number
export function landPerWorker(workers: number, land: number): number
```

The income formula mirrors `ResourceGrowthSco.CalculateWorkerIncome`:

```
if (workers === 0) return baseIncomePerTick
ratio = land / (workers × sweetSpot)
eff   = clamp(ratio, minEfficiency, 1)
return baseIncomePerTick + workers × maxIncomePerWorker × eff
```

**Land card** (`Base.tsx`, `EconomyStat` for Land):
- `detail` line: `1.5 / mineral worker · 3.0 / gas worker  ·  sweet 3 / 6`
- Existing `colonizationCostPerLand` line moves to the Colonize button tooltip.

**Worker Assignment dialog** (`WorkerAssignmentDialog.tsx`):
- Add a preview block below the sliders:
  ```
  Income next tick · Land 30
    Minerals   +50
    Gas        +30
  ```
- Tip line appears only when **any pool with workers** has
  `land / workers < sweetSpot / 2`. Muted color, not warning.
- Special case: a pool with 0 workers shows just `+10` (the base) and never
  triggers the tip.

## Testing

- **C#** — regression test in `StatefulGameServer.Test` that the
  `EconomyFormulaViewModel` values surfaced via `/api/resources` match the
  constants used by `ResourceGrowthSco`. Guards against drift if someone edits
  one but not the other.
- **TypeScript** — Vitest unit test for `workerIncome` and `landPerWorker`
  against a small truth table matching the C# reference behavior (zero workers,
  below sweet spot, at sweet spot, above sweet spot, floor case).
- No Playwright changes; the additions are purely informational.

## Files Touched

- `src/BrowserGameEngine.Shared/EconomyFormulaViewModel.cs` (new)
- `src/BrowserGameEngine.Shared/PlayerResourcesViewModel.cs` (add `Formula`)
- `src/BrowserGameEngine.GameDefinition.SCO/EconomyFormulaConstants.cs` (new)
- `src/BrowserGameEngine.StatefulGameServer/GameTicks/Modules/ResourceGrowthSco.cs` (use the constants)
- `src/BrowserGameEngine.FrontendServer/Controllers/ResourceController.cs` (populate)
- `src/BrowserGameEngine.StatefulGameServer.Test/EconomyFormulaTest.cs` (new)
- `src/ReactClient/src/api/types.ts` (add types)
- `src/ReactClient/src/lib/economy.ts` (new)
- `src/ReactClient/src/lib/economy.test.ts` (new)
- `src/ReactClient/src/pages/Base.tsx` (Land card detail)
- `src/ReactClient/src/components/WorkerAssignmentDialog.tsx` (preview)
