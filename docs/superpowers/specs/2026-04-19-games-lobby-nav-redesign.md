# Games List & Briefing — Navigation UX Redesign Spec

**Status:** Draft · awaiting user review
**Date:** 2026-04-19
**Part of:** `feat/sco-flair-redesign` branch (the SCO flair PR)
**Follows:** [`2026-04-19-sco-flair-redesign-design.md`](2026-04-19-sco-flair-redesign-design.md) — the shell/tokens/primitives landed in Phase 1 of that plan

---

## 1. Intent

Clean up the navigation mess around game listing, joining, spectating, and returning to the list. Before this change the client has two parallel game-list pages (`/games`, `/lobby`), two different Join code paths (one of which skips race selection), three Spectate destinations, an orphaned `/games/:id/spectate` route, and no back-link from inside a game after the recent shell rewrite. Players who register for an Upcoming game have nowhere coherent to wait.

After this change there is **one list page**, **one pre-game briefing page**, **one Join flow**, **one Spectate target**, **one Results target**, and a restored back-link in the in-game sidebar.

## 2. Problems being solved (in priority order)

1. **Two duplicate list pages** — `/games` (Games.tsx, card grid) and `/lobby` (GameLobby.tsx, table). Users don't know which to use.
2. **Two Join paths with different semantics** — `/games` card's "Join" is a direct POST that skips race selection; `/lobby` routes to the race picker form. The first path is a bug in disguise.
3. **Three Spectate destinations** — `/games/:id/live` (GameLiveView, scoreboard), `/games/:id/ranking` (PlayerRanking, misused as "Spectate" from `/lobby`), `/games/:id/spectate` (orphaned Spectator.tsx).
4. **No back-link from inside a game** — Phase 1 of the SCO flair rewrite dropped the "← All Games" sidebar link; there is now no visible way to return to the list once inside `/games/:id/base`.
5. **Pre-game limbo** — a player registers for an Upcoming game and has nowhere meaningful to land. `/lobby/:id` (GameLobbyDetail) exists as a pre-game view but is buried behind the admin-flavored `/lobby` table and isn't reachable from `/games`.
6. **Finished-game flow forks** — `/games` card points to `/results`, `/lobby` table row points to `/ranking`. Inconsistent.
7. **Status sections look nearly identical** — Active / Upcoming / Finished share the same card chrome; it's hard to scan at a glance which games are which.

## 3. Decisions locked during brainstorming

| Decision | Choice | Why / alternative rejected |
|---|---|---|
| Who creates games | **Admins only** (via existing `/admin/games`) | User-facing list is read-only; the `/lobby` inline Create Game form is retired |
| List layout | **Table** — three sections (Active / Upcoming / Finished) with state-specific columns | Rejected: card grid (too sparse for SCO's dense-data ethic) |
| Pre-game page | **`/games/:id/briefing`** — one page for all pre-game and spectator landing states | Renamed from `/lobby/:id`; absorbs `JoinGame.tsx`'s race-picker form |
| Join flow | **Always through the briefing page's race picker** | The direct POST shortcut on the card is deleted |
| Spectate target | **`/games/:id/live`** is the single destination | `/games/:id/ranking` remains as a page but is no longer a "Spectate" entry; `/games/:id/spectate` route + Spectator.tsx are deleted |
| Results target | **`/games/:id/results`** | `/games/:id/ranking` does not double as results |
| In-game back-link | **Sidebar top: "← All Games" link below the race-identity block** | Restored behavior that existed before the Phase 1 shell rewrite |
| `/lobby` routes | **Deleted** | `/lobby` and `/lobby/:id` removed from App routing |

## 4. The `/games` page

### 4.1 Shape

Plain list page living at `/games`. Stays outside the `GameLayout` (no in-game sidebar). Three stacked sections in order:

1. **Active** — games currently ticking
2. **Upcoming** — games registered and scheduled
3. **Finished** — completed games (collapsed by default, "show all N" link if >5)

### 4.2 Each section is a table

Each section renders its own `<table>` using the `DataGrid`-adjacent pattern already established in Phase 1 (no outer card wrapper, hair-thin row dividers, uppercase label header, starfield background visible).

Columns are **state-specific** — not a single schema across all three:

**Active columns:** Status badge · Game name + subline · Players · Round · Ends (countdown) · Actions
**Upcoming columns:** Status badge · Game name + subline · Registered · Tick interval · Starts in (countdown) · Actions
**Finished columns:** Status badge · Game name · Players · Winner (race-colored name) · Ended (date) · Actions

### 4.3 Status badges

Left-border style (matches the SCO flair aesthetic — no rounded pill shapes):

- **Live** — `bg-resource/15`, `text-resource`, 2px left border `--color-resource`
- **Upcoming** — `bg-warning/15`, `text-prestige`, 2px left border `--color-prestige`
- **Finished** — `bg-muted/8`, `text-muted-foreground`, 2px left border `--color-muted-foreground`

### 4.4 Row highlight for enrolled games

Rows where the current player is enrolled or registered get a `border-left-2 border-resource` and a subtle green tint (`bg-resource/4`). The subline under the game name reads: "you are enrolled · Terran" (or "you are registered · Zerg"), with race name in the race-color.

### 4.5 Action column per state

The rightmost cell shows only the actions valid for that state × enrollment combination:

| Game state | Enrolled? | Primary action | Secondary actions |
|---|---|---|---|
| Active | Yes | **Enter →** (green, links to `/games/:id/base`) | Spectate (links to `/games/:id/live`) |
| Active | No | **Join** (amber, links to `/games/:id/briefing`) | Spectate |
| Upcoming | Yes | **Briefing →** (green, links to `/games/:id/briefing`) | — |
| Upcoming | No | **Register** (amber, links to `/games/:id/briefing`) | Details (same link, dim) |
| Finished | — | Results (links to `/games/:id/results`) | Replay (links to `/replays/:id`) |

Actions render as text links (`text-interactive hover:underline`) except the primary action which takes the state-appropriate semantic color (green=enter, amber=register/join).

### 4.6 Empty states

- No Active games: "No games are currently running. Check upcoming seasons below or come back later."
- No Upcoming games: "No upcoming games scheduled yet. Admins post new seasons here."
- No Finished games: section hidden entirely (not rendered).

### 4.7 Refresh

`useQuery` on `/api/games` with `refetchInterval: 30_000`. `LobbyUpdated` SignalR event on `/gamehub` invalidates the query — this is the existing mechanism; it moves from `GameLobby.tsx` to `Games.tsx`. The meta line "auto-refresh on" is a passive indicator; clicking it toggles the interval.

## 5. The `/games/:id/briefing` page

Replaces `GameLobbyDetail.tsx` (same route parameter) and absorbs `JoinGame.tsx`'s race-picker form. Renamed route path: `/games/:id/briefing` (not `/lobby/:id`).

Lives **outside the `GameLayout`** — the user doesn't have in-game race chrome for this game yet (or hasn't joined). Uses the base `RootLayout` with the starfield background, like `/games`.

### 5.1 Header (always shown)

- "← All Games" text link (back to `/games`)
- `<h1>` game name + status badge inline
- Subline: game def type, tournament association if any, enrollment state if relevant (e.g., "You are registered · Zerg · Overmind")

### 5.2 Body — one of four states

The page renders one primary "hero" block based on `(status, enrolled)`:

**A. Upcoming + not registered**
1. "Starts in" block — `cd` class (30px mono prestige-color numerals) + absolute time below
2. "Rules" block — 4-column stats (Tick, End tick, Starting resources, Protection)
3. "Register — choose your race" — three large race tiles (emblem + race name + tagline), commander-name input, "Register as X" button (amber primary)
4. "Registered commanders" list — mono list, race-colored names, join date

**B. Upcoming + registered**
1. "Starts in" block (same as A.1)
2. One-line confirmation: "You're in. The game hasn't started yet."
3. Registered commanders list (same as A.4)

**C. Active + not enrolled** (open-join games)
1. Subline: "Round 22 / 50 · 6 commanders · open join"
2. CTAs: "Join & enter game" (green primary) · "Spectate live →" (blue secondary)
3. "Commanders" list with land / unit count / online state per entry

**D. Active + enrolled** — users arriving here from an existing bookmark or intentional navigation
1. Subline: "You are enrolled · Terran · Commander Chris · Round 14 / 30"
2. CTAs: "Enter game →" (green primary) · "Live scoreboard" (blue secondary)
3. "Rules" stats block with the player's rank as one of the stats

### 5.3 Finished games have no briefing page

Attempting to navigate to `/games/:id/briefing` for a Finished game redirects to `/games/:id/results`. The row on `/games` for Finished games goes directly to `/results` or `/replays/:id`.

### 5.4 Race tiles

Three tiles, each ~180px tall, rendering the existing `TerranEmblem` / `ZergEmblem` / `ProtossEmblem` components at 44–56px above a `label`-style race name and a one-line description. Selected tile gets a 2px race-colored border; unselected tiles get `border-border`. Selection is a controlled React state on the page, NOT `data-race` on `<html>` (because the user hasn't actually committed to the race yet).

The commander-name input and "Register as <Race>" button live below the tiles. Button label updates live with the selection.

## 6. In-game back-link

In `GameLayout.tsx`, inside `SidebarContent`, after the race-identity block and before `<NavMenu />`, insert:

```tsx
<NavLink
  to="/games"
  className="flex items-center gap-1.5 px-4 py-2 text-[11px] text-interactive hover:text-[color:var(--color-race-secondary)] border-b border-border"
>
  <ArrowLeftIcon className="h-3 w-3" aria-hidden /> All Games
</NavLink>
```

Small, sits above the main nav list, always visible. No race theming on this link itself (it's "leaving the race context"), but it does hover to the race-secondary color.

## 7. Routing changes

Additions:

| Route | Component | Layout |
|---|---|---|
| `/games/:id/briefing` | `GameBriefing` (new; renamed from `GameLobbyDetail`) | `RootLayout` (outside in-game) |

Removals:

| Route | Component | Reason |
|---|---|---|
| `/lobby` | `GameLobby` | Duplicate of `/games`; admins use `/admin/games` |
| `/lobby/:gameId` | `GameLobbyDetail` | Renamed/moved to `/games/:id/briefing` |
| `/games/:gameId/spectate` | `Spectator` | Orphaned; `/live` is the spectate target |
| `/games/:gameId/join` | `JoinGame` | Folded into briefing page |

Behavior note: visiting the game name in a `/games` row is not clickable — only the action cells navigate. This avoids hidden-click UX and keeps keyboard navigation explicit. Clicking "Details" on an Upcoming row or "Register" / "Briefing →" all route to the same `/games/:id/briefing` page.

## 8. Files to delete, create, modify

### Delete

- `src/ReactClient/src/pages/GameLobby.tsx`
- `src/ReactClient/src/pages/JoinGame.tsx`
- `src/ReactClient/src/pages/Spectator.tsx`

### Create

- `src/ReactClient/src/components/games/GamesTable.tsx` — the row renderer for the three sections on `/games`
- `src/ReactClient/src/components/games/GameCountdown.tsx` — the large mono countdown used on briefing hero ("Starts in" / "Ends in") — wraps the existing `Countdown` with the larger typography
- `src/ReactClient/src/components/games/RaceTile.tsx` — single race tile button, used by `GameBriefing` and reusable elsewhere

### Rename + rewrite

- `src/ReactClient/src/pages/GameLobbyDetail.tsx` → `src/ReactClient/src/pages/GameBriefing.tsx` — keeps the querying + player list code; restructures into four render states (§5.2); absorbs the race-picker form from `JoinGame.tsx`

### Modify

- `src/ReactClient/src/pages/Games.tsx` — rewritten to use `GamesTable` three times; existing `GameCard` helper deleted
- `src/ReactClient/src/layouts/GameLayout.tsx` — add "← All Games" link to `SidebarContent`
- `src/ReactClient/src/App.tsx` — update routing per §7
- `src/BrowserGameEngine.Shared/GameLobbyViewModel.cs` — optional rename to `GameBriefingViewModel` (lower priority; defer to a follow-up if it introduces too much churn in backend)

### Backend

Primary ask: the viewmodel on `/api/games/:id/lobby` needs to include the currentPlayer's enrollment state and race so the briefing page can render without an additional request. Check `GameLobbyViewModel.cs` — it currently has `canJoin`. It should also expose:

- `isCurrentPlayerEnrolled: bool`
- `currentPlayerRace: string | null`
- `currentPlayerName: string | null`
- `currentPlayerRank: int | null` (Active games only)

If these fields don't exist, add them and populate them in the matching controller. Endpoint path stays `/api/games/:id/lobby` (no rename to avoid churn).

Also: the `/api/games/:gameId/spectate` endpoint backing the orphan Spectator.tsx can be deleted alongside the frontend, if it has no other consumers.

## 9. Out of scope

- Changing the in-game `/games/:id/ranking` page — it continues to exist as an in-game-nav page (utility footer link). We only stop calling it "Spectate."
- Results page redesign (`GameResults.tsx`) — separate concern; may get touched later in the SCO flair redesign plan.
- Admin page redesign (`/admin/games`) — out of scope.
- Tournament list/detail — out of scope.
- Live view (`GameLiveView.tsx`) visual redesign — it's the Spectate target but its internals stay as-is.

## 10. Phasing / commit plan

Single-PR work, two logical commits (these add to the existing `feat/sco-flair-redesign` branch PR):

**Commit 1 — backend + types**
- Add `isCurrentPlayerEnrolled`, `currentPlayerRace`, `currentPlayerName`, `currentPlayerRank` to `GameLobbyViewModel` + populate in controller.
- Mirror fields in `api/types.ts`.

**Commit 2 — frontend rewrite**
- Create `GamesTable`, `GameCountdown`, `RaceTile` components
- Rewrite `Games.tsx`
- Rename `GameLobbyDetail.tsx` → `GameBriefing.tsx`; absorb `JoinGame.tsx` content; replace routing
- Delete `GameLobby.tsx`, `JoinGame.tsx`, `Spectator.tsx`
- Update `App.tsx` routes per §7
- Add "← All Games" link to `GameLayout`
- Update E2E selectors that reference old routes / "Game Lobby" text

Between commits the app must build; Commit 1 is safe even if the frontend hasn't been rebuilt because all new fields are additive.

## 11. Success criteria

- `grep -c "GameLobby\.tsx\|JoinGame\.tsx\|Spectator\.tsx" src/ReactClient/src/App.tsx` → 0
- Any `/games` row with enrollment shows the green left-border highlight
- All Join / Register actions route through `/games/:id/briefing`; no direct-POST shortcut remains
- All Spectate actions route to `/games/:id/live`
- In-game sidebar shows "← All Games" above the nav list
- `/lobby` hitting the browser gives a 404 or redirect to `/games` (router decides)

## 12. Risks

- **Backend field additions may break tests** — the viewmodel is a record; adding nullable fields should be additive but verify `dotnet test` after.
- **E2E tests reference "Game Lobby" text** — needs sweeping update; small but not zero work.
- **Bookmarks to `/lobby` will 404** — acceptable; internal tool with known users.
- **Users with open `/games/:id/join` tabs** will 404 after the rename. They re-open briefing. Acceptable.
