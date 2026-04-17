# UI/UX Improvements — Design

**Date:** 2026-04-17
**Project:** Age of Agents (BGE React client)
**Scope:** Foundation design-system pass (shadcn/ui) plus a visual refresh, applied to every player-facing page (~37, counted below). Admin pages out of scope.

## Context

The React client at `src/ReactClient/` is a dark-first SPA built on Tailwind 4, Radix UI primitives, React Router 7, TanStack Query, and lucide-react. `components.json` is already configured for shadcn/ui ("new-york" style, `zinc` base, `components/ui` alias), `cn()` is in `src/lib/utils.ts`, and `class-variance-authority` / `clsx` / `tailwind-merge` are installed — but **no shadcn components have been installed**, and `src/components/ui/` does not exist. The project is fully scaffolded for shadcn, just not using it.

As a result, ~45 pages reinvent the same primitives ad hoc:

- `rounded bg-primary px-… hover:opacity-90` buttons with drifting sizes.
- Hand-rolled tables with duplicated `<thead>` blocks (Market, Trade, Diplomacy, Alliances, GameLobby, Units — same shape, six implementations).
- Inline form blocks with local error strings and never-clearing success banners.
- A hand-rolled modal in `Alliances.tsx` with no focus trap.
- Six color palettes for status pills.
- Mixed lucide icons and emoji.
- Terminology drift ("rounds" vs "ticks").

Full audit findings are in the conversation transcript; high-severity issues informed this spec.

## Goals

1. Install and actually use shadcn/ui. Build a small set of custom primitives (`PageHeader`, `Section`, `DataTable`, `Field`, `Stat`, `Ticks`) on top.
2. Visual refresh: "polished shadcn" base layered with tactical/strategic flavor — amber as the primary action color, blue demoted to informational accent, sharper corners on commit actions, monospace tabular numerals, subtle grid texture on `<main>`.
3. Redesign the persistent frame (`GameLayout`, `PlayerResources`, top bar, `NavMenu`).
4. Apply the primitives + conventions across every player-facing page in four batched PRs.

## Non-goals

- Backend API changes. New data needs (per-tick deltas, nav badges) are computed client-side from existing responses.
- Admin pages (`src/pages/admin/*`) — a later pass.
- Charting internals (`recharts` stays; `ResourceHistoryChart` + `PlayerStatsCharts` only get wrapped in `Card`).
- Bot client, mobile-native apps, PWA offline, i18n.

## Visual direction

**"Polished shadcn + tactical"**: keep the dark shadcn foundation and tightened spacing/typography; layer in strategic flavor (amber accent, sharper corners on commit actions, monospace numerals, subtle grid texture, mono/uppercase labels). No glow effects, no backdrop-blur on primary UI, no gradients — those are reserved for dialogs and overlays.

The visual-direction mockups the user reviewed are preserved at `.superpowers/brainstorm/20431-1776456485/content/visual-direction.html`.

## Section 1 — Foundation primitives

**Installed from shadcn** (into `src/components/ui/`, via `npx shadcn@latest add …`):

`button`, `card`, `badge`, `input`, `label`, `select`, `dialog`, `dropdown-menu`, `tooltip`, `progress`, `separator`, `skeleton`, `tabs`, `scroll-area`, `sonner`, `toggle`, `form` (integrates with `react-hook-form`).

**Custom primitives** (also in `src/components/ui/`):

- `PageHeader` — `{ title, description?, actions? }`. Canonical top-of-page block. Replaces `<h1 className="text-2xl font-bold mb-4">` everywhere.
- `Section` + `SectionHeader` — padded block with heading. Replaces the ad-hoc `<div className="rounded-lg border bg-card p-4">` pattern.
- `DataTable<TRow>` — wraps `@tanstack/react-table`. Props: `columns`, `rows`, `empty`, `loading`, `error`, `sortable`, `stickyHeader`, `density: 'compact' | 'comfortable'`, `onRowClick?`. Absorbs every hand-rolled table.
- `Field` — `{ label, error?, hint?, children }`; wires into `react-hook-form` + `zod` via the shadcn `form` component.
- `Stat` — `{ label, value, delta?, trend? }`. Big-number + label + optional delta + optional sparkline.
- `Ticks` — renders `"120 ticks (~4m)"` consistently, driven by the game's tick duration from config.
- `Countdown` — `{ to: ISODate }`. Pure component handling tick updates, low-time `danger` color, pause-when-tab-hidden.
- `ResourceIcon` — single source of truth for resource iconography.
- `CostBadge` (refactor) — icon + amount; text label opt-in; icon map via `ResourceIcon`.

**Existing, kept & refactored:** `EmptyState`, `ApiError`, `PageLoader`, `NotificationBell`, `NotificationToast`, `TutorialOverlay`, `Skeleton`, `VictoryProgressBar`, `BuildQueue`, `BuildUnitsForm`, `WorkerAssignment`, `ResourceHistoryChart`, `CurrencyBadge`.

**Deleted:** hand-rolled modal scaffolding in `Alliances.tsx`; duplicated `<thead>` blocks; inline button class strings across pages; duplicated Research Attack/Defense blocks (extracted to `<UpgradeTrack>`).

**New dependencies:** `@tanstack/react-table`, `sonner`, `react-hook-form`, `zod`, `@radix-ui/react-tooltip`, `@radix-ui/react-tabs`, `@radix-ui/react-progress`, `@radix-ui/react-scroll-area`. All already transitively compatible with the installed Radix + Tailwind 4 stack.

## Section 2 — Design tokens

All changes localized to `src/ReactClient/src/index.css`. No per-component style overrides.

**Dark theme tokens:**

```
--color-background:        hsl(222 47% 6%)
--color-card:              hsl(222 47% 8%)       # distinct from bg (today they're identical)
--color-border:            hsl(217 32% 16%)
--color-primary:           hsl(38 92% 55%)       # amber — commit action
--color-primary-foreground:hsl(38 30% 10%)
--color-accent:            hsl(217 91% 60%)      # blue — informational
--color-ring:              hsl(38 92% 55%)       # matches primary
--color-warning:           hsl(48 95% 55%)       # shifted yellow so it's distinct from primary amber
# success / danger / info keep their roles, minor contrast tuning
```

Light theme: same role mapping (amber primary, blue accent), tokens rebalanced for WCAG AA.

**Typography:**

- `--font-sans: system-ui, -apple-system, sans-serif` (unchanged).
- `--font-mono: 'JetBrains Mono', ui-monospace, Menlo, monospace` (new; self-hosted for prod).
- Fixed scale: `text-3xl` page titles, `text-lg` section headings, `text-sm` body, `text-xs` labels, `text-[11px]` uppercase meta. Enforced via `PageHeader` / `SectionHeader`.
- `.label` utility: `text-[11px] uppercase tracking-[0.12em] text-muted-foreground font-semibold`.

**Shape, spacing, motion, numerals:**

- `--radius` stays `0.5rem` for cards. `Button` primary variant and `Badge` use `rounded-sm` (2px) — where the tactical flavor lives.
- Spacing scale: 4 / 8 / 12 / 16 / 24. No stray `p-3`, `p-5`, `gap-2.5`.
- `.bg-grid` utility on `<main>`: 1px grid lines at `hsl(217 32% 12%)`, 16px × 16px, opacity 40%. Not on cards.
- `transition-[background,border-color,transform] duration-150` baseline.
- `@media (prefers-reduced-motion: reduce)` disables all `transition` except focus rings.
- All numerics get `font-variant-numeric: tabular-nums` (via `.mono` class + a default on `DataTable` cells).

## Section 3 — Global chrome

**Top bar (`GameLayout.tsx`):**

- Two rows below `md` breakpoint, one row on `md+`.
- Row 1 (always): hamburger (mobile) · game name + tick counter (`Ticks`, mono) · right-aligned: signal pill (`Connected` / `Reconnecting…` / `Offline`, replaces the bare wifi icon) · `NotificationBell` · theme toggle · user menu.
- Row 2 (mobile, sticky): `PlayerResources`.
- User menu (shadcn `DropdownMenu`): avatar + display name → Profile, Shop, History, Stats, Sign out. Replaces the sidebar-footer sign-out link.

**`PlayerResources` redesign:**

- Horizontal list of `<Stat>` cells: `ResourceIcon` + value (mono, tabular) + `+Δ/t` delta.
- Low-resource state (resource will reach zero within 3 ticks at current consumption rate, computed client-side from the existing resource + delta values): value `danger`-toned, delta in a warning `Badge`.
- Hovering a value: `Tooltip` with raw number, income breakdown (workers × rate), sparkline (reuses `ResourceHistoryChart` data).
- `md+`: inline. Below `md`: horizontal scroll with snap points and fade mask. No wrapping.

**`NavMenu.tsx` redesign:**

- One semantic `<ul>` with `SectionLabel` list-items (coherent screen-reader traversal — current layout uses 4 separate `<ul>`s).
- `badge` slot per `NavItem` for counts (unread messages, pending alliance invites, open diplomacy proposals). Data from a new `useNavBadges(gameId)` hook driven by existing SignalR notifications — no new API endpoints.
- Active item: 2px left amber accent bar (not a full filled background).
- `CurrencyBadge` moves to a dedicated sidebar footer row above "Sign out".
- Sidebar width: 208 → 224 (fixes "Alliance Ranking" truncation).

**Banners:**

- `GameEndedBanner` + `TimeRemainingBanner` collapse into one `<GameStatusBanner>` that picks the correct variant and hides when neither applies.
- Uses `Badge` + `Progress` (time remaining) instead of emoji + text.

**`GameOverOverlay`:** uses Radix `Dialog` for focus trap + escape + aria (currently hand-rolled, no focus trap).

## Section 4 — Cross-cutting conventions

**Page template:**

```tsx
<PageHeader title="…" description="…" actions={…} />
<Section>
  <DataTable … />  {/* or <Form>, <Card>, <Stat> grid */}
</Section>
```

**Forms:**

- `react-hook-form` + `zod`, colocated schema.
- `<Form>` + `<FormField>` + `<FormMessage>` (shadcn `form`).
- Submit button state driven by `form.formState`; disabled with `disabledReason` surfaced via `Tooltip`.
- Server errors → `form.setError('root', …)`; displayed next to the submit button.
- Success → `toast.success(…)` via Sonner. Never an inline stale success banner.

**Loading / empty / error:**

- Loading: `Skeleton` rows matching the real layout. `PageLoader` only for initial page load.
- Empty: `<EmptyState>` with `icon`, `title`, `description`, optional `action`.
- Error: `<ApiError>` with standard retry button.
- `DataTable` accepts `emptyState` / `loadingState` / `errorState` slots.

**Optimistic updates:** applied to every commit-action mutation (build, research, assign workers, accept trade, propose diplomacy, etc.) via `useMutation({ onMutate, onError: rollback })`. Primary action button never spins for >100ms.

**Numbers & time:**

- All numerics: `formatNumber()` + tabular nums.
- All tick counts: `<Ticks n />`.
- Timers: `<Countdown to={iso} />`.

**Terminology:**

- "tick" (never "round"). Pre-commit soft-warn grep: `rg -n '\brounds?\b' src/ReactClient/src`, flagged opt-outs via `// round: keep-literal`.
- "Build" not "Construct"/"Create"/"Produce".
- "Opponent" in neutral UI; "Enemy" reserved for battle/attack surfaces.

**Icons:** `lucide-react` only in UI chrome. Emoji in chat messages, notification bodies, user-entered content only. `🪖⚔️🛡️❤️💎⚗️🏗️` in Base/Help/Games → `Sword`, `Shield`, `Heart`, `Gem`, `FlaskConical`, `Hammer`.

**Accessibility floor (every PR):**

- Interactive ≥ 44×44 px touch target.
- Focus visible: amber `ring-2 ring-primary ring-offset-2 ring-offset-background`.
- Status communicated by icon + text, not color alone.
- Decorative emoji get `aria-hidden="true"`.
- All modals via Radix `Dialog`.
- `aria-label` on all icon-only buttons.

**Testing:** existing Playwright suite (`src/ReactClient/e2e/`, `npm run test:e2e`) must stay green. Pages keep their current `data-testid` attributes; test behavior assertions stay; class-name-dependent tests updated in the PR that triggers the change.

## Section 5 — Page refactor batches

Foundation PR lands first (Sections 1–4). Then four page batches.

**Batch 1 — Core gameplay (5 pages)**: `Base`, `Units`, `Research`, `Market`, `Trade`.

- `Base`: reorder vertically — Assets + BuildQueue first, WorkerAssignment, Colonize, Charts, Activity. The inline 2-way resource swap is removed from Base and folded into Market as a "Convert" tab.
- `Units`: `DataTable` rows, tabular nums, `Stat` cells for squad totals.
- `Research`: extract duplicated Attack/Defense blocks into `<UpgradeTrack>`; add `Progress` for in-progress research.
- `Market`: `Tabs` for Buy / Sell / Convert; `DataTable` listings; per-row `disabledReason` tooltips.
- `Trade`: `DataTable` of incoming + outgoing offers; Radix `Dialog` for propose-trade.

**Batch 2 — Social & game lists (7 pages)**: `Diplomacy`, `Alliances`, `AllianceDetail`, `GameLobby`, `GameLobbyDetail`, `Games`, `PlayerRanking`.

- `Diplomacy`: `Tabs` for Incoming / Outgoing / Relationships.
- `Alliances`: Radix `Dialog` replaces hand-rolled modal. `Tabs` for Members / Invites / Applications.
- `Games` becomes the canonical list surface; `GameLobby` reduces to a create-game form + list filtered to `Upcoming`. Same `DataTable`, same `Badge`.
- `GameLobbyDetail`: `PageHeader` + `Section` grid for game settings + participants `DataTable`.
- `PlayerRanking`: sortable `DataTable` with sticky header.

**Batch 3 — Meta-game & spectator (9 pages)**: `GameSummary`, `GameResults`, `ReplayViewer`, `BattleReplay`, `Spectator`, `GameLiveView`, `TournamentList`, `TournamentDetail`, `TournamentResults`.

- Tournament pages: `Tabs` for Rounds / Standings / Rules; `DataTable` participants.
- `GameResults` / `GameSummary`: `Stat` grid + shared `<VictoryBadge>` using existing `vcText` / `vcBadgeCss`.
- Replay pages: consistent playback controls; `PageHeader`.

**Batch 4 — Account, shop, help, misc (16 pages)**: `PlayerProfile`, `PublicProfile`, `PlayerHistory`, `PlayerStats`, `PlayerStatsCharts`, `PlayersList`, `Shop`, `Economy`, `Help`, `Messages`, `Chat`, `CreatePlayer`, `SignIn`, `JoinGame`, `SelectEnemy`, `InGamePlayerProfile`, `EnemyBase`, `UnitDefinitions`.

- `Help`: sidebar TOC + anchored `Section`s + search-in-page.
- `Chat` / `Messages`: shared message-list + composer.
- `Shop` / `Economy`: `Stat` grid + `DataTable`.
- `SignIn`: structure unchanged; GitHub button becomes a styled `Button`.

**Admin pages** (`src/pages/admin/*`): out of scope.

**Page count:** 5 + 7 + 9 + 16 = 37 player-facing pages total, plus the `Index.tsx` redirect stub (no refactor needed).

**Size estimate:**
- Foundation PR: ~1500 LOC added (primitives) + ~500 LOC touched (`GameLayout`, `NavMenu`, `PlayerResources`, `index.css`).
- Each page batch: ~800–1500 LOC net deletion. Batch 4 is the largest (16 pages) but most are mechanical page-header/table/form swaps.

## Section 6 — Rollout & verification

**Foundation PR gates:**

- `dotnet build --configuration Release` + `dotnet test` green.
- `npm run build` green (tsc strict + vite build).
- `npm run lint` clean.
- `npm run test:e2e` green. Class-name-dependent test assertions updated in this PR.
- Manual QA on `GameLayout`, `NavMenu`, `PlayerResources`, plus one representative page.
- PR review checklist: tokens only in `index.css`; no inline hex colors in component code; all new primitives in `src/components/ui/`.

**Per-batch PR gates:**

- Same build / test / lint / e2e baseline.
- Screenshot diff grid in the PR body (Playwright script logs in via DevAuth, navigates each page, captures full-page screenshots). Review aid, not a blocker.
- CI grep checks (non-blocking warnings; fail only on new violations):
  - `rg 'rounded.*bg-primary.*px-' src/ReactClient/src/pages` → 0.
  - `rg '<table' src/ReactClient/src/pages` → 0.
  - `rg '\brounds?\b' src/ReactClient/src` → 0 outside opt-outs.
  - `rg 'bg-card p-4.*rounded-lg' src/ReactClient/src/pages` → 0.
- Manual QA: each page in the batch loaded once in dev, one critical action tried per page.

**Rollback:** foundation PR bakes on `master` at least a few days before Batch 1 opens. Each batch is independently revertable via `git revert`.

**Success metric (qualitative):**

- `wc -l src/ReactClient/src/pages/*.tsx` drops measurably.
- Grep checks above stay at 0.
- No increase in Playwright test count.
- Sentry error rate not worse post-deploy (existing `@sentry/react` integration).

## Open questions

None after brainstorming — all scoping Qs resolved in the conversation. Any decisions the implementation plan needs to reopen should be lifted into a fresh brainstorm, not buried in implementation.
