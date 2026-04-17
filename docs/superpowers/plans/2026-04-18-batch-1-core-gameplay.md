# Batch 1 — Core Gameplay Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the five core-gameplay pages (`Base`, `Units`, `Research`, `Market`, `Trade`) to consume the design-system foundation shipped in PR #315. Page logic (queries, mutations, effects) is preserved; markup is rewritten to use the new primitives. Two new shared components are extracted (`UpgradeTrack`, `ConvertResourceForm`).

**Architecture:** Each page gets a mechanical pass: `<h1>` → `PageHeader`, `<div className="rounded-lg border bg-card p-4">` → `Section`/`Card`, hand-rolled `<table>` → `DataTable`, inline `<button>` → `Button`, status spans → `Badge`, inline success banners → `sonner` toast, hand-rolled modal/form → Radix `Dialog` + `react-hook-form`/`zod`. Extract two shared components so Base can drop the Trade block and Market can gain a Convert tab in the same commit window.

**Tech Stack:** React 19, TanStack Query v5, TanStack Table v8, shadcn/ui (already installed), react-hook-form + zod, sonner, lucide-react.

**Scope:** Five pages plus two new shared components. No backend changes. No refactor of pages outside the five.

**Parent spec:** `docs/superpowers/specs/2026-04-17-ui-ux-improvements-design.md` (Batch 1 section).

**Foundation PR that unlocked this work:** #315 (merged, commit `de444da`).

---

## Pre-flight

- Working directory for all tasks: `/tmp/bge-work/batch-1-core-gameplay`. Worktree is already created on branch `batch-1-core-gameplay` off `origin/master` (HEAD `de444da`).
- `node` comes from nvm; prepend `/home/chris/.nvm/versions/node/v24.12.0/bin` to `PATH` at the top of every shell invocation.
- All `npm`/`npx` commands run from `src/ReactClient/`. All `dotnet` commands run from `src/`.
- **Do NOT restructure primitives themselves.** If a primitive's prop shape doesn't fit a page's need, pass the data differently or add a small wrapper, don't edit `src/components/ui/*`.
- **Preserve all existing `useQuery`/`useMutation` calls verbatim** — keep keys, endpoints, invalidations, `onSuccess`/`onError` handlers. Only the markup changes.
- Existing pages have zero `data-testid` attributes (verified). Don't add new ones unless a test starts failing for a text/role selector.

---

### Task 1: Extract `UpgradeTrack` component

**Files:** Create `src/ReactClient/src/components/UpgradeTrack.tsx`.

Rationale: `Research.tsx` currently contains two nearly identical 60-line blocks (Attack and Defense). Extract into one component before refactoring Research itself so the page becomes trivial afterwards.

- [ ] **Step 1: Read the source blocks**

```bash
sed -n '60,190p' src/ReactClient/src/pages/Research.tsx
```

Understand: `upgrades.attackLevel`, `upgrades.defenseLevel`, `upgrades.maxUpgradeLevel`, `upgrades.upgradeBeingResearched` (string sentinel `'None'|'Attack'|'Defense'`), `upgrades.upgradeResearchTimer` (ticks), `upgrades.attackUpgradeCost.cost`, `upgrades.defenseUpgradeCost.cost`, and the `upgradeMutation` shape.

- [ ] **Step 2: Create `src/ReactClient/src/components/UpgradeTrack.tsx`**

```tsx
import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Ticks } from '@/components/ui/ticks'
import { CostBadge } from '@/components/CostBadge'
import { SectionHeader } from '@/components/ui/section'
import type { ReactNode } from 'react'

type UpgradeType = 'Attack' | 'Defense'

interface UpgradeTrackProps {
  type: UpgradeType
  currentLevel: number
  maxLevel: number
  upgradeBeingResearched: string // 'None' | 'Attack' | 'Defense'
  researchTimer: number // ticks remaining; 0 when nothing in progress
  /** Cost object for the NEXT level, or null if at max. */
  nextCost: Record<string, number> | null
  /** True when this type's next upgrade cannot be afforded. */
  canAfford: boolean
  isResearching: boolean // true when upgradeMutation is in flight
  onResearch: () => void
  /** Header slot — typically a short description of what the track does. */
  description?: ReactNode
}

export function UpgradeTrack({
  type,
  currentLevel,
  maxLevel,
  upgradeBeingResearched,
  researchTimer,
  nextCost,
  canAfford,
  isResearching,
  onResearch,
  description,
}: UpgradeTrackProps) {
  const inProgressHere = upgradeBeingResearched === type
  const otherInProgress = upgradeBeingResearched !== 'None' && !inProgressHere
  const atMax = currentLevel >= maxLevel
  const disabled = atMax || otherInProgress || !canAfford || isResearching
  let disabledReason: string | undefined
  if (atMax) disabledReason = 'At max level'
  else if (otherInProgress) disabledReason = 'Another upgrade in progress'
  else if (!canAfford) disabledReason = 'Cannot afford'

  return (
    <section className="mb-6">
      <SectionHeader title={`${type} upgrades`} description={description} />
      <Card>
        <CardContent className="p-4 space-y-4">
          {/* Level chain */}
          <div className="flex items-center gap-2 flex-wrap">
            {Array.from({ length: maxLevel }).map((_, i) => {
              const level = i + 1
              const done = level <= currentLevel
              const current = level === currentLevel + 1 && inProgressHere
              return (
                <div key={level} className="flex items-center gap-2">
                  <div className={cn(
                    'flex flex-col items-center justify-center w-16 h-16 rounded-md border text-xs',
                    done && 'bg-success/20 border-success/40',
                    current && 'bg-primary/10 border-primary',
                    !done && !current && 'bg-card border-border text-muted-foreground',
                  )}>
                    <span className="label leading-none">Lv</span>
                    <span className="mono text-lg font-bold leading-tight">{level}</span>
                    {done && <Badge variant="secondary" className="mt-0.5 text-[9px] px-1 py-0">✓</Badge>}
                  </div>
                  {level < maxLevel && <span aria-hidden className="text-muted-foreground">→</span>}
                </div>
              )
            })}
          </div>

          {/* In-progress bar */}
          {inProgressHere && researchTimer > 0 && (
            <div className="space-y-1">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Researching level {currentLevel + 1}…</span>
                <Ticks n={researchTimer} />
              </div>
              <Progress value={Math.max(0, 100 - (researchTimer / (researchTimer + 1)) * 100)} />
            </div>
          )}

          {/* Research action */}
          {!atMax && (
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Next:</span>
                {nextCost && <CostBadge cost={nextCost} />}
              </div>
              <Button
                variant="default"
                size="sm"
                onClick={onResearch}
                disabled={disabled}
                title={disabledReason}
              >
                {inProgressHere ? 'Researching…' : 'Research'}
              </Button>
            </div>
          )}
          {atMax && (
            <div className="text-sm text-muted-foreground">At maximum level ({maxLevel}).</div>
          )}
        </CardContent>
      </Card>
    </section>
  )
}
```

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/components/UpgradeTrack.tsx
git commit -m "feat(ui): add UpgradeTrack component for Research"
```

---

### Task 2: Extract `ConvertResourceForm` component

**Files:** Create `src/ReactClient/src/components/ConvertResourceForm.tsx`.

Rationale: Base's inline 2-way resource-swap block gets removed in Task 5. Market's new Convert tab (Task 6) needs to host it. Extract first.

- [ ] **Step 1: Read Base's current Trade block**

```bash
sed -n '240,300p' src/ReactClient/src/pages/Base.tsx
```

Capture the mutation shape (key, endpoint, invalidations), the state (`tradeFromResource`, `tradeAmount`, `tradeError`), and the `TradeResourceRequest` type used.

- [ ] **Step 2: Create `src/ReactClient/src/components/ConvertResourceForm.tsx`**

The component owns its local state + mutation. It takes `gameId` as a prop and calls the same endpoint the Base block calls. Use the Base block as the source of truth for the endpoint URL and request shape — do not invent names.

```tsx
import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import apiClient from '@/api/client'
import type { TradeResourceRequest } from '@/api/types'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ArrowRightLeftIcon } from 'lucide-react'
import { SectionHeader } from '@/components/ui/section'

export function ConvertResourceForm({ gameId }: { gameId: string }) {
  const qc = useQueryClient()
  const [from, setFrom] = useState<'minerals' | 'gas'>('minerals')
  const [amount, setAmount] = useState<number>(100)

  const mutation = useMutation({
    mutationFn: (payload: TradeResourceRequest) =>
      apiClient.post('/api/resources/trade', payload).then((r) => r.data),
    onSuccess: () => {
      toast.success('Resources converted')
      qc.invalidateQueries({ queryKey: ['resources', gameId] })
      setAmount(100)
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Conversion failed'
      toast.error(msg)
    },
  })

  const flip = () => setFrom((f) => (f === 'minerals' ? 'gas' : 'minerals'))
  const to = from === 'minerals' ? 'gas' : 'minerals'

  return (
    <section className="mb-6">
      <SectionHeader title="Convert resources" description="Swap minerals for gas or vice-versa at the current market rate." />
      <Card>
        <CardContent className="p-4 space-y-3 max-w-lg">
          <div className="grid grid-cols-[1fr_auto_1fr] items-end gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="convert-amount">Amount of {from}</Label>
              <Input
                id="convert-amount"
                type="number"
                min={1}
                value={amount}
                onChange={(e) => setAmount(Number(e.target.value))}
              />
            </div>
            <Button type="button" variant="ghost" size="icon" onClick={flip} aria-label="Swap direction" className="mb-0.5">
              <ArrowRightLeftIcon className="h-4 w-4" />
            </Button>
            <div className="space-y-1.5">
              <Label className="text-muted-foreground">→ {to}</Label>
              <div className="h-9 flex items-center px-3 rounded-md border bg-muted/30 text-sm text-muted-foreground mono">
                (market rate applied)
              </div>
            </div>
          </div>
          <Button
            variant="default"
            size="sm"
            disabled={mutation.isPending || amount <= 0}
            onClick={() => mutation.mutate({ fromResource: from, amount })}
          >
            {mutation.isPending ? 'Converting…' : `Convert ${from} → ${to}`}
          </Button>
        </CardContent>
      </Card>
    </section>
  )
}
```

**If `TradeResourceRequest` in `@/api/types` does not exactly match `{ fromResource: string, amount: number }`, adapt the mutation's payload shape to match the real type. Do not rename fields.**

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/components/ConvertResourceForm.tsx
git commit -m "feat(ui): extract ConvertResourceForm for Market's Convert tab"
```

---

### Task 3: Refactor `Research.tsx`

**Files:** Modify `src/ReactClient/src/pages/Research.tsx`.

- [ ] **Step 1: Read the current file end-to-end**

```bash
cat src/ReactClient/src/pages/Research.tsx
```

Preserve the `useQuery` (key `['research', gameId]`), the `upgradeMutation` and its `onSuccess` invalidation, and the `PageLoader`/`ApiError` early returns.

- [ ] **Step 2: Rewrite the page body**

Keep the imports for `useQuery`, `useMutation`, `useQueryClient`, `apiClient`, `PageLoader`, `ApiError`, plus the view-model type. Replace the two giant upgrade-block loops and the `<h1>` with:

```tsx
import { PageHeader } from '@/components/ui/page-header'
import { UpgradeTrack } from '@/components/UpgradeTrack'
// ... existing imports preserved

export function Research({ gameId }: { gameId: string }) {
  // ... existing useQuery + ApiError / PageLoader boilerplate preserved ...

  const upgradeMutation = useMutation(/* existing — unchanged */)

  // Guard against intermediate loading/error states — PageLoader/ApiError already return early above.
  const upgrades = data!

  return (
    <>
      <PageHeader title="Research" description="Upgrade your units' combat capabilities." />
      <UpgradeTrack
        type="Attack"
        currentLevel={upgrades.attackLevel}
        maxLevel={upgrades.maxUpgradeLevel}
        upgradeBeingResearched={upgrades.upgradeBeingResearched}
        researchTimer={upgrades.upgradeResearchTimer}
        nextCost={upgrades.attackUpgradeCost?.cost ?? null}
        canAfford={upgrades.canAffordAttackUpgrade}
        isResearching={upgradeMutation.isPending}
        onResearch={() => upgradeMutation.mutate({ upgradeType: 'Attack' })}
        description="Increase attack damage of all units."
      />
      <UpgradeTrack
        type="Defense"
        currentLevel={upgrades.defenseLevel}
        maxLevel={upgrades.maxUpgradeLevel}
        upgradeBeingResearched={upgrades.upgradeBeingResearched}
        researchTimer={upgrades.upgradeResearchTimer}
        nextCost={upgrades.defenseUpgradeCost?.cost ?? null}
        canAfford={upgrades.canAffordDefenseUpgrade}
        isResearching={upgradeMutation.isPending}
        onResearch={() => upgradeMutation.mutate({ upgradeType: 'Defense' })}
        description="Increase defense of all units."
      />
    </>
  )
}
```

**If the view model property names differ** (`attackLevel` vs `attackUpgradeLevel`, or `canAffordAttackUpgrade` vs `canAffordAttack`), use the real names — do not invent. `grep -A3 'upgrades.attackLevel\|upgrades.attack' src/ReactClient/src/pages/Research.tsx` confirms the original file's names.

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/pages/Research.tsx
git commit -m "feat(ui): Research uses UpgradeTrack + Progress + Ticks"
```

---

### Task 4: Refactor `Units.tsx`

**Files:** Modify `src/ReactClient/src/pages/Units.tsx`.

- [ ] **Step 1: Read the current file**

```bash
cat src/ReactClient/src/pages/Units.tsx
```

Preserve: `useQuery` (`['units', gameId]`), `mergeAllMutation`, the `AttackDialog` component reference, `SplitUnitForm` (keep it for now — inline row expansion is acceptable).

- [ ] **Step 2: Replace the `<h1>Units</h1>` block and both tables**

Plan: keep `BuildQueue` above the content. Use two `DataTable`s — one for "Home Base" units, one for "Deployed" units. Wrap each in a `Section`. Use a summary `<Stat>` row above both tables. Replace all inline `<button>` + status spans with `<Button>` + `<Badge>`. Replace emoji stats with plain `<Stat>` cells.

```tsx
import { useMemo } from 'react'
import type { ColumnDef } from '@tanstack/react-table'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { DataTable } from '@/components/ui/data-table'
import { Stat } from '@/components/ui/stat'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { formatNumber } from '@/lib/formatters'
// ... existing imports preserved (types, hooks, existing `SplitUnitForm`, `AttackDialog`)
```

- `PageHeader` top with `actions={<Button onClick={() => mergeAllMutation.mutate()} disabled={mergeAllMutation.isPending || !canMerge}>Merge All</Button>}`.
- `<BuildQueue gameId={gameId} />` remains where it is.
- A `<Section>` wrapping a `Stat` grid: Total Units, Deployed, At Home, In Combat (derive from `data.units`).
- Home base `DataTable` — columns: Unit (with icon), Count (right, `mono tabular-nums`), Attack/Def/HP (compact `<Stat>` cells with tiny labels), Actions (Attack/Split/Merge `<Button>`s). Use `variant="destructive" size="sm"` for Attack, `variant="secondary" size="sm"` for Split/Merge.
- Deployed `DataTable` — same columns + a "Location" column showing `<Badge variant="destructive">In Combat</Badge>` or `<Badge variant="success">Defending</Badge>`. Actions: Cancel (`<Button variant="destructive" size="sm">`).
- Inline `SplitUnitForm` remains rendered by a row-expander pattern — the row's Split button toggles a per-row `expanded` state, and the form renders in a `colSpan={N}` row beneath. This is pragmatic — a later pass can move it to a `Dialog`.

**If `DataTable` doesn't support row-expansion out of the box** (it doesn't in the foundation PR), render the split form inline by making the Split button toggle a local state keyed by `unitId`, and render the form in a separate `Section` below the table ("Splitting: Marines (5)") rather than an in-row expander. Keep it simple.

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/pages/Units.tsx
git commit -m "feat(ui): Units uses DataTable + Button + Badge + Stat"
```

---

### Task 5: Refactor `Base.tsx`

**Files:** Modify `src/ReactClient/src/pages/Base.tsx`.

Largest page. Does three things: drops the inline Trade block (now handled by Market's Convert tab), reorders tabs, migrates all markup to primitives.

- [ ] **Step 1: Read the current file**

```bash
cat src/ReactClient/src/pages/Base.tsx
```

- [ ] **Step 2: Rewrite**

Preserve: all `useQuery` / `useMutation` calls for assets, build queue, worker assignment, and colonize. Preserve the URL-param tab sync logic. Drop: the `TradeResourceRequest` import, the `tradeFromResource`/`tradeAmount`/`tradeError` state, the `tradeMutation`, and lines 250–279 (the Trade block UI).

Key changes:

- Replace `<h1>Base</h1>` with `<PageHeader title="Base" />`.
- Replace the yellow "Game Over" div with `<Section>` containing a warning `<Badge>` + message.
- Replace `TabButton` + hand-rolled tab bar with shadcn `<Tabs>` / `<TabsList>` / `<TabsTrigger>` / `<TabsContent>`. Preserve the URL search-param sync — wire `value={currentTab}` and `onValueChange={setTab}` where `setTab` updates `searchParams`.
- **Tab order per spec:** `build` (Assets + BuildQueue), `economy` (WorkerAssignment + Colonize), `activity`. `build` becomes the new default.
- `AssetCard`: wrap outer div in `<Card>`, replace both inline `<button>`s with `<Button variant="default" size="sm">Build</Button>` and `<Button variant="secondary" size="sm">Queue</Button>`. Replace "Built ✓" text with `<Badge variant="secondary">Built</Badge>` (use success if the Badge primitive has a success variant; otherwise secondary + success-toned className).
- Colonize form: wrap in `<Section>`, replace bare `<input>` with `<Input>`, bare `<button>` with `<Button>`, the inline error text with `toast.error(...)` on mutation failure.
- Activity tab: wrap in `<Section>` with a `<Button variant="ghost" size="sm">Clear</Button>` in the `actions` slot.
- Remove the entire Trade block (lines ~250–279 of the original file).
- The `ResourceHistoryChart` Suspense block stays in a `<Section title="Resource history">`.

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/pages/Base.tsx
git commit -m "feat(ui): Base uses Tabs + Card + Button; drop Trade block"
```

---

### Task 6: Refactor `Market.tsx`

**Files:** Modify `src/ReactClient/src/pages/Market.tsx`.

Must consume `ConvertResourceForm` from Task 2.

- [ ] **Step 1: Read the current file**

```bash
cat src/ReactClient/src/pages/Market.tsx
```

Preserve: `useQuery` (`['market', gameId]`), `postOrderMutation`, `cancelOrderMutation`, `acceptOrderMutation`, the `useConfirm` context usage for Cancel.

- [ ] **Step 2: Rewrite with three tabs**

- `<PageHeader title="Market" description="Post or accept resource trades with other players." />`
- `<Tabs defaultValue="buy">` containing three `<TabsContent>`:
  - **Buy**: `DataTable` of `otherOrders` (derived from `data.openOrders.filter(o => o.playerId !== currentPlayerId)`). Columns: Player, Offering, Wants, Rate, Posted, Actions. Actions column renders `<Button variant="default" size="sm" onClick={accept}>Accept</Button>` with a `<Tooltip>` on the disabled state showing the reason from `orderErrors[order.orderId]` (tooltip trigger wraps the button; use the `disabled` prop but render the button always).
  - **Sell**: upper section `<Section title="My offers">` with a `DataTable` of `myOrders`. Actions column: `<Button variant="destructive" size="sm" onClick={() => cancel(order.orderId)}>Cancel</Button>` (the existing `useConfirm` call is preserved). Lower section `<Section title="Post a new offer">` with a `react-hook-form` + `zod` form: `offeringResourceId` `<Select>`, `offeringAmount` `<Input type="number">`, `wantedResourceId` `<Select>`, `wantedAmount` `<Input type="number">`. Submit button `<Button>Post offer</Button>`. On success: `toast.success('Offer posted')`. On error: `toast.error(msg)`.
  - **Convert**: `<ConvertResourceForm gameId={gameId} />`.
- Delete the `postError` state and `orderErrors` state — both are replaced by toasts and tooltips.
- The loading state uses `DataTable`'s built-in `loading` prop with `<Skeleton>` rows.

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/pages/Market.tsx
git commit -m "feat(ui): Market uses Tabs (Buy/Sell/Convert) + DataTable + form"
```

---

### Task 7: Refactor `Trade.tsx`

**Files:** Modify `src/ReactClient/src/pages/Trade.tsx`.

- [ ] **Step 1: Read the current file**

```bash
cat src/ReactClient/src/pages/Trade.tsx
```

Preserve: both queries (`marketData` for resources, `playersResp` for players — keep endpoint paths and keys verbatim), `sendMutation`, `acceptMutation`, `declineMutation`, `cancelMutation`, the `useConfirm` usage, the `resourceName` helper, and `relativeTime`.

- [ ] **Step 2: Rewrite**

- `<PageHeader title="Trade" description="Propose direct trades with other players." actions={<Button onClick={() => setDialogOpen(true)}>Propose trade</Button>} />`.
- Propose-trade form moves into a Radix `<Dialog>` opened by the header button. Use `react-hook-form` + `zod` with `<Field>` for each input. On success: `toast.success('Trade offer sent')`, close dialog, reset form. On error: `toast.error(msg)`.
- Body has three `<Tabs>`: Incoming / Sent / History. Each contains a `DataTable`.
  - Incoming columns: From, Offering, Wants, Expires (`<Countdown>`), Note, Actions (`<Button variant="default" size="sm">Accept</Button>`, `<Button variant="destructive" size="sm">Decline</Button>`).
  - Sent columns: To, Offering, Wants, Expires, Note, Actions (`<Button variant="destructive" size="sm">Cancel</Button>` with `useConfirm`).
  - History columns: Counterparty, Offering, Wants, Status (`<Badge variant={...}>`), Settled (`relativeTime`).
- Delete the `success` and `error` inline state variables and their banner divs. All feedback now via sonner.

- [ ] **Step 3: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/pages/Trade.tsx
git commit -m "feat(ui): Trade uses Dialog for propose + Tabs + DataTable + toasts"
```

---

### Task 8: Verification sweep

**Files:** none changed (unless an issue surfaces).

- [ ] **Step 1: Lint**

```bash
cd src/ReactClient && npm run lint 2>&1 | tail -20
```

Zero errors. If a refactored page has a warning we introduced (e.g. an unused variable left over from removing the Trade block in Base), fix it and amend the relevant task's commit — **NO, don't amend**; make a separate `fix(ui): <what>` commit.

- [ ] **Step 2: Vite build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 3: Vitest**

```bash
cd src/ReactClient && npm run test
```

Still 10/10 (foundation tests). No new tests added in Batch 1 — we're refactoring markup, not adding logic.

- [ ] **Step 4: dotnet build + tests**

```bash
cd /tmp/bge-work/batch-1-core-gameplay/src
dotnet build --configuration Release 2>&1 | tail -5
dotnet test --verbosity minimal 2>&1 | tail -10
```

Green.

- [ ] **Step 5: Red-flag greps**

```bash
cd /tmp/bge-work/batch-1-core-gameplay

# Any inline button class strings left on refactored pages?
grep -nE 'rounded.*bg-primary.*px-[0-9]' src/ReactClient/src/pages/{Base,Units,Research,Market,Trade}.tsx

# Any <table> left on refactored pages?
grep -nE '<table' src/ReactClient/src/pages/{Base,Units,Research,Market,Trade}.tsx

# Any inline success/error banner state?
grep -nE "const \[success|const \[postError|const \[tradeError" src/ReactClient/src/pages/{Base,Units,Research,Market,Trade}.tsx

# "rounds" terminology on refactored pages?
grep -nw 'rounds\?' src/ReactClient/src/pages/{Base,Units,Research,Market,Trade}.tsx
```

All should return empty. If any line appears, open the file and fix it in a `fix(ui): <what>` commit.

- [ ] **Step 6: Manual smoke (optional)**

Start the backend + Vite if you want to load pages in a browser. Not a blocker — CI covers it.

---

### Task 9: Commit the plan + push + open PR

**Files:** `docs/superpowers/plans/2026-04-18-batch-1-core-gameplay.md`.

- [ ] **Step 1: Commit this plan as the final commit on the branch**

```bash
cd /tmp/bge-work/batch-1-core-gameplay
git add docs/superpowers/plans/2026-04-18-batch-1-core-gameplay.md
git commit -m "docs: add Batch 1 implementation plan"
```

(Committing at the end rather than the beginning lets the branch land as a coherent unit without reviewers having to jump between plan and code.)

- [ ] **Step 2: Push**

```bash
git push -u origin batch-1-core-gameplay
```

- [ ] **Step 3: Open PR**

```bash
gh pr create --title "feat(ui): Batch 1 — core gameplay pages refactored to design-system primitives" --body "$(cat <<'EOF'
## Summary
- Refactors the five core-gameplay pages (\`Base\`, \`Units\`, \`Research\`, \`Market\`, \`Trade\`) to consume the design-system foundation from PR #315. Page logic (queries, mutations, effects) preserved — only markup changes.
- Extracts two shared components: \`UpgradeTrack\` (consumed by Research) and \`ConvertResourceForm\` (consumed by Market's new Convert tab).
- **Base**: tab order reorganised (Build first, then Economy, then Activity); inline 2-way resource swap block removed (moved to Market's Convert tab); \`PageHeader\`/\`Card\`/\`Button\`/\`Badge\` throughout.
- **Units**: two \`DataTable\`s (Home Base, Deployed) + \`Stat\` summary cells + \`Badge\` for combat status; emoji stats (⚔️🛡️❤️) replaced with \`Stat\` cells.
- **Research**: two duplicated 60-line blocks collapsed into two \`<UpgradeTrack>\` instances (Attack / Defense); \`<Progress>\` + \`<Ticks>\` for in-progress research.
- **Market**: three \`<Tabs>\` — Buy (other players' orders) / Sell (my orders + post-offer form) / Convert (ConvertResourceForm). \`DataTable\` listings, \`<Tooltip>\` on disabled Accept buttons, sonner toasts replacing inline error banners.
- **Trade**: Propose-trade form lifted into a Radix \`<Dialog>\`. Incoming / Sent / History as three \`<Tabs>\`, each a \`DataTable\`. Inline success/error banners replaced with sonner toasts.

## Non-changes
Backend API unchanged. Primitives in \`src/components/ui/\` unchanged. No page outside the five refactored.

## Parent spec + plan
- Spec: \`docs/superpowers/specs/2026-04-17-ui-ux-improvements-design.md\` (Batch 1 section).
- Plan: \`docs/superpowers/plans/2026-04-18-batch-1-core-gameplay.md\`.

## Test plan
- [x] \`npm run build\` green.
- [x] \`npm run lint\` clean.
- [x] \`npm run test\` (vitest) 10/10 (no new tests — markup-only refactor).
- [x] \`dotnet build --configuration Release\` clean.
- [x] \`dotnet test\` green.
- [ ] \`npm run test:e2e\` deferred to CI (Playwright in docker-compose).
- [ ] Manual smoke: log in via DevAuth, touch each of the 5 pages, verify core actions (build, research, post order, accept order, propose trade) still work end-to-end.

## Follow-ups not landing here
- \`useTickDuration\` still hardcoded to 30s.
- Per-tick deltas on \`PlayerResources\`.
- Units \`SplitUnitForm\` lives in a \`<Section>\` below the table; a Dialog-based split is a nicer UX but out of scope for this PR.
- Research \`<Progress>\` bar uses a best-effort value until the backend exposes total research duration.
EOF
)"
```

---

## Self-Review

**Spec coverage:**
- Batch 1 section of the spec lists five pages + specific changes. Each is a task:
  - Research → UpgradeTrack + Progress: Tasks 1 + 3. ✅
  - Base reorder + drop Trade block: Task 5. ✅
  - Units DataTable + Stat cells: Task 4. ✅
  - Market Tabs (Buy/Sell/Convert) + DataTable + disabled-reason tooltips: Task 6 (depends on Task 2). ✅
  - Trade Dialog + DataTables + toasts: Task 7. ✅
- Extracted shared components: Tasks 1 (UpgradeTrack) + 2 (ConvertResourceForm). ✅

**Placeholder scan:** no "TBD" / "implement later" / "similar to Task N". Where a plan snippet says "the existing boilerplate preserved", it points the implementer at the real file to read.

**Type consistency:**
- `UpgradeTrack` prop shape (Task 1) matches the call-sites in Research (Task 3). ✅
- `ConvertResourceForm` takes only `gameId` (Task 2) and is called the same way in Market (Task 6). ✅
- Mutation payload `TradeResourceRequest` is taken verbatim from `@/api/types` — implementer verifies. ✅

**Scope check:** one PR covers 5 pages + 2 new components + 1 plan doc. Substantial but bounded, and each task is self-contained. If any task balloons mid-flight, that's a cue to narrow it rather than expand.

**Ambiguity check:**
- Units' `SplitUnitForm` placement: plan allows either in-row expansion (ideal) or a separate section below (pragmatic fallback). Implementer picks based on `DataTable`'s current capabilities.
- Research `Progress` bar value: plan notes the backend doesn't expose total duration; implementer uses a best-effort estimate or indeterminate bar. Explicit.
- Status `<Badge>` variants: shadcn's Badge primitive as installed in the foundation PR has `default|secondary|destructive|outline`. Success/warning are conveyed via custom className (`bg-success/20 text-success`) where the plan calls for them. Mentioned implicitly in the AssetCard "Built" and Units combat-status badges.

---

## Execution Handoff

**Plan complete. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks.

**2. Inline Execution** — execute tasks in this session using executing-plans, batched with checkpoints.

**Which?**
