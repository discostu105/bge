# Base & Units — UX Redesign

**Date:** 2026-04-19
**Project:** Age of Agents (BGE React client)
**Scope:** A focused UX rethink of the two pages the player spends most of their time on: **Base** and **Units**. Mechanics and backend API unchanged.

## Problem

Observed friction on the current Base and Units pages (code at `src/ReactClient/src/pages/Base.tsx`, `Units.tsx`, plus `BuildUnitsForm.tsx`, `WorkerAssignment.tsx`, `BuildQueue.tsx`):

1. **Build vs Queue is confusing.** Both buttons appear next to each other for every asset. The difference ("Build now if affordable vs. add to the queue unconditionally") is never explained and both paths produce similar-looking results. New players don't know which one to press.
2. **Unit-training is hidden inside asset cards.** Built production buildings grow a cramped `<select>` + number input + *two more* buttons (Build, Queue). Four controls inside a 4-line card. Hard to scan; looks like an admin form.
3. **Raw number inputs everywhere.** Count of units to train, split amount, colonize amount — all bare `<input type="number">`. No increment buttons, no presets, no "max affordable" helper.
4. **Unit building requires jumping to Base.** You see an army stat on Units, realise you need more marines, leave the Units page, find the right asset card on Base, dig into its cramped form, train, then navigate back. The page split mirrors the backend split, not the player's mental flow.
5. **Economy tab is under-discoverable.** Workers and Colonize live under a second-tier tab. Colonize in particular is a core expansion mechanic; new players don't find it.
6. **Split uses an inline form that reflows the page.** Clicking Split on a row in the home table opens a separate form section *below* both tables, far from the row that triggered it. Visually disconnected.
7. **Destructive/ambiguous states lack clarity.** Queued assets show "ready in N ticks" but offer no way to cancel from the card (you have to scroll to the queue list). Locked assets don't explain *why* clearly.
8. **Assets are a flat grid of ~10 cards** with no grouping. Built vs not built vs locked are the only states, and they're only distinguishable by border color.

## Goals

- Collapse Build/Queue into one mental model: **the queue is how things get built**. Instant-build is a convenience, not a parallel path.
- Make unit-training accessible from **both** Base and Units via a single reusable dialog. No more context-switching for the "I want more marines" loop.
- Replace every raw number input on these pages with a **stepper + presets + Max-affordable**.
- Surface the **two economy levers** (workers, colonize) without a second-tier tab. They belong on the Base overview.
- Group assets by state so the eye sorts them: **Built → Ready to build → Locked**.
- Keep the persistent `BuildQueue` visible where it matters, and give it an action affordance from the card ("Queued — cancel?") instead of scroll-hunting.
- Everything stays server-authoritative: existing endpoints (`/api/assets/build`, `/api/units/build`, `/api/buildqueue/*`, `/api/workers/*`, `/api/colonize/*`, `/api/units/merge|split`, `/api/battle/*`) are unchanged.

## Non-goals

- No backend/API changes. No new ViewModels.
- No changes to Research, Market, Trade, or any other page.
- No server-side asset categorization (we don't invent a category field just to group visually).
- No mobile-native / touch-specific redesigns beyond keeping existing responsive patterns working.
- No addition of NEW mechanics (auto-train, repeat build, etc.) — just re-expressing what's already there.

## Design

### Reusable primitives (new)

Two small components in `src/components/ui/` and one domain component:

- **`NumberStepper`** (`ui/number-stepper.tsx`): a tabular-numerals number input flanked by `−` / `+` buttons with a row of preset chips underneath (e.g. `1  5  10  25  50  Max`). Props: `value`, `onChange`, `min`, `max`, `presets?`, `onMax?`, `disabled?`. One component, five uses (train unit count, split count, colonize amount, worker minerals, worker gas).
- **`AffordabilityChip`** — small helper wrapping `CostBadge` that adds a tone: green when affordable, red when not. Eliminates the eight places that currently check `canAfford` and style the cost differently.

Domain component (new, shared by Base and Units):

- **`TrainUnitsDialog`** (`components/TrainUnitsDialog.tsx`): a Radix `Dialog` that takes `gameId` and either (a) a specific `AssetViewModel` (pre-selected building) or (b) the full `AssetsViewModel` (user picks building first, from built production buildings only). Internals:
  - Segmented tabs for each `availableUnit` of the chosen building.
  - Per-unit: stat line (Atk / Def / HP / Speed) + `AffordabilityChip` for the **per-unit** cost.
  - `NumberStepper` for count.
  - Live **total cost** with affordability tone ("Total: 250 min, 100 gas" → green/red).
  - Two actions: **Train now** (calls `POST /api/units/build` — only enabled if total cost is affordable) and **Add to queue** (calls `POST /api/buildqueue/add` — always enabled).
  - After either action: invalidate `units`, `buildqueue`, `resources` queries, and close.

### Base page — new structure

`src/pages/Base.tsx` is rewritten. The existing `tab` search-param (`build` / `economy` / `activity`) is kept for deep-linkability but rethought:

```
Base
├─ (no tabs — single page)
├─ Economy strip        — 3 stat cards, always visible
│   • Workers (total + idle)  → "Assign" button → WorkerAssignmentDialog
│   • Land                     → "Colonize" button → ColonizeDialog
│   • Build Queue (N pending)  → scroll to queue
├─ Buildings section
│   • Header:  "Buildings"  + search/filter is *not* added (YAGNI)
│   • Grouped by state:
│     - Built (green-accent header strip)
│     - Ready to build (no strip)
│     - Locked (muted header strip)
│   • Each group is a responsive grid of AssetCards
├─ BuildQueue section   — always visible, sticky on desktop (lg+)
└─ Activity             — collapsed into a disclosable panel ("Show recent activity")
```

The existing `tab` search-param still scrolls-to the right anchor so bookmarked URLs aren't broken: `?tab=economy` scrolls to the economy strip; `?tab=activity` opens the activity panel; `?tab=build` is the default (no scroll).

#### AssetCard — three states, clear affordances

```
BUILT                    READY                    LOCKED / QUEUED
┌────────────────┐       ┌────────────────┐       ┌────────────────┐
│ Barracks ✓     │       │ Factory        │       │ Spaceport 🔒   │
│ [Train units]  │       │ 200 min 100 gas│       │ Requires: …    │
│                │       │ [Build] [Queue]│       │ or             │
└────────────────┘       └────────────────┘       │ Queued · 3t left│
                                                   │ [Cancel]        │
                                                   └────────────────┘
```

- **Built**: green accent border, "✓" badge, **single primary button** `Train units` (only present if the building produces units). Clicking opens `TrainUnitsDialog` with this asset pre-selected.
- **Ready**: neutral border, cost shown with affordability tone, two buttons:
  - `Build` — primary, disabled if not affordable. Calls `/api/assets/build`.
  - `Queue` — secondary, always enabled. Calls `/api/buildqueue/add` with `type='asset'`.
- **Queued**: amber accent, "Queued · ready in Nt", `Cancel` button that finds the queue entry by `defId` and calls `/api/buildqueue/remove`.
- **Locked**: muted, shows `Requires: <prerequisite names>`. No buttons.

#### Workers & Colonize — lightweight dialogs

Rather than the current inline forms, both controls move to small Dialogs triggered from the Economy strip:

- **WorkerAssignmentDialog**: identical logic to the current component, but inputs become `NumberStepper` for both mineral and gas, with a "Balance 50/50" helper preset. Opens from the `Workers` stat card's `Assign` button.
- **ColonizeDialog**: `NumberStepper` (min 1, max 24 — matches the current form), with a computed `Max affordable` preset. Live total cost display. Opens from the `Land` stat card's `Colonize` button.

The existing inline `WorkerAssignment` component is kept but demoted — used only inside the dialog. Net code churn is small.

### Units page — new structure

`src/pages/Units.tsx` gets a companion pattern: "manage what you have, and get more from here too":

```
Units
├─ PageHeader: "Units"
│   ├─ action: [Train units]  → TrainUnitsDialog (opens in "pick a building" mode)
│   └─ action: [Merge all]    — existing, unchanged behaviour
├─ Army overview — 3 Stat cards (Total, At home, Deployed)  (unchanged)
├─ BuildQueue     (unchanged; visible so trained units are easy to track)
├─ Deployed section  — DataTable (unchanged columns)
└─ Home section      — DataTable (changes below)
```

Home section table changes:

- **Row actions reordered** so primary action (`Attack`) is first, followed by `Split`, then `Merge`. `Merge` is hidden for unit types that only have one stack (no-op would fail silently today).
- **Split** switches from an inline form below the tables to a small **SplitDialog** opened from the row button. Contents: unit name, current count, `NumberStepper` (min 1, max `count - 1`, preset `Half`). Single primary action `Split`.
- If the player has `0` units at home and `0` deployed, the empty state gets an action: `Train your first units →` which opens `TrainUnitsDialog`.

Merge-all stays. Per-type merge stays. Attack dialog is unchanged.

### What gets removed

- `src/components/BuildUnitsForm.tsx` — superseded by `TrainUnitsDialog`. Deleted.
- Inline `SplitUnitForm` inside `Units.tsx` — replaced by `SplitDialog` (new, co-located at bottom of `Units.tsx`).
- Inline colonize form in the Economy tab of `Base.tsx` — replaced by `ColonizeDialog`.
- The three `Tabs` on Base — replaced by the stacked layout above.

### What gets reused

- `WorkerAssignment` component — lives on, rendered inside the new `WorkerAssignmentDialog`. One small refactor: its two bare `<input type="number">` become `NumberStepper`.
- `BuildQueue` — unchanged; stays prominent on both Base and Units.
- `CostBadge`, `AttackDialog`, `ResourceIcon`, `Stat`, `Section`, `PageHeader`, `DataTable`, all `ui/*` primitives — unchanged.

### File plan

New:
- `src/components/ui/number-stepper.tsx`
- `src/components/TrainUnitsDialog.tsx`
- `src/components/WorkerAssignmentDialog.tsx`
- `src/components/ColonizeDialog.tsx`
- `src/components/SplitUnitDialog.tsx`
- `src/components/AffordabilityChip.tsx`

Changed:
- `src/pages/Base.tsx` — rewritten structure
- `src/pages/Units.tsx` — header actions, Split dialog, empty-state CTA
- `src/components/WorkerAssignment.tsx` — stepper inputs

Deleted:
- `src/components/BuildUnitsForm.tsx`

### Testing strategy

- `npm test` — type-check and existing Vitest unit tests should pass unchanged.
- `dotnet build` + `dotnet test` — backend untouched; must remain green as a sanity check.
- Existing Playwright e2e tests (`03-gameplay.spec.ts`, `06-attack-flow.spec.ts`, `09-unit-army.spec.ts`) are expected to keep passing; their selectors target `h1` text, `PageHeader` title, and `Stat` labels — all preserved. If any selector breaks because a surface turned into a dialog, update the test minimally.
- Manual browser walkthrough before PR: dev server + Chrome DevTools MCP. User journeys: `build a building`, `train a unit from Base`, `train a unit from Units`, `assign workers`, `colonize land`, `split a stack`, `cancel a queued build`, `attack an enemy`.

### Accessibility

- Every dialog is Radix-based — focus trap, `Escape`, `aria-modal`.
- `NumberStepper` keyboard: `ArrowUp`/`ArrowDown` step, `Home`/`End` clamp to min/max, preset chips are `<button>`s in natural tab order.
- AssetCard state is communicated by text and icon, not color alone.
- Touch targets ≥36px on all new buttons; ≥44px on primary actions.

## Risks

- **Bundle size**: adds ~4 new components. Each is <150 lines, no new deps. Acceptable.
- **Test churn**: inline text "Build Queue" is stable; existing tests reference `h1` and stat labels, both preserved. Low risk.
- **"Tab" deep links**: `?tab=economy` and `?tab=activity` from old bookmarks still work (scroll-to-anchor fallback).
- **Memorability**: players used to the old Build/Queue split may briefly look for the old buttons. Mitigation: both `Build` and `Queue` are still visibly present on "Ready to build" cards; we only removed the confused *embedded* form inside built cards.

## Success criteria

Manual walkthrough in a running dev env can complete each of these in ≤3 clicks from the target page, with no raw number-typing required:

- [ ] From Base: build a building.
- [ ] From Base: queue a building.
- [ ] From Base: cancel a queued building from its card.
- [ ] From Base: train 10 marines in one pass.
- [ ] From Base: colonize land.
- [ ] From Base: re-assign workers.
- [ ] From Units: train units without leaving the page.
- [ ] From Units: split a stack.
- [ ] From Units: attack an enemy.
