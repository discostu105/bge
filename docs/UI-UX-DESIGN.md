# BGE UI/UX Design Reference

Reverse-engineered from the React client at `src/ReactClient`. Describes the current state of the UI: what it is, how it looks, how it behaves, and the conventions it has settled on. Intended as the shared mental model for anyone touching frontend code or designing new screens.

---

## 1. Product Shape

BGE is a **browser-based multiplayer strategy game** (StarCraft Online / SCO being the first concrete ruleset). The UI has three high-level modes:

1. **Meta-game**: sign in, browse seasons, join/create games, manage account.
2. **In-game**: per-game scoped pages for base management, units, research, diplomacy, trade, chat, intel.
3. **Spectator / post-game**: live view, results, replays, rankings.

The entire client is a single SPA; mode is expressed through the route, not through separate apps.

---

## 2. Tech Stack

| Concern | Choice |
|---|---|
| Framework | React 19 + TypeScript (strict) |
| Build | Vite 6 |
| Routing | React Router v7 (lazy routes + Suspense) |
| Server state | TanStack Query v5 (5s staleTime, retry=1, per-query `refetchInterval`) |
| HTTP | Axios instance with 401 → `/signin` interceptor |
| Real-time | SignalR (`/gamehub`) with WS → long-poll fallback, auto-reconnect |
| Styling | Tailwind CSS v4 (via `@tailwindcss/vite`) + CSS custom properties for tokens |
| Primitives | Radix UI (dialog, dropdown, select, toast, label, separator, slot) |
| Icons | Lucide React (+ sparing emoji) |
| Charts | Recharts v3 (resource history) |
| Utility | `clsx` + `tailwind-merge` via a `cn()` helper, `class-variance-authority` for variants |
| Monitoring | Sentry (opt-in, 10% trace sample) |
| E2E | Playwright |

Routes are split aggressively: `/signin` and `/` are eager, **every game page is `React.lazy()`** wrapped in a `RouteWithBoundary`. Suspense fallback is a shared `PageLoader`.

---

## 3. Design Tokens

Tokens live in `index.css` as CSS custom properties, consumed by Tailwind's theme. Dark is the default; a `.light` class on `<html>` switches palette.

### Color (HSL)

| Token | Dark | Light | Usage |
|---|---|---|---|
| `--background` | `222 84% 5%` | white | page background |
| `--foreground` | `210 40% 98%` | dark gray | body text |
| `--card` | = background | white | card surfaces |
| `--primary` | `217 91% 60%` | desaturated blue | CTAs, active nav, links |
| `--primary-foreground` | `222 47% 11%` | — | text on primary |
| `--secondary` / `--muted` / `--border` / `--input` | `217 33% 18%` | light gray | chrome |
| `--muted-foreground` | `215 20% 65%` | — | helper/label text |
| `--ring` | `224 76% 48%` | — | focus outline |
| `--destructive` | `0 63% 31%` | — | danger buttons/messages |

**Semantic status colors** (custom, outside the shadcn-style token set):

- `success` `142 71% 35%` — green
- `warning` `38 92% 45%` — amber
- `danger` `0 72% 45%` — red
- `info` `217 91% 55%` — blue

Theme preference persists in `localStorage` under `bge-theme`; initial value reads `prefers-color-scheme`.

### Typography

- Font: OS stack (`system-ui, -apple-system, sans-serif`). No custom webfont.
- Scale: `text-4xl` hero → `text-2xl` page title → `text-lg` section → `text-sm` body → `text-xs` labels.
- Weights: 400 / 500 / 600 / 700.
- **`font-mono` for all numeric game data** (resources, costs, timers, counts). This is a deliberate and consistent choice.

### Spacing & Radii

- Tailwind 4px grid. Common rhythms: `gap-2`, `gap-3`, `p-3`/`p-4`, `py-2 px-3`.
- Responsive padding pattern: `p-3 sm:p-4` (tighter on mobile).
- Radius token: `--radius: 0.5rem` (8px) applied uniformly.
- Fixed sidebar width `w-52` (208px), header `h-12` (48px).
- Touch targets: interactive controls target ≥36px, primary/nav ≥44px.

---

## 4. App Shell & Layout

### Provider stack

```
ErrorBoundary
└── ThemeProvider
    └── CurrentUserProvider        (loads /me on mount)
        └── <Outlet />              (route tree)
```

### `GameLayout` (the in-game shell)

A persistent three-region layout once you're inside a game:

```
┌────────────┬──────────────────────────────────────────┐
│            │  Header (h-12, sticky)                   │
│  Sidebar   │  [☰] ……………… PlayerResources · 📶 · 🔔 · ☀ │
│  (w-52)    ├──────────────────────────────────────────┤
│  • game    │  [GameEndedBanner] · [TimeRemainingBanner]│
│    title   ├──────────────────────────────────────────┤
│  • NavMenu │                                          │
│  • sign    │          <Outlet />  (main, scroll-y)    │
│    out     │                                          │
└────────────┴──────────────────────────────────────────┘
```

- **Sidebar**: `hidden md:flex`, scrollable. On mobile it becomes a fixed drawer (`-translate-x-full` ↔ `translate-x-0`) with a z-40 backdrop.
- **Header**: sticky, carries global controls. `PlayerResources` collapses gaps (`gap-2 sm:gap-4`) and uses `whitespace-nowrap` to survive narrow screens.
- **Banners**: `GameEndedBanner` (winner + victory condition + results link) and `TimeRemainingBanner` (countdown, turns red when <1h) appear above main content when relevant.
- **Main**: `flex-1 overflow-y-auto`, `p-3 sm:p-4`, page-fade-in animation on route change (respects `prefers-reduced-motion`).

### Overlay z-index ladder

| Layer | z-index | Example |
|---|---|---|
| Toasts | 50 | `NotificationToasts` top-right |
| Tutorial overlay | 60 | first-run tutorial dialog |
| Game-over modal | 100 | finalized game w/ 8s countdown to results |

### Navigation menu

Five labeled groups, uppercase section headers, active state is `bg-primary/20 text-primary font-medium`:

- **Play** — Base, Units, Research, Market, Trade
- **Info** — Live View, Unit Types, Ranking, Help
- **Social** — Diplomacy, Alliances, Alliance Ranking, Chat, Messages
- **Account** — History, Stats, Profile
- **Economy** — Shop, My Economy, Currency badge

Each entry is a `NavLink` with icon + label, min-height 44px. Bare routes like `/base` exist and redirect via `BareRouteRedirect` to the current game's scoped route.

---

## 5. Page Inventory

### Meta / onboarding
- **SignIn** *(eager)* — GitHub OAuth button, optional dev-login form gated on `VITE_DEV_AUTH`.
- **Index** *(eager)* — landing/redirect.
- **CreatePlayer** — first-time profile creation.

### Game selection
- **Games** — season schedule split into Active / Upcoming / Finished card grids with status badges.
- **GameLobby**, **GameLobbyDetail** — admin-facing game management and detail.
- **JoinGame** — registration for a specific game.

### Core gameplay (under `/games/:gameId/`)
- **Base** — the hub. Victory progress → worker assignment → trade/colonize → asset grid → build queue → resource history chart → recent activity.
- **Units** — home-base and deployed stacks, with merge / split / attack actions.
- **Research** — Attack & Defense upgrade tracks rendered as connected node rows.
- **Market** — post/accept/cancel trade orders, tabbed (mine vs open).
- **Trade** — direct player-to-player offers with incoming/sent/history tabs.

### Social & multiplayer
- **Diplomacy** — Non-Aggression Pacts and Resource Agreements: active, incoming, outgoing.
- **Alliances**, **AllianceDetail**, **AllianceRanking**.
- **Chat** — global game chat via SignalR with 5s polling fallback.
- **Messages** — DM inbox, threads, compose.
- **PlayerRanking**, **InGamePlayerProfile**, **EnemyBase** (intel view).

### Spectator & analysis
- **GameLiveView** — real-time scoreboard and notification feed, with pinch-zoom/pan on mobile.
- **GameSummary**, **GameResults**, **BattleReplay**, **ReplayViewer**, **Spectator**.

### Account & meta
- **PlayerProfile** — API keys, win/loss, stats cards.
- **PublicProfile**, **PlayerHistory**, **PlayerStats**, **PlayersList**, **UnitDefinitions** (codex).

### Economy & tournaments
- **Shop**, **Economy**.
- **TournamentList**, **TournamentDetail**, **TournamentResults**.

### Admin (role-gated)
- **AdminGames**, **AdminPlayers**, **AdminTickControl**, **AdminStats**, **AdminMetrics**, **AdminAuditLog**, **AdminReports**.

### Misc
- **Help** — mechanics reference.

---

## 6. Component Patterns

### Containers
- **Card**: `rounded-lg border bg-card p-4`. The universal surface.
- **Section**: card + `border-b px-4 py-2.5` header strip.
- **Modal**: full-screen `inset-0 bg-black/60` backdrop + centered `max-w-md` card, `role="dialog"`.
- **Table**: wrap in `overflow-x-auto`; `thead` uses `font-medium text-muted-foreground`; rows `border-b`; some columns `hidden sm:table-cell` on mobile.

### Forms
- Inputs / selects / textareas share `rounded border bg-input px-2 py-1.5 text-sm`.
- Labels: `text-xs text-muted-foreground mb-1 block` above the control.
- Validation is mostly server-side; errors render inline in `text-destructive` or as an alert box `border border-destructive/50 bg-destructive/10 px-3 py-2`.

### Buttons
| Variant | Classes |
|---|---|
| Primary | `bg-primary text-primary-foreground hover:opacity-90` |
| Secondary | `bg-secondary text-secondary-foreground` |
| Destructive | `bg-destructive text-destructive-foreground` |
| Ghost / outline | `border text-sm hover:bg-muted` |
| Icon | `p-1.5 rounded hover:bg-secondary` |

Disabled: `disabled:opacity-50`. Loading states change the label ("Building…") and disable the button — no spinners inside buttons.

### Data display
- **Progress bars**: nested div, outer `overflow-hidden`, inner `transition-all` width + `role="progressbar"` with ARIA values.
- **Badges**: `rounded-full px-2 py-0.5 text-xs`.
- **Stat cards**: 3-column grid (e.g. total / minerals / gas) with large mono numbers + label underneath.
- **EmptyState**: icon + title + description for zero-result lists.

### Domain components
- **PlayerResources** — header resource strip, mono values, responsive gaps.
- **BuildQueue** — priority-ordered list card, remove per entry (no reorder UI).
- **VictoryProgressBar** — player + top-3 competitors with color-coded bars.
- **CostBadge** — inline resource cost chip.
- **WorkerAssignment** — 3-stat header + two assignment inputs.
- **NotificationBell** — icon button with count badge, opens a notification panel.
- **NotificationToasts** — top-right stack, max 3 visible, 5s auto-dismiss, clickable to navigate; `animate-in slide-in-from-right-4 fade-in duration-300`.

---

## 7. Interaction Patterns

### Build & queue
Asset cards on Base show locked / available / affordable states. **Build** executes immediately; **Queue** always available and appends to the build queue with tick cost. Success mutations invalidate queries to refresh live state.

### Unit management
Units page separates **deployed** and **home** into two tables with Merge / Split / Attack per row. Split opens an inline count input. Attack routes to `SelectEnemy` → target → confirm.

### Market & trade
Market has two tabs (mine / open). Posting takes resource dropdowns + amount inputs. Direct `Trade` screen is player-to-player with target selector, resource inputs, optional note, and incoming/sent/history tabs.

### Research
Two horizontal tracks (Attack, Defense). Nodes display status (completed / in-progress / available / locked), cost, and a research button. Only one upgrade may be in progress at a time — the UI blocks starting another.

### Chat & messages
Chat scrolls to latest on new message; SignalR-first, 5s polling fallback. Messages use an inbox + inline reply pattern.

### Diplomacy
Active agreements, incoming (needs response), outgoing. Propose via form (target, duration in ticks, terms). Respond = accept/decline inline.

### Game lifecycle
Games page card grid. Game-over event triggers a full-screen modal with an 8-second countdown to results plus a "View Results Now" button. A game-ended banner remains visible afterward inside `GameLayout`.

### Admin
Create-game form with schedule/tick/max-players/Discord webhook, manage-table actions, moderation queue, and a tick control page exposing pause / advance / resume.

---

## 8. Real-Time Model

- Single SignalR connection from `GameLayout` via `useSignalR('/gamehub')`.
- Handlers: `ReceiveChatMessage`, `GameFinalized`, `LobbyUpdated`, plus notification events.
- Reconnect schedule: `[0, 2000, 5000, 10000, 30000]` ms.
- Connection status drives the header WiFi icon (`disconnected | connecting | connected | reconnecting`).
- **Fallback**: when status is not `connected`, polling queries step in (chat at 5s, others 10–30s depending on page).
- **Optimistic updates** are rare; the default is "mutate → invalidate → refetch". The exception is chat, where messages append optimistically and dedupe on server echo.

---

## 9. Information Density & Hierarchy

**Base page** is the densest view. Reading order, top to bottom:

1. Title
2. `VictoryProgressBar` (player vs top 3)
3. `WorkerAssignment`
4. Trade / Colonize panels
5. Asset grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`)
6. `BuildQueue`
7. Resource history chart (Recharts)
8. Recent activity (scrollable, dismissible)

Other density-heavy views:

- **Units** — summary row + two tables; stats columns hidden on mobile.
- **Research** — narrow (`w-36`) node cards arranged horizontally, can overflow-scroll.
- **Games / Lobby** — card grid, 1/2/3 cols across breakpoints.
- **GameLiveView** — distributed panels (rankings / resources / notifications / assets / units) with a custom `useTouchZoomPan` hook for mobile pinch-zoom and pan.

---

## 10. Responsive Design

Breakpoints used: **`sm` 640**, **`md` 768** (main sidebar breakpoint), **`lg` 1024**. `xl`/`2xl` barely appear.

Patterns:
- Sidebar collapses to a drawer below `md`.
- Header text scales (`text-xs sm:text-sm`), gaps shrink (`gap-2 sm:gap-4`).
- Tables default to `overflow-x-auto`; some columns hide (`hidden sm:table-cell`) with **no alternative card-per-row layout** — this is a known rough edge.
- Grids cascade `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`.
- Touch targets ≥44px for nav and primary actions.

---

## 11. Accessibility

Baseline, not exhaustive.

**Good**
- Semantic HTML: `main`, `header`, `aside`, `nav aria-label`, `<table>` with `th scope`.
- ARIA: `aria-label` on icon buttons, `aria-hidden` on decorative glyphs, `role="alert"` on error banners, `role="progressbar"` with `aria-valuenow/min/max`, `role="dialog"` + `aria-modal` on overlays.
- Focus: tutorial overlay focuses its dialog on open and handles `Escape`.
- Motion: page-fade-in respects `prefers-reduced-motion`.

**Gaps**
- No custom focus trapping for most modals (relies on native tab order).
- Research tree and GameLiveView panels are not fully keyboard-navigable.
- Not every icon button carries an `aria-label`.
- Color contrast hasn't been formally verified against WCAG AA.

---

## 12. Known Rough Edges

A candid list, useful as a backlog seed.

1. **Thin client-side validation** — most forms defer to server errors, which surface as generic messages.
2. **No skeleton loaders** — "Loading…" text in many places; no perceived-performance scaffolding.
3. **No confirmation dialogs on destructive actions** — cancel/delete/revoke execute immediately.
4. **No reordering in `BuildQueue`** — only removal.
5. **Tables on mobile hide columns** with no card-view fallback, so some data is simply invisible on phones.
6. **Generic error text** — no error codes, no suggested recovery.
7. **No draft/autosave** — forms are ephemeral.
8. **Emoji used as icons** in several places alongside Lucide — render inconsistency across platforms.
9. **No pagination / infinite scroll** — long lists rely on truncation or "show all" toggles, which will not scale.
10. **Refetch intervals are hard-coded** (10s / 30s) — not configurable per user or per connection quality.
11. **Limited optimistic UI** outside chat — most actions feel "submit → wait → refresh".

---

## 13. Conventions Cheat Sheet

Use this when adding new UI to stay consistent with the existing system.

- **Surface** = `rounded-lg border bg-card p-4`.
- **Section header** = card + `border-b px-4 py-2.5` strip.
- **Numbers** = `font-mono`. Always.
- **Labels** = `text-xs text-muted-foreground`.
- **Primary action** = `bg-primary text-primary-foreground`. One per view, ideally.
- **Destructive action** = `bg-destructive` + the word "Delete/Cancel/Remove" in the label.
- **Loading** = change the button label, disable it. No spinners inside buttons.
- **Empty** = `EmptyState` component, not ad-hoc messages.
- **Error** = inline `text-destructive` for field errors; `border border-destructive/50 bg-destructive/10` box for form-level errors.
- **Responsive** = `p-3 sm:p-4`, `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`, sidebar hidden below `md`.
- **Data fetching** = TanStack Query; set an explicit `refetchInterval` if the data is tick-driven.
- **Mutations** = invalidate related queries on success; don't hand-roll cache updates unless you have a reason.
- **Real-time** = subscribe in `GameLayout` via `useSignalR`; fall back to polling when status ≠ `connected`.
- **Routes** = lazy-load new game pages via `React.lazy` + `RouteWithBoundary`; keep `/signin` and `/` eager.
- **Icons** = Lucide by default; avoid adding new emoji-as-icon usages.
- **Tokens** = reference CSS vars / Tailwind theme colors; don't hard-code hex values.
