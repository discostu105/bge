# UI/UX Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the most painful UX rough edges in the BGE React client, starting with safe polish (Phase 1) and escalating into journey redesigns (Phase 2).

**Architecture:** React 19 + Vite + Tailwind v4 + TanStack Query + Radix UI + Lucide. All changes stay inside `src/ReactClient/`. No server changes. No new dependencies unless a task explicitly calls for one. Each phase is its own PR in its own git worktree per `CLAUDE.md`.

**Tech Stack:** React 19, TypeScript, Tailwind v4, Radix UI (dialog already in use), Lucide React, TanStack Query v5, Playwright (E2E).

**Out of scope for this plan (deferred to Phase 3, separate plan):** optimistic mutations, client-side form validation, research-tree keyboard navigation, chat search/mentions, pagination, admin polish.

**Source of problems:** `docs/UI-UX-DESIGN.md` §11 ("Known Rough Edges") and the journey analysis that followed it.

---

## Scope Overview

### Phase 1 — Safe polish (this PR: `ui-ux-phase-1`)

Low-design-risk changes that reduce daily friction without touching layout.

- **Task 1:** Generic `<ConfirmDialog>` primitive + wire up destructive actions (cancel trade, remove from build queue, revoke API key, cancel market order, cancel proposal).
- **Task 2:** `<Skeleton>` primitive + replace "Loading…" text on Base, Units, Market, Games, PlayerRanking.
- **Task 3:** Standardize on Lucide icons. Sweep emoji-as-icon usages and replace with Lucide equivalents. Leave emoji-as-flavor (e.g. chat messages) alone.
- **Task 4:** Inline accept/decline on diplomacy notification toasts.

### Phase 2a — Mobile table card fallback (PR: `ui-ux-mobile-tables`)

- **Task 5:** Add a `<ResponsiveTable>` wrapper that renders as a `<table>` at `md+` and as a stacked card list on mobile. Migrate Units, Market, PlayerRanking.

### Phase 2b — Attack flow as modal (PR: `ui-ux-attack-modal`)

- **Task 6:** Replace the `SelectEnemy` route navigation with a modal opened from the Units page. Keep forces visible behind the modal.

### Phase 2c — Base page restructure (PR: `ui-ux-base-tabs`)

- **Task 7:** Reorganize the Base page into three tabs — Economy / Build / Activity — with a pinned VictoryProgressBar + BuildQueue rail.

### Phase 3 — deferred

Dashboard page, optimistic updates, form validation, pagination, chat search, keyboard a11y, accessibility audit. Revisit after Phase 1+2 land.

---

## File Structure

### New files (Phase 1)

- `src/ReactClient/src/components/ui/ConfirmDialog.tsx` — generic confirmation dialog built on Radix `@radix-ui/react-dialog` (already a dependency).
- `src/ReactClient/src/components/ui/Skeleton.tsx` — single element + a `<SkeletonList>` helper for repeated rows.
- `src/ReactClient/src/hooks/useConfirm.ts` — imperative `const confirm = useConfirm(); if (await confirm({...})) {...}` hook so call sites don't each need local state for the dialog.

### New files (Phase 2)

- `src/ReactClient/src/components/ui/ResponsiveTable.tsx` — mobile card fallback for tables.
- `src/ReactClient/src/components/AttackDialog.tsx` — attack-target picker dialog.

### Modified files

Covered per-task.

---

## Phase 1 — Tasks

### Task 0: Worktree and plan commit

**Files:**
- Create: `docs/superpowers/plans/2026-04-12-ui-ux-improvements.md` (this file)
- Create: `docs/UI-UX-DESIGN.md` (already written in the previous turn, port from master working tree)

- [ ] **Step 1:** Create worktree `ui-ux-phase-1` off master.
- [ ] **Step 2:** Commit the design reference + this plan.

```bash
git add docs/UI-UX-DESIGN.md docs/superpowers/plans/2026-04-12-ui-ux-improvements.md
git commit -m "docs: add UI/UX design reference and improvement plan"
```

---

### Task 1: `<ConfirmDialog>` primitive + `useConfirm` hook

**Why:** Destructive actions currently fire immediately. Cancelling a market order or revoking an API key deserves a confirm step.

**Files:**
- Create: `src/ReactClient/src/components/ui/ConfirmDialog.tsx`
- Create: `src/ReactClient/src/hooks/useConfirm.ts`
- Create: `src/ReactClient/src/hooks/useConfirm.test.tsx` (Vitest + React Testing Library if present; if not, skip tests and rely on Playwright smoke)

- [ ] **Step 1:** Verify the test setup.

Run: `cd src/ReactClient && cat package.json | grep -E '"(test|vitest|jest)"'`
Expected: determine if a unit test runner is configured. If not, skip unit tests and do a manual smoke test in the dev server.

- [ ] **Step 2:** Implement `ConfirmDialog.tsx` using Radix Dialog.

```tsx
import * as Dialog from '@radix-ui/react-dialog';
import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface ConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
}

export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  destructive = false,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-[90] bg-black/60 animate-in fade-in" />
        <Dialog.Content
          className={cn(
            'fixed left-1/2 top-1/2 z-[91] w-[calc(100vw-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2',
            'rounded-lg border bg-card p-5 shadow-lg',
            'animate-in fade-in zoom-in-95 duration-150',
          )}
        >
          <Dialog.Title className="text-lg font-semibold">{title}</Dialog.Title>
          {description && (
            <Dialog.Description className="mt-2 text-sm text-muted-foreground">
              {description}
            </Dialog.Description>
          )}
          <div className="mt-5 flex justify-end gap-2">
            <Dialog.Close asChild>
              <button className="rounded border px-3 py-1.5 text-sm hover:bg-muted">
                {cancelLabel}
              </button>
            </Dialog.Close>
            <button
              className={cn(
                'rounded px-3 py-1.5 text-sm',
                destructive
                  ? 'bg-destructive text-destructive-foreground hover:opacity-90'
                  : 'bg-primary text-primary-foreground hover:opacity-90',
              )}
              onClick={() => {
                onConfirm();
                onOpenChange(false);
              }}
            >
              {confirmLabel}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
```

- [ ] **Step 3:** Implement `useConfirm` hook (imperative API via context provider).

```tsx
// src/ReactClient/src/hooks/useConfirm.tsx
import { createContext, ReactNode, useCallback, useContext, useRef, useState } from 'react';
import { ConfirmDialog, ConfirmDialogProps } from '@/components/ui/ConfirmDialog';

type ConfirmOptions = Omit<ConfirmDialogProps, 'open' | 'onOpenChange' | 'onConfirm'>;

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn | null>(null);

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null);
  const resolverRef = useRef<((v: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>((opts) => {
    return new Promise((resolve) => {
      resolverRef.current = resolve;
      setOptions(opts);
    });
  }, []);

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      resolverRef.current?.(false);
      resolverRef.current = null;
      setOptions(null);
    }
  };

  const handleConfirm = () => {
    resolverRef.current?.(true);
    resolverRef.current = null;
    setOptions(null);
  };

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {options && (
        <ConfirmDialog
          open
          onOpenChange={handleOpenChange}
          onConfirm={handleConfirm}
          {...options}
        />
      )}
    </ConfirmContext.Provider>
  );
}

export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error('useConfirm must be used within ConfirmProvider');
  return ctx;
}
```

- [ ] **Step 4:** Mount `ConfirmProvider` inside `RootLayout.tsx`, inside `CurrentUserProvider`.

- [ ] **Step 5:** Wire confirmation into destructive call sites. For each, replace the direct mutation call with:

```tsx
const confirm = useConfirm();
const onCancel = async () => {
  if (!(await confirm({
    title: 'Cancel this market order?',
    description: 'Your resources will be returned to your base.',
    destructive: true,
    confirmLabel: 'Cancel order',
    cancelLabel: 'Keep order',
  }))) return;
  cancelOrder.mutate({...});
};
```

Call sites to wire up (search with Grep first to confirm exact paths):
- Market page — cancel own order
- Trade page — cancel outgoing offer
- BuildQueue — remove entry
- PlayerProfile — revoke API key
- Diplomacy — cancel outgoing proposal

- [ ] **Step 6:** Run `npm run build` in `src/ReactClient` to verify TypeScript compiles.

- [ ] **Step 7:** Manually smoke in dev server: click a destructive button, verify dialog appears, both paths work.

- [ ] **Step 8:** Commit.

```bash
git add src/ReactClient/src/components/ui/ConfirmDialog.tsx \
        src/ReactClient/src/hooks/useConfirm.tsx \
        src/ReactClient/src/layouts/RootLayout.tsx \
        src/ReactClient/src/pages/<affected-pages>
git commit -m "feat(ui): add ConfirmDialog primitive and wire destructive actions"
```

---

### Task 2: `<Skeleton>` primitive + replace "Loading…" text

**Why:** "Loading…" flashes feel slow and generic. Skeletons communicate structure.

**Files:**
- Create: `src/ReactClient/src/components/ui/Skeleton.tsx`
- Modify: Base, Units, Market, Games, PlayerRanking pages (exact paths via Grep before edit).

- [ ] **Step 1:** Implement Skeleton.

```tsx
import { cn } from '@/lib/utils';
import { HTMLAttributes } from 'react';

export function Skeleton({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('animate-pulse rounded bg-muted', className)}
      aria-busy="true"
      aria-live="polite"
      {...props}
    />
  );
}

export function SkeletonRows({ count = 4, className }: { count?: number; className?: string }) {
  return (
    <div className={cn('space-y-2', className)}>
      {Array.from({ length: count }).map((_, i) => (
        <Skeleton key={i} className="h-10 w-full" />
      ))}
    </div>
  );
}
```

- [ ] **Step 2:** Grep for `Loading\.\.\.` inside `src/ReactClient/src/pages/` and list files.

Run: Grep pattern `Loading\.\.\.` in `src/ReactClient/src/pages` → files_with_matches

- [ ] **Step 3:** For each page, replace the loading text with a page-appropriate skeleton layout. For tables, use `<SkeletonRows count={5} />` inside the table container. For cards, a single `<Skeleton className="h-32 w-full" />` per card slot.

- [ ] **Step 4:** Build + smoke test in dev server.

- [ ] **Step 5:** Commit.

```bash
git commit -m "feat(ui): add Skeleton primitive and replace Loading text on core pages"
```

---

### Task 3: Icon standardization

**Why:** Mixed Lucide + emoji-as-icon is visually jarring and rendering-inconsistent.

**Files:**
- Modify: any file with emoji used as an icon inside buttons, nav items, or headings (not inside user-authored chat content).

- [ ] **Step 1:** Grep for common emoji-icon patterns.

Run: Grep for `⚔️|🛡️|🏭|💰|📊|🏆|📜|🚀|⚡|🔬|🏛️|⛏️|📡|🎯|🔔` in `src/ReactClient/src/` → files_with_matches.

- [ ] **Step 2:** For each match, decide: is this UI chrome or content?
- UI chrome (button icon, nav icon, panel header): replace with Lucide equivalent.
- Content (chat, user-facing flavor text, notification body): leave alone.

Suggested mappings:
- ⚔️ → `Swords`
- 🛡️ → `Shield`
- 🏭 → `Factory`
- 💰 → `Coins`
- 📊 → `BarChart3`
- 🏆 → `Trophy`
- 📜 → `ScrollText`
- 🚀 → `Rocket`
- ⚡ → `Zap`
- 🔬 → `FlaskConical`
- 🏛️ → `Landmark`
- ⛏️ → `Pickaxe`
- 📡 → `Radio`
- 🎯 → `Target`
- 🔔 → `Bell`

- [ ] **Step 3:** Replace one file at a time. Keep the icon size consistent (`h-4 w-4` inline, `h-5 w-5` for nav).

- [ ] **Step 4:** Build. Smoke test.

- [ ] **Step 5:** Commit.

```bash
git commit -m "refactor(ui): standardize on Lucide icons for UI chrome"
```

---

### Task 4: Inline accept/decline on diplomacy notification toasts

**Why:** Today, a diplomacy proposal notification routes you to the Diplomacy page to act. Most users just want to click accept.

**Files:**
- Modify: `src/ReactClient/src/components/NotificationToasts.tsx` (path via Grep)
- Modify: notification type definitions if they exist

- [ ] **Step 1:** Find the notification toast component and its data model.

Run: Grep `NotificationToasts` in `src/ReactClient/src/` → file path.

- [ ] **Step 2:** Determine how notification payloads distinguish types. If there's a `type` or `category` field with values like `diplomacy-proposal`, branch on it to render extra action buttons.

- [ ] **Step 3:** Add `Accept` / `Decline` buttons on the toast when `notification.type === 'diplomacy-proposal'`. On click, call the existing respond-to-proposal mutation, then dismiss the toast. If the API requires the proposal ID, pull it from the notification payload.

```tsx
{notification.type === 'diplomacy-proposal' && notification.proposalId && (
  <div className="mt-2 flex gap-2">
    <button
      onClick={(e) => { e.stopPropagation(); respond.mutate({ id: notification.proposalId, accept: true }); dismiss(notification.id); }}
      className="rounded bg-primary px-2 py-1 text-xs text-primary-foreground"
    >
      Accept
    </button>
    <button
      onClick={(e) => { e.stopPropagation(); respond.mutate({ id: notification.proposalId, accept: false }); dismiss(notification.id); }}
      className="rounded border px-2 py-1 text-xs hover:bg-muted"
    >
      Decline
    </button>
  </div>
)}
```

- [ ] **Step 4:** If the notification payload does not include the proposal ID, document the limitation, skip this task, and open a follow-up ticket instead of changing the API in this PR.

- [ ] **Step 5:** Build + smoke (dev server or Playwright).

- [ ] **Step 6:** Commit.

```bash
git commit -m "feat(ui): inline accept/decline on diplomacy notification toasts"
```

---

### Task 5: Phase 1 wrap-up

- [ ] **Step 1:** Full `dotnet build --configuration Release` in `src/` to catch any type errors downstream.

Run: `cd src && dotnet build --configuration Release`

- [ ] **Step 2:** `npm run build` in `src/ReactClient` if it exists.

- [ ] **Step 3:** Push branch and open PR.

```bash
git push -u origin ui-ux-phase-1
gh pr create --title "UI/UX Phase 1: confirmation dialogs, skeletons, icon cleanup, inline diplomacy" --body "$(cat <<'EOF'
## Summary
- Add `ConfirmDialog` + `useConfirm` hook; wire destructive actions (market/trade cancel, queue remove, API-key revoke, proposal cancel).
- Add `Skeleton` + replace `"Loading..."` text on Base, Units, Market, Games, PlayerRanking.
- Replace emoji-as-icon usages in UI chrome with Lucide equivalents.
- Diplomacy proposal notifications now accept/decline inline from the toast.
- Add `docs/UI-UX-DESIGN.md` (reverse-engineered UI reference) and `docs/superpowers/plans/2026-04-12-ui-ux-improvements.md`.

## Test plan
- [ ] Dev server boots.
- [ ] Cancel market order → confirmation appears; both paths work.
- [ ] Remove build-queue entry → confirmation appears.
- [ ] Revoke API key → confirmation appears.
- [ ] Navigate to Base / Units / Market → skeletons visible during load.
- [ ] No residual emoji-as-icon in nav, headers, or buttons.
- [ ] Receive diplomacy proposal → accept/decline inline from toast.
EOF
)"
```

---

## Phase 2a — Mobile table card fallback

### Task 6: `<ResponsiveTable>` component

**Why:** Tables currently hide columns below `md` and there is no alternative view. Data is literally invisible on phones.

**Worktree:** `ui-ux-mobile-tables` off current `master` at the time Phase 1 merges.

**Files:**
- Create: `src/ReactClient/src/components/ui/ResponsiveTable.tsx`
- Modify: Units, Market, PlayerRanking pages

- [ ] **Step 1:** Design the API. Target usage:

```tsx
<ResponsiveTable
  data={units}
  columns={[
    { key: 'name', header: 'Unit', cell: (u) => u.name },
    { key: 'count', header: 'Count', cell: (u) => u.count, className: 'text-right font-mono' },
    { key: 'attack', header: 'Atk', cell: (u) => u.attack, mobileHidden: false },
    { key: 'defense', header: 'Def', cell: (u) => u.defense },
    { key: 'actions', header: '', cell: (u) => <UnitActions unit={u} /> },
  ]}
  rowKey={(u) => u.id}
  mobileTitle={(u) => u.name}
  mobileSubtitle={(u) => `${u.count} units`}
/>
```

- [ ] **Step 2:** Implement the component. At `md+`, render a standard `<table>`. Below `md`, render a stack of cards: mobile title in bold, subtitle in muted, then a two-column key/value grid for the remaining columns (respecting `mobileHidden`), and a full-width row for the `actions` column at the bottom.

- [ ] **Step 3:** Migrate Units page first. Verify both desktop and mobile layouts in dev tools.

- [ ] **Step 4:** Migrate Market and PlayerRanking.

- [ ] **Step 5:** Commit, push, PR.

```bash
git commit -m "feat(ui): add ResponsiveTable with mobile card fallback"
git push -u origin ui-ux-mobile-tables
gh pr create --title "UI/UX: responsive tables (mobile card fallback)" ...
```

---

## Phase 2b — Attack flow as modal

### Task 7: `<AttackDialog>`

**Why:** The attack flow navigates you away from the Units page to `SelectEnemy`, breaking context and forcing back-navigation.

**Worktree:** `ui-ux-attack-modal` off master.

**Files:**
- Create: `src/ReactClient/src/components/AttackDialog.tsx`
- Modify: Units page (attack button opens modal instead of navigating)
- Possibly delete/deprecate: `SelectEnemy` page if it's only reachable from Units

- [ ] **Step 1:** Read the current `SelectEnemy` page to understand its data dependencies and mutations.

- [ ] **Step 2:** Build `AttackDialog` as a Radix Dialog with:
  - Header: "Attack with {stack summary}"
  - Body: target player search/list (reuse whatever query `SelectEnemy` uses)
  - Footer: Cancel + Attack (destructive variant)

- [ ] **Step 3:** In the Units page, replace the Attack `<Link>` with a button that opens the dialog. Pass the selected stack.

- [ ] **Step 4:** On confirm, call the existing attack mutation and close the dialog. Show inline error if it fails.

- [ ] **Step 5:** If `SelectEnemy` has no other entry points, delete it and its route. Otherwise leave it.

- [ ] **Step 6:** Build, smoke, commit, push, PR.

---

## Phase 2c — Base page restructure

### Task 8: Tabbed Base page

**Why:** Base is a wall of stacked sections with no hierarchy. Tabs give a clear mental model: "where am I looking right now?"

**Worktree:** `ui-ux-base-tabs` off master (after 2a/2b land — this is the most visible change).

**Files:**
- Modify: Base page and its subcomponents

- [ ] **Step 1:** Read the current Base page to enumerate every child section.

- [ ] **Step 2:** Plan the three tabs:
  - **Economy:** WorkerAssignment, resource history chart, trade/colonize panels.
  - **Build:** Asset grid, prerequisite legend.
  - **Activity:** Recent activity feed, notification log.

  Pinned rail (always visible above tabs): VictoryProgressBar, BuildQueue. These do not scroll with the tab content.

- [ ] **Step 3:** Use Radix `@radix-ui/react-tabs` (add to deps if not present — check first).

- [ ] **Step 4:** Preserve URL state by syncing the active tab to a `?tab=` search param so deep links work.

- [ ] **Step 5:** Verify existing queries still fire — tab-switching must not force a full refetch unless needed. TanStack Query caches across unmounts by default, so this should be free.

- [ ] **Step 6:** Build, smoke desktop + mobile, commit, push, PR.

---

## Self-Review

**Spec coverage against the prior exploratory response:**
- Confirmation dialogs ✓ Task 1
- Skeleton loaders ✓ Task 2
- Inline diplomacy actions ✓ Task 4
- Kill emoji-as-icon ✓ Task 3
- Attack modal ✓ Task 7
- Base restructure ✓ Task 8
- Mobile table card fallback ✓ Task 6
- Situation dashboard — **deferred, documented in Phase 3 section**
- Optimistic updates, form validation, research tree a11y, chat search, pagination — **deferred, documented**

**Placeholder scan:** No TBDs in implementation steps. Code blocks are concrete. Component migrations use Grep-first because exact paths aren't known yet — that's a deliberate discovery step, not a placeholder.

**Type consistency:** `ConfirmDialogProps` is defined once in Task 1 and reused by the hook. `useConfirm` returns a `Promise<boolean>` consistently across tasks.

**Scope risk:** Phase 1 is four sub-features in one PR. Reviewers may prefer to split, but each sub-feature is small (~50–150 LOC) and they share no code except the primitives, so a single PR is reasonable. If reviewers push back, splitting is trivial because every sub-feature has isolated file modifications.
