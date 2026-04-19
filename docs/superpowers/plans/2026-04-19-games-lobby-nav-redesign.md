# Games List & Briefing — Navigation UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the duplicated `/games` + `/lobby` pair with one table-based list page at `/games`, one pre-game briefing page at `/games/:id/briefing` that absorbs `JoinGame.tsx`, and a restored "← All Games" link inside the game sidebar — deleting `GameLobby.tsx`, `JoinGame.tsx`, and `Spectator.tsx` along the way.

**Architecture:** Two logical commits stacked on the existing `feat/sco-flair-redesign` branch. Commit 1 extends the backend `GameLobbyViewModel` with four current-player fields so the briefing page can render its four states from a single request. Commit 2 rewrites the frontend: three new small components (`RaceTile`, `GameCountdown`, `GamesTable`), rewrite of `Games.tsx`, rename + rewrite of `GameLobbyDetail.tsx` → `GameBriefing.tsx`, three file deletions, routing updates, and the sidebar back-link.

**Tech Stack:** ASP.NET Core (backend), React 19 + TypeScript + Tailwind v4 + TanStack Query v5 + React Router v7 (frontend). Tests: vitest + testing-library.

**Reference:** [`docs/superpowers/specs/2026-04-19-games-lobby-nav-redesign.md`](../specs/2026-04-19-games-lobby-nav-redesign.md) (approved spec).

**Branch:** Continue on `feat/sco-flair-redesign` in worktree `/tmp/bge-work/sco-flair-redesign`. All work is additional commits on that branch — the same PR #319.

---

## File structure

### New

- `src/ReactClient/src/components/games/RaceTile.tsx` — single race tile button (~60 lines)
- `src/ReactClient/src/components/games/GameCountdown.tsx` — large mono countdown for briefing hero (~30 lines)
- `src/ReactClient/src/components/games/GamesTable.tsx` — row renderer for the three sections (~150 lines)
- `src/ReactClient/src/pages/GameBriefing.tsx` — unified pre-game page with four render states (~350 lines); effectively replaces `GameLobbyDetail.tsx` after rename
- `src/ReactClient/test/components/games/RaceTile.test.tsx`
- `src/ReactClient/test/components/games/GameCountdown.test.tsx`

### Delete

- `src/ReactClient/src/pages/GameLobby.tsx`
- `src/ReactClient/src/pages/GameLobbyDetail.tsx` (content moves into `GameBriefing.tsx`)
- `src/ReactClient/src/pages/JoinGame.tsx`
- `src/ReactClient/src/pages/Spectator.tsx`

### Modify

- `src/BrowserGameEngine.Shared/GameLobbyViewModels.cs` — add four fields to `GameLobbyViewModel`
- `src/BrowserGameEngine.FrontendServer/Controllers/GamesController.cs` — populate the new fields in `GetLobby`
- `src/ReactClient/src/api/types.ts` — mirror the new fields on `GameLobbyViewModel`
- `src/ReactClient/src/pages/Games.tsx` — rewrite using `GamesTable`
- `src/ReactClient/src/layouts/GameLayout.tsx` — add "← All Games" link
- `src/ReactClient/src/App.tsx` — remove deleted routes, update `/lobby/:id` → `/games/:id/briefing`

---

## Task 1: Backend — extend `GameLobbyViewModel` with current-player fields

**Files:**
- Modify: `src/BrowserGameEngine.Shared/GameLobbyViewModels.cs`

- [ ] **Step 1: Extend the record**

Change `GameLobbyViewModel` from:

```csharp
public record GameLobbyViewModel(
    string GameId,
    string GameName,
    string Status,
    int MaxPlayers,
    DateTime? StartTime,
    DateTime? EndTime,
    List<LobbyPlayerViewModel> Players,
    bool CanJoin = false,
    GameSettingsViewModel? Settings = null
);
```

to:

```csharp
public record GameLobbyViewModel(
    string GameId,
    string GameName,
    string Status,
    int MaxPlayers,
    DateTime? StartTime,
    DateTime? EndTime,
    List<LobbyPlayerViewModel> Players,
    bool CanJoin = false,
    GameSettingsViewModel? Settings = null,
    bool IsCurrentPlayerEnrolled = false,
    string? CurrentPlayerId = null,
    string? CurrentPlayerName = null,
    string? CurrentPlayerRace = null,
    int? CurrentPlayerRank = null
);
```

- [ ] **Step 2: Build**

```bash
cd /tmp/bge-work/sco-flair-redesign/src
dotnet build --configuration Release
```

Expected: build succeeds. Record extension is additive and all existing call-sites still work because new fields are optional.

- [ ] **Step 3: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/BrowserGameEngine.Shared/GameLobbyViewModels.cs
git commit -m "feat(api): extend GameLobbyViewModel with current-player fields

Adds IsCurrentPlayerEnrolled, CurrentPlayerId/Name/Race/Rank so the
frontend briefing page can render all four states (upcoming±registered,
active±enrolled) from a single /api/games/:id/lobby request."
```

## Task 2: Backend — populate current-player fields in `GetLobby`

**Files:**
- Modify: `src/BrowserGameEngine.FrontendServer/Controllers/GamesController.cs:281-330`

**Context:** The controller's `GetLobby` action is `[AllowAnonymous]` — anonymous callers see the same data as logged-in users. The new fields must be null / false when the caller is anonymous, and populated when the caller is an authenticated player enrolled in this game.

The controller already injects `currentUserContext`. Use `currentUserContext.IsValid` to detect authentication, `currentUserContext.PlayerId` for the active player id.

Rank for Active games: the `PlayerRankingRepository` (or equivalent existing service — grep for `ranking` or `rank` in `StatefulGameServer` to find the right one) computes player rank by land. For Upcoming games, rank is null because the game hasn't started ticking yet.

- [ ] **Step 1: Identify the rank service**

```bash
cd /tmp/bge-work/sco-flair-redesign
grep -rn "public.*Rank\|GetRank\|RankRepository" src/BrowserGameEngine.StatefulGameServer --include="*.cs" | head -10
```

Inspect the result — it will reveal the class and method name. Existing ranking logic is already used in `PlayerRankingController.cs`; look there first for patterns.

- [ ] **Step 2: Inject the rank dependency**

If the rank computation is a repository (e.g., `PlayerRankingRepository`), add it to `GamesController`'s constructor parameters alongside the existing injections. If it's a method on an already-injected repository, no injection change needed.

- [ ] **Step 3: Rewrite the `GetLobby` return block**

Change the `return Ok(new GameLobbyViewModel(...))` block in `GamesController.cs` at approximately line 318 to:

```csharp
            // Current-player fields (only populated for authenticated callers whose player
            // matches one of the enrolled lobby players). Anonymous callers get the defaults.
            bool isEnrolled = false;
            string? currentPlayerId = null;
            string? currentPlayerName = null;
            string? currentPlayerRace = null;
            int? currentPlayerRank = null;

            if (currentUserContext.IsValid && currentUserContext.PlayerId != null) {
                var myPlayerId = currentUserContext.PlayerId.Id;
                var me = players.FirstOrDefault(p => p.PlayerId == myPlayerId);
                if (me != null) {
                    isEnrolled = true;
                    currentPlayerId = me.PlayerId;
                    currentPlayerName = me.PlayerName;
                    currentPlayerRace = me.PlayerType;

                    if (record.Status == GameStatus.Active && instance != null) {
                        currentPlayerRank = playerRankingRepository.GetRank(instance, currentUserContext.PlayerId);
                    }
                }
            }

            return Ok(new GameLobbyViewModel(
                GameId: record.GameId.Id,
                GameName: record.Name,
                Status: record.Status.ToString(),
                MaxPlayers: record.MaxPlayers,
                StartTime: record.StartTime,
                EndTime: record.EndTime,
                Players: players,
                CanJoin: canJoin,
                Settings: settingsVm,
                IsCurrentPlayerEnrolled: isEnrolled,
                CurrentPlayerId: currentPlayerId,
                CurrentPlayerName: currentPlayerName,
                CurrentPlayerRace: currentPlayerRace,
                CurrentPlayerRank: currentPlayerRank
            ));
```

Note: the exact API of `playerRankingRepository.GetRank(instance, playerId)` depends on what you found in Step 1. Match that signature — if the method takes different parameters, adjust the call accordingly. If it returns `int` not `int?`, leave the existing behavior unless a game has no active players (should not happen in Active state).

- [ ] **Step 4: Build and test**

```bash
cd /tmp/bge-work/sco-flair-redesign/src
dotnet build --configuration Release
dotnet test --filter "FullyQualifiedName~GamesController|FullyQualifiedName~Lobby"
```

Expected: build succeeds. Test run: any existing tests for `GetLobby` should continue to pass (new fields default to sensible values for anonymous callers).

- [ ] **Step 5: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/BrowserGameEngine.FrontendServer/Controllers/GamesController.cs
git commit -m "feat(api): populate current-player fields in GetLobby"
```

## Task 3: Frontend — mirror new lobby fields in api/types.ts

**Files:**
- Modify: `src/ReactClient/src/api/types.ts:796-807`

- [ ] **Step 1: Replace the `GameLobbyViewModel` interface**

Find the existing interface (around line 796). Replace with:

```typescript
export interface GameLobbyViewModel {
  gameId: string
  gameName: string
  status: string
  maxPlayers: number
  startTime: string | null
  endTime: string | null
  players: LobbyPlayerViewModel[]
  canJoin: boolean
  settings?: GameSettingsViewModel | null
  isCurrentPlayerEnrolled: boolean
  currentPlayerId: string | null
  currentPlayerName: string | null
  currentPlayerRace: string | null
  currentPlayerRank: number | null
}
```

- [ ] **Step 2: Typecheck**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit
```

Expected: no errors. If existing `GameLobbyDetail.tsx` references the old shape, its code still compiles because we only added fields, never removed.

- [ ] **Step 3: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/api/types.ts
git commit -m "feat(client): mirror GameLobbyViewModel current-player fields"
```

## Task 4: `RaceTile` component

**Files:**
- Create: `src/ReactClient/src/components/games/RaceTile.tsx`
- Test: `src/ReactClient/test/components/games/RaceTile.test.tsx`

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/components/games/RaceTile.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { RaceTile } from '@/components/games/RaceTile'

describe('RaceTile', () => {
  it('renders the race name and description', () => {
    render(<RaceTile race="terran" selected={false} onSelect={() => {}} />)
    expect(screen.getByText(/terran/i)).toBeDefined()
    expect(screen.getByText(/industrial/i)).toBeDefined()
  })

  it('calls onSelect with the race when clicked', () => {
    const onSelect = vi.fn()
    render(<RaceTile race="zerg" selected={false} onSelect={onSelect} />)
    fireEvent.click(screen.getByRole('button'))
    expect(onSelect).toHaveBeenCalledWith('zerg')
  })

  it('applies aria-pressed when selected', () => {
    render(<RaceTile race="protoss" selected={true} onSelect={() => {}} />)
    expect(screen.getByRole('button').getAttribute('aria-pressed')).toBe('true')
  })
})
```

- [ ] **Step 2: Run test — expect failure (module missing)**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx vitest run test/components/games/RaceTile.test.tsx
```

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/components/games/RaceTile.tsx`:

```tsx
import type { CSSProperties } from 'react'
import { cn } from '@/lib/utils'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'
import type { Race } from '@/lib/race'

const DESCRIPTIONS: Record<Race, { name: string; tagline: string }> = {
  terran: { name: 'Terran', tagline: 'Industrial · balanced · mechanized' },
  zerg: { name: 'Zerg', tagline: 'Organic · cheap units · swarm' },
  protoss: { name: 'Protoss', tagline: 'Crystalline · expensive · elite' },
}

const ACCENT_STYLES: Record<Race, CSSProperties> = {
  terran: {
    ['--color-race-primary' as string]: '#CBD5E1',
    ['--color-race-secondary' as string]: '#F59E0B',
    ['--color-race-emblem-fill' as string]: '#1f2937',
  },
  zerg: {
    ['--color-race-primary' as string]: '#B45309',
    ['--color-race-secondary' as string]: '#ea580c',
    ['--color-race-emblem-fill' as string]: '#3a1a08',
  },
  protoss: {
    ['--color-race-primary' as string]: '#FBBF24',
    ['--color-race-secondary' as string]: '#0e7490',
    ['--color-race-emblem-fill' as string]: '#0c1e2e',
  },
}

const EMBLEMS: Record<Race, React.ComponentType<React.SVGProps<SVGSVGElement>>> = {
  terran: TerranEmblem,
  zerg: ZergEmblem,
  protoss: ProtossEmblem,
}

export function RaceTile({
  race,
  selected,
  onSelect,
}: {
  race: Race
  selected: boolean
  onSelect: (race: Race) => void
}) {
  const { name, tagline } = DESCRIPTIONS[race]
  const Emblem = EMBLEMS[race]
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={() => onSelect(race)}
      style={ACCENT_STYLES[race]}
      className={cn(
        'flex-1 flex flex-col items-center gap-1.5 p-4 min-h-[150px] transition-colors bg-background/40 cursor-pointer',
        selected
          ? 'border-2 border-[color:var(--color-race-primary)] bg-[color:var(--color-race-primary)]/5'
          : 'border border-border hover:border-[color:var(--color-race-primary)]/60',
      )}
    >
      <Emblem width={56} height={56} style={{ color: 'var(--color-race-primary)' }} />
      <div className="label mt-1" style={{ color: 'var(--color-race-primary)' }}>{name}</div>
      <div className="text-[10.5px] text-muted-foreground text-center leading-tight">{tagline}</div>
    </button>
  )
}
```

- [ ] **Step 4: Run tests — expect 3 pass**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx vitest run test/components/games/RaceTile.test.tsx
```

- [ ] **Step 5: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/components/games/RaceTile.tsx src/ReactClient/test/components/games/RaceTile.test.tsx
git commit -m "feat(ui): RaceTile — race selector button with emblem, name, tagline"
```

## Task 5: `GameCountdown` component

**Files:**
- Create: `src/ReactClient/src/components/games/GameCountdown.tsx`
- Test: `src/ReactClient/test/components/games/GameCountdown.test.tsx`

This component wraps a large mono countdown in the prestige (amber) color, used by the briefing page's "Starts in" / "Ends in" hero blocks.

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/components/games/GameCountdown.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { GameCountdown } from '@/components/games/GameCountdown'

describe('GameCountdown', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('shows days + hours + minutes when over 1 day away', () => {
    const to = new Date(Date.now() + (3 * 86400_000 + 12 * 3600_000 + 4 * 60_000)).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^3d 12h 04m$/)).toBeDefined()
  })

  it('shows hours + minutes when under 1 day', () => {
    const to = new Date(Date.now() + (5 * 3600_000 + 30 * 60_000)).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^5h 30m$/)).toBeDefined()
  })

  it('shows "0m" when the target is in the past', () => {
    const to = new Date(Date.now() - 60_000).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^0m$/)).toBeDefined()
  })
})
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx vitest run test/components/games/GameCountdown.test.tsx
```

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/components/games/GameCountdown.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'

interface GameCountdownProps {
  to: string
  className?: string
}

function format(ms: number): string {
  if (ms <= 0) return '0m'
  const totalMin = Math.floor(ms / 60_000)
  const d = Math.floor(totalMin / (24 * 60))
  const h = Math.floor((totalMin % (24 * 60)) / 60)
  const m = totalMin % 60
  const mm = m.toString().padStart(2, '0')
  if (d > 0) return `${d}d ${h}h ${mm}m`
  if (h > 0) return `${h}h ${m}m`
  return `${m}m`
}

export function GameCountdown({ to, className }: GameCountdownProps) {
  const [now, setNow] = useState(() => Date.now())
  const target = new Date(to).getTime()
  const remaining = Math.max(0, target - now)

  useEffect(() => {
    if (remaining <= 0) return
    const interval = remaining < 3600_000 ? 1000 : 60_000
    const id = setInterval(() => setNow(Date.now()), interval)
    return () => clearInterval(id)
  }, [remaining])

  return (
    <span
      className={cn('mono text-[30px] font-bold tracking-tight', className)}
      style={{
        color: 'var(--color-prestige)',
        textShadow: '0 0 12px rgba(245,158,11,0.3)',
      }}
    >
      {format(remaining)}
    </span>
  )
}
```

- [ ] **Step 4: Run tests — expect 3 pass**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx vitest run test/components/games/GameCountdown.test.tsx
```

- [ ] **Step 5: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/components/games/GameCountdown.tsx src/ReactClient/test/components/games/GameCountdown.test.tsx
git commit -m "feat(ui): GameCountdown — large mono prestige-colored countdown"
```

## Task 6: `GamesTable` component

**Files:**
- Create: `src/ReactClient/src/components/games/GamesTable.tsx`

No test for this component — it's a thin row renderer over existing data and is exercised by the `Games.tsx` page which is manually verified.

- [ ] **Step 1: Implement**

Create `src/ReactClient/src/components/games/GamesTable.tsx`:

```tsx
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import type { GameSummaryViewModel } from '@/api/types'
import { GameCountdown } from './GameCountdown'
import { cn } from '@/lib/utils'

type Section = 'active' | 'upcoming' | 'finished'

interface GamesTableProps {
  section: Section
  games: GameSummaryViewModel[]
}

const BADGE_CLASSES: Record<Section, string> = {
  active: 'text-resource border-l-2 border-[color:var(--color-resource)] bg-[color:var(--color-resource)]/15',
  upcoming: 'text-prestige border-l-2 border-[color:var(--color-prestige)] bg-[color:var(--color-prestige)]/15',
  finished: 'text-muted-foreground border-l-2 border-muted-foreground bg-muted/10',
}

const BADGE_LABEL: Record<Section, string> = {
  active: 'Live',
  upcoming: 'Upcoming',
  finished: 'Finished',
}

function StatusBadge({ section }: { section: Section }) {
  return (
    <span className={cn('inline-block text-[9.5px] uppercase tracking-[0.12em] px-1.5 py-0.5 font-bold', BADGE_CLASSES[section])}>
      {BADGE_LABEL[section]}
    </span>
  )
}

function NameCell({ game, section }: { game: GameSummaryViewModel; section: Section }) {
  const subline: ReactNode = section === 'active' && game.isPlayerEnrolled
    ? <>{game.gameDefType.toUpperCase()} · <span className="text-resource">you are enrolled</span></>
    : section === 'upcoming' && game.isPlayerEnrolled
    ? <>{game.gameDefType.toUpperCase()} · <span className="text-resource">you are registered</span></>
    : game.gameDefType.toUpperCase()
  return (
    <div>
      <div className="font-semibold text-foreground">{game.name}</div>
      <div className="text-[10.5px] text-muted-foreground font-normal mt-0.5">{subline}</div>
    </div>
  )
}

function Actions({ game, section }: { game: GameSummaryViewModel; section: Section }) {
  if (section === 'active') {
    return (
      <>
        {game.isPlayerEnrolled ? (
          <Link to={`/games/${game.gameId}/base`} className="text-[11.5px] font-semibold text-resource hover:underline ml-3">Enter →</Link>
        ) : game.canJoin ? (
          <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-prestige hover:underline ml-3">Join</Link>
        ) : null}
        <Link to={`/games/${game.gameId}/live`} className="text-[11.5px] text-interactive hover:underline ml-3">Spectate</Link>
      </>
    )
  }
  if (section === 'upcoming') {
    if (game.isPlayerEnrolled) {
      return <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-resource hover:underline ml-3">Briefing →</Link>
    }
    return (
      <>
        {game.canJoin && <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-prestige hover:underline ml-3">Register</Link>}
        <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] text-interactive hover:underline ml-3">Details</Link>
      </>
    )
  }
  // finished
  return (
    <>
      <Link to={`/games/${game.gameId}/results`} className="text-[11.5px] text-interactive hover:underline ml-3">Results</Link>
      <Link to={`/replays/${game.gameId}`} className="text-[11.5px] text-interactive hover:underline ml-3">Replay</Link>
    </>
  )
}

function fmtDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function GamesTable({ section, games }: GamesTableProps) {
  if (games.length === 0) return null
  return (
    <table className="w-full text-[12.5px]">
      <thead>
        <tr className="text-left">
          <th className="label py-1.5 pr-3 w-[88px]">Status</th>
          <th className="label py-1.5 pr-3">Game</th>
          {section === 'active' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[100px]">Players</th>
              <th className="label py-1.5 pr-3 text-right w-[120px]">Round</th>
              <th className="label py-1.5 pr-3 text-right w-[140px]">Ends</th>
            </>
          )}
          {section === 'upcoming' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[100px]">Registered</th>
              <th className="label py-1.5 pr-3 text-right w-[140px]">Starts in</th>
            </>
          )}
          {section === 'finished' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[80px]">Players</th>
              <th className="label py-1.5 pr-3">Winner</th>
              <th className="label py-1.5 pr-3 text-right w-[120px]">Ended</th>
            </>
          )}
          <th className="py-1.5 text-right min-w-[200px]" />
        </tr>
      </thead>
      <tbody>
        {games.map((g) => {
          const enrolled = g.isPlayerEnrolled
          const rowClass = enrolled
            ? 'border-b border-border bg-[color:var(--color-resource)]/4 border-l-2 border-l-[color:var(--color-resource)]'
            : 'border-b border-border hover:bg-white/2'
          return (
            <tr key={g.gameId} className={cn(rowClass, section === 'finished' && 'opacity-75 hover:opacity-100')}>
              <td className="py-2 pr-3"><StatusBadge section={section} /></td>
              <td className="py-2 pr-3"><NameCell game={g} section={section} /></td>

              {section === 'active' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}{g.maxPlayers > 0 ? ` / ${g.maxPlayers}` : ''}</td>
                  <td className="py-2 pr-3 mono text-right text-muted-foreground">—</td>
                  <td className="py-2 pr-3 mono text-right">
                    {g.endTime ? <GameCountdown to={g.endTime} className="text-[12.5px] font-semibold" /> : '—'}
                  </td>
                </>
              )}

              {section === 'upcoming' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}{g.maxPlayers > 0 ? ` / ${g.maxPlayers}` : ''}</td>
                  <td className="py-2 pr-3 mono text-right">
                    {g.startTime ? <GameCountdown to={g.startTime} className="text-[12.5px] font-semibold" /> : '—'}
                  </td>
                </>
              )}

              {section === 'finished' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}</td>
                  <td className="py-2 pr-3">
                    {g.winnerName
                      ? <>◆ <span className="font-semibold">{g.winnerName}</span></>
                      : <span className="text-muted-foreground">—</span>}
                  </td>
                  <td className="py-2 pr-3 mono text-right text-muted-foreground">{fmtDate(g.endTime)}</td>
                </>
              )}

              <td className="py-2 text-right whitespace-nowrap"><Actions game={g} section={section} /></td>
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}
```

Note on winner-name race coloring: the spec mocks showed race-colored winner names. `GameSummaryViewModel` doesn't currently carry `winnerRace`, so this task ships the winner name uncolored. Adding `WinnerRace` (backend) + race color mapping (this component) is a follow-up; the design survives without it.

- [ ] **Step 2: Typecheck**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit
```

- [ ] **Step 3: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/components/games/GamesTable.tsx
git commit -m "feat(ui): GamesTable — three-section aware row renderer for /games"
```

## Task 7: Rewrite `Games.tsx` using `GamesTable`

**Files:**
- Modify: `src/ReactClient/src/pages/Games.tsx` (full rewrite)

- [ ] **Step 1: Replace the file**

Overwrite `src/ReactClient/src/pages/Games.tsx` with:

```tsx
import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel } from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonLine } from '@/components/Skeleton'
import { GamesTable } from '@/components/games/GamesTable'
import { useSignalR } from '@/hooks/useSignalR'
import { useEffect, useCallback } from 'react'

function SectionHeader({ label, meta }: { label: string; meta?: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between border-b border-border mb-2 pt-6 pb-1.5">
      <h2 className="label">{label}</h2>
      {meta && <div className="text-[10.5px] text-muted-foreground mono">{meta}</div>}
    </div>
  )
}

export function Games() {
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const isNewPlayer = searchParams.get('welcome') === '1'
  const [showAllFinished, setShowAllFinished] = useState(false)
  const { on, off } = useSignalR('/gamehub')

  const { data, isLoading, error, refetch } = useQuery<GameListViewModel>({
    queryKey: ['games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['games'] })
  }, [queryClient])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  const games = data?.games ?? []
  const active = games.filter((g) => g.status === 'Active')
  const upcoming = games.filter((g) => g.status === 'Upcoming')
  const finished = games.filter((g) => g.status === 'Finished')
  const visibleFinished = showAllFinished || finished.length <= 5 ? finished : finished.slice(0, 5)

  return (
    <div className="max-w-5xl">
      <h1 className="text-[22px] font-bold tracking-tight">Games</h1>
      <p className="text-[11.5px] text-muted-foreground mt-1 mb-4">
        Season schedule · live, upcoming, and finished seasons · {games.length} total
      </p>

      {isNewPlayer && (
        <div className="border-l-2 border-[color:var(--color-resource)] bg-[color:var(--color-resource)]/5 px-3 py-2 text-sm mb-4">
          <strong className="text-resource">Welcome to BGE.</strong>{' '}
          <span className="text-muted-foreground">Your commander is ready. Join an active game below, or register for an upcoming season.</span>
        </div>
      )}

      {isLoading && (
        <div className="space-y-3 mt-4" aria-hidden="true">
          <SkeletonLine className="h-5 w-24" />
          <SkeletonLine className="h-4 w-full" />
          <SkeletonLine className="h-4 w-full" />
          <SkeletonLine className="h-4 w-full" />
        </div>
      )}
      {error && !data && <ApiError message="Failed to load games." onRetry={() => void refetch()} />}

      {data && (
        <>
          <SectionHeader label="Active — in progress" meta={`${active.length} game${active.length === 1 ? '' : 's'}`} />
          {active.length === 0 ? (
            <p className="text-[11.5px] text-muted-foreground py-2">
              No games are currently running. Check upcoming seasons below or come back later.
            </p>
          ) : (
            <GamesTable section="active" games={active} />
          )}

          <SectionHeader label="Upcoming — pre-season" meta={`${upcoming.length} game${upcoming.length === 1 ? '' : 's'}`} />
          {upcoming.length === 0 ? (
            <p className="text-[11.5px] text-muted-foreground py-2">
              No upcoming games scheduled yet. Admins post new seasons here.
            </p>
          ) : (
            <GamesTable section="upcoming" games={upcoming} />
          )}

          {finished.length > 0 && (
            <>
              <SectionHeader
                label="Finished"
                meta={
                  <>
                    {visibleFinished.length} shown
                    {finished.length > 5 && (
                      <>
                        {' · '}
                        <button
                          className="text-interactive hover:underline"
                          onClick={() => setShowAllFinished((v) => !v)}
                        >
                          {showAllFinished ? 'collapse' : `show all (${finished.length})`}
                        </button>
                      </>
                    )}
                  </>
                }
              />
              <GamesTable section="finished" games={visibleFinished} />
            </>
          )}
        </>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Typecheck and visually confirm**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit
```

Start the dev server briefly and visit `/games`; confirm three sections render with the table layout. (If you're offline or the backend isn't running, skip the visual check — the build step below catches most regressions.)

- [ ] **Step 3: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/pages/Games.tsx
git commit -m "feat(ui): rewrite /games as three-section table (Active / Upcoming / Finished)"
```

## Task 8: Rename + rewrite `GameLobbyDetail.tsx` → `GameBriefing.tsx`

**Files:**
- Create: `src/ReactClient/src/pages/GameBriefing.tsx`
- Delete: `src/ReactClient/src/pages/GameLobbyDetail.tsx`

- [ ] **Step 1: Create the new page**

Create `src/ReactClient/src/pages/GameBriefing.tsx`:

```tsx
import { useEffect, useCallback, useMemo, useState } from 'react'
import { useParams, Link, useNavigate, Navigate } from 'react-router'
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameLobbyViewModel, JoinGameRequest, RaceListViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useSignalR } from '@/hooks/useSignalR'
import { GameCountdown } from '@/components/games/GameCountdown'
import { RaceTile } from '@/components/games/RaceTile'
import { normalizeRace, RACES, type Race } from '@/lib/race'
import { cn } from '@/lib/utils'

function StatusBadge({ status }: { status: string }) {
  const s = status.toLowerCase()
  const cls = s === 'active'
    ? 'text-resource border-[color:var(--color-resource)] bg-[color:var(--color-resource)]/15'
    : s === 'upcoming'
    ? 'text-prestige border-[color:var(--color-prestige)] bg-[color:var(--color-prestige)]/15'
    : 'text-muted-foreground border-muted-foreground bg-muted/10'
  const label = s === 'active' ? 'Live' : s === 'upcoming' ? 'Upcoming' : 'Finished'
  return (
    <span className={cn('inline-block text-[9.5px] uppercase tracking-[0.12em] px-1.5 py-0.5 border-l-2 font-bold ml-3 align-middle', cls)}>
      {label}
    </span>
  )
}

function SectionHeader({ label }: { label: string }) {
  return <h2 className="label mb-2.5 mt-5 pb-1.5 border-b border-border">{label}</h2>
}

function Stat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className="label text-[9.5px]">{label}</div>
      <div className="mono text-[16px] font-bold mt-0.5">{value}</div>
    </div>
  )
}

function PlayerList({ players, currentPlayerId }: { players: GameLobbyViewModel['players']; currentPlayerId: string | null }) {
  const raceClass: Record<string, string> = {
    terran: 'text-[#CBD5E1]',
    zerg: 'text-[#B45309]',
    protoss: 'text-[#FBBF24]',
  }
  if (players.length === 0) return <p className="text-[11.5px] text-muted-foreground">No commanders have joined yet.</p>
  return (
    <div className="mono text-[11.5px] leading-relaxed">
      {players.map((p) => {
        const race = normalizeRace(p.playerType)
        const isMe = p.playerId === currentPlayerId
        return (
          <div key={p.playerId}>
            <span className={cn(race && raceClass[race], isMe && 'font-bold text-resource')}>{p.playerName}</span>
            <span className="text-muted-foreground text-[10.5px] ml-2.5">
              {new Date(p.joined).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} · {p.playerType}
              {isMe ? ' · you' : ''}
            </span>
          </div>
        )
      })}
    </div>
  )
}

interface GameBriefingProps {}

export function GameBriefing(_props: GameBriefingProps) {
  const { gameId } = useParams<{ gameId: string }>()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { on, off } = useSignalR('/gamehub')

  const { data: lobby, isLoading, error, refetch } = useQuery<GameLobbyViewModel>({
    queryKey: ['game-lobby', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/lobby`).then((r) => r.data),
    enabled: !!gameId,
    refetchInterval: 15_000,
  })

  const { data: racesData } = useQuery<RaceListViewModel>({
    queryKey: ['races'],
    queryFn: () => apiClient.get('/api/games/races').then((r) => r.data),
    staleTime: Infinity,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['game-lobby', gameId] })
  }, [queryClient, gameId])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  const [selectedRace, setSelectedRace] = useState<Race>('terran')
  const [playerName, setPlayerName] = useState('')
  const [joinError, setJoinError] = useState<string | null>(null)

  const joinMutation = useMutation({
    mutationFn: (req: JoinGameRequest) => apiClient.post(`/api/games/${gameId}/join`, req),
    onSuccess: () => {
      setJoinError(null)
      void queryClient.invalidateQueries({ queryKey: ['game-lobby', gameId] })
      if (lobby?.status.toLowerCase() === 'active') {
        void navigate(`/games/${gameId}/base`)
      }
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to join.'
      setJoinError(String(msg))
    },
  })

  const racesForUi = useMemo(
    () => (racesData?.races.map((r) => normalizeRace(r.id)).filter((r): r is Race => !!r) ?? RACES),
    [racesData],
  )

  if (isLoading) return <PageLoader message="Loading briefing..." />
  if (error) return <ApiError message="Failed to load game." onRetry={() => void refetch()} />
  if (!lobby || !gameId) return <ApiError message="Game not found." />

  // Finished games go straight to results
  if (lobby.status.toLowerCase() === 'finished') {
    return <Navigate to={`/games/${gameId}/results`} replace />
  }

  const status = lobby.status.toLowerCase() as 'active' | 'upcoming'
  const isEnrolled = lobby.isCurrentPlayerEnrolled

  const enrollmentLine = isEnrolled ? (
    <>
      You are {status === 'active' ? 'enrolled' : 'registered'} ·{' '}
      <span style={{ color: `var(--color-race-primary)` }}>{lobby.currentPlayerRace ?? '—'}</span>
      {lobby.currentPlayerName && <> · {lobby.currentPlayerName}</>}
      {status === 'active' && lobby.currentPlayerRank != null && <> · Round rank #{lobby.currentPlayerRank}</>}
    </>
  ) : null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!playerName.trim()) {
      setJoinError('Please enter a commander name.')
      return
    }
    setJoinError(null)
    joinMutation.mutate({ playerName: playerName.trim(), playerType: selectedRace })
  }

  return (
    <div className="max-w-4xl">
      <Link to="/games" className="text-[11.5px] text-interactive hover:underline">← All Games</Link>

      <h1 className="text-[22px] font-bold tracking-tight mt-2">
        {lobby.gameName}<StatusBadge status={lobby.status} />
      </h1>
      {enrollmentLine && (
        <p className="text-[11.5px] text-muted-foreground mt-1">{enrollmentLine}</p>
      )}

      {status === 'upcoming' && lobby.startTime && (
        <>
          <SectionHeader label="Starts in" />
          <div>
            <GameCountdown to={lobby.startTime} />
            <span className="text-[11.5px] text-muted-foreground ml-2">
              {new Date(lobby.startTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
        </>
      )}

      {status === 'active' && lobby.endTime && (
        <>
          <SectionHeader label="Ends in" />
          <div>
            <GameCountdown to={lobby.endTime} />
            <span className="text-[11.5px] text-muted-foreground ml-2">
              {new Date(lobby.endTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
        </>
      )}

      {lobby.settings && (
        <>
          <SectionHeader label="Rules" />
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-5">
            <Stat label="Starting resources" value={`${lobby.settings.startingMinerals.toLocaleString()}m / ${lobby.settings.startingGas.toLocaleString()}g`} />
            <Stat label="Starting land" value={lobby.settings.startingLand} />
            <Stat label="End tick" value={lobby.settings.endTick.toLocaleString()} />
            <Stat label="Protection" value={`${lobby.settings.protectionTicks} ticks`} />
          </div>
        </>
      )}

      {status === 'active' && isEnrolled && (
        <div className="flex gap-3 mt-6">
          <Link
            to={`/games/${gameId}/base`}
            className="border border-[color:var(--color-resource)] text-resource px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-resource)]/10"
          >
            Enter game →
          </Link>
          <Link
            to={`/games/${gameId}/live`}
            className="border border-interactive text-interactive px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-interactive)]/10"
          >
            Live scoreboard
          </Link>
        </div>
      )}

      {status === 'active' && !isEnrolled && lobby.canJoin && (
        <>
          <SectionHeader label="Join this game" />
          <RegisterForm
            races={racesForUi}
            selectedRace={selectedRace}
            onRaceChange={setSelectedRace}
            playerName={playerName}
            onNameChange={setPlayerName}
            submitLabel={`Join as ${selectedRace}`}
            onSubmit={handleSubmit}
            busy={joinMutation.isPending}
            error={joinError}
          />
          <div className="mt-4">
            <Link
              to={`/games/${gameId}/live`}
              className="border border-interactive text-interactive px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-interactive)]/10"
            >
              Or spectate live →
            </Link>
          </div>
        </>
      )}

      {status === 'upcoming' && !isEnrolled && lobby.canJoin && (
        <>
          <SectionHeader label="Register — choose your race" />
          <RegisterForm
            races={racesForUi}
            selectedRace={selectedRace}
            onRaceChange={setSelectedRace}
            playerName={playerName}
            onNameChange={setPlayerName}
            submitLabel={`Register as ${selectedRace}`}
            onSubmit={handleSubmit}
            busy={joinMutation.isPending}
            error={joinError}
          />
        </>
      )}

      {status === 'upcoming' && isEnrolled && (
        <p className="text-[12.5px] text-muted-foreground mt-5">
          You're in. The game hasn't started — you'll be able to enter when the clock hits 0.
        </p>
      )}

      <SectionHeader label={`${status === 'upcoming' ? 'Registered commanders' : 'Commanders'} — ${lobby.players.length}${lobby.maxPlayers > 0 ? ` / ${lobby.maxPlayers}` : ''}`} />
      <PlayerList players={lobby.players} currentPlayerId={lobby.currentPlayerId} />
    </div>
  )
}

function RegisterForm({
  races, selectedRace, onRaceChange, playerName, onNameChange, submitLabel, onSubmit, busy, error,
}: {
  races: Race[]
  selectedRace: Race
  onRaceChange: (r: Race) => void
  playerName: string
  onNameChange: (v: string) => void
  submitLabel: string
  onSubmit: (e: React.FormEvent) => void
  busy: boolean
  error: string | null
}) {
  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="flex gap-3">
        {races.map((r) => (
          <RaceTile key={r} race={r} selected={selectedRace === r} onSelect={onRaceChange} />
        ))}
      </div>
      {error && (
        <div role="alert" className="border-l-2 border-[color:var(--color-danger)] bg-[color:var(--color-danger)]/10 px-3 py-2 text-[12px] text-[color:var(--color-danger)]">
          {error}
        </div>
      )}
      <div className="flex gap-2">
        <input
          type="text"
          value={playerName}
          onChange={(e) => onNameChange(e.target.value)}
          placeholder="Commander name"
          maxLength={32}
          className="flex-1 bg-input border border-border px-3 py-2 text-[13px] focus:outline-none focus:border-[color:var(--color-prestige)]"
        />
        <button
          type="submit"
          disabled={busy}
          className="border border-[color:var(--color-prestige)] text-prestige px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-prestige)]/10 disabled:opacity-50"
        >
          {busy ? 'Submitting…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
```

- [ ] **Step 2: Delete the old file**

```bash
cd /tmp/bge-work/sco-flair-redesign
git rm src/ReactClient/src/pages/GameLobbyDetail.tsx
```

- [ ] **Step 3: Typecheck**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit
```

Expected: passes. (App.tsx still references `GameLobbyDetail` — that's removed in Task 10.)

If typecheck fails now because App.tsx can't find `GameLobbyDetail`, proceed anyway — it'll be resolved in Task 10. Alternatively, combine Task 8 + Task 10 in one commit at the end; see Task 10.

- [ ] **Step 4: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/pages/GameBriefing.tsx
git commit -m "feat(ui): GameBriefing — unified pre-game page with four render states

Replaces GameLobbyDetail + absorbs JoinGame. Renders:
- Upcoming + not registered: rules, race picker, registered list
- Upcoming + registered: countdown + confirmation
- Active + not enrolled: CTAs + join form + commander list
- Active + enrolled: CTAs + rules with rank
Finished games redirect to /results."
```

Note: this commit leaves App.tsx broken-referenced until Task 10. If you want a clean bisect, squash Tasks 8–10 into a single commit at the end of Task 10.

## Task 9: Delete `GameLobby.tsx`, `JoinGame.tsx`, `Spectator.tsx`

**Files:**
- Delete: `src/ReactClient/src/pages/GameLobby.tsx`
- Delete: `src/ReactClient/src/pages/JoinGame.tsx`
- Delete: `src/ReactClient/src/pages/Spectator.tsx`

- [ ] **Step 1: Remove the files**

```bash
cd /tmp/bge-work/sco-flair-redesign
git rm src/ReactClient/src/pages/GameLobby.tsx src/ReactClient/src/pages/JoinGame.tsx src/ReactClient/src/pages/Spectator.tsx
```

- [ ] **Step 2: Do NOT typecheck yet**

App.tsx still references these; the routing update in Task 10 resolves that. This task is just the file deletion.

- [ ] **Step 3: Commit (deferred)**

Don't commit standalone — bundle with Task 10. Proceed to Task 10 without committing.

## Task 10: Update `App.tsx` — remove deleted routes, add briefing route

**Files:**
- Modify: `src/ReactClient/src/App.tsx`

- [ ] **Step 1: Edit the lazy imports**

In `App.tsx`, replace the current lazy imports for `GameLobby`, `GameLobbyDetail`, `JoinGame`, `Spectator` with a single one for `GameBriefing`:

Remove these lines (around lines 37–39, 65):

```tsx
const GameLobby = lazy(() => import('@/pages/GameLobby').then(m => ({ default: m.GameLobby })))
const GameLobbyDetail = lazy(() => import('@/pages/GameLobbyDetail').then(m => ({ default: m.GameLobbyDetail })))
const JoinGame = lazy(() => import('@/pages/JoinGame').then(m => ({ default: m.JoinGame })))
const Spectator = lazy(() => import('@/pages/Spectator').then(m => ({ default: m.Spectator })))
```

Add (place alongside other lazy imports):

```tsx
const GameBriefing = lazy(() => import('@/pages/GameBriefing').then(m => ({ default: m.GameBriefing })))
```

- [ ] **Step 2: Remove the `SpectatorPage` wrapper**

Delete the `SpectatorPage` function (around lines 151–155):

```tsx
function SpectatorPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Spectator gameId={gameId} />
}
```

- [ ] **Step 3: Edit the route table**

Inside the `/games/:gameId` nested route children, **remove** the `join` route:

```tsx
{ path: 'join', element: <RouteWithBoundary><JoinGame /></RouteWithBoundary> },
```

Add the new briefing route as a sibling of `base`, `units`, etc.:

```tsx
{ path: 'briefing', element: <RouteWithBoundary><GameBriefing /></RouteWithBoundary> },
```

At the top level (not under `/games/:gameId`), **remove**:

```tsx
{ path: '/lobby', element: <RouteWithBoundary><GameLobby /></RouteWithBoundary> },
{ path: '/lobby/:gameId', element: <RouteWithBoundary><GameLobbyDetail /></RouteWithBoundary> },
{ path: '/games/:gameId/spectate', element: <RouteWithBoundary><SpectatorPage /></RouteWithBoundary> },
```

- [ ] **Step 4: Typecheck and build**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit && npm run build
```

Expected: both succeed.

- [ ] **Step 5: Commit (bundles Tasks 8–10)**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/App.tsx
git commit -m "refactor(routes): route /games/:id/briefing; drop /lobby, /spectate, /join

Removes 3 dead pages (GameLobby, JoinGame, Spectator), renames
GameLobbyDetail → GameBriefing, and wires /games/:id/briefing as the
single pre-game landing page."
```

Note: Task 8 already committed `GameBriefing.tsx`; Task 9 staged the deletions; Task 10 wires the router. The final state is equivalent to a single logical commit — the intermediate ones don't typecheck, which is fine for feature-branch history.

## Task 11: `GameLayout` — restore "← All Games" link

**Files:**
- Modify: `src/ReactClient/src/layouts/GameLayout.tsx` — the `SidebarContent` component

- [ ] **Step 1: Import `NavLink` and `ArrowLeftIcon`**

At the top of `GameLayout.tsx`, add to the `lucide-react` import block (already exists): include `ArrowLeftIcon`. And make sure `NavLink` is imported from `react-router` (the file already imports `Outlet, useParams, Navigate, useLocation, useNavigate` — add `NavLink`).

Resulting imports (showing only the relevant deltas):

```tsx
import { Outlet, useParams, Navigate, useLocation, useNavigate, NavLink } from 'react-router'
import {
  LogOutIcon, MenuIcon, XIcon, UserIcon, HistoryIcon, LineChartIcon, Trophy, ArrowLeftIcon,
} from 'lucide-react'
```

- [ ] **Step 2: Add the back-link inside `SidebarContent`**

Find `SidebarContent` and insert a `NavLink` between the race-identity `<div>` and `<NavMenu />`:

```tsx
function SidebarContent({ race }: { race: Race | null }) {
  const { currentGame } = useCurrentGame()
  const raceLabel = race ? race[0].toUpperCase() + race.slice(1) : ''
  return (
    <>
      <RaceStripe />
      <div className="px-4 py-3 border-b border-border flex items-center gap-3">
        <RaceEmblem race={race} size={28} />
        <div className="min-w-0">
          <div className="label" style={{ color: 'var(--color-race-primary)' }}>{raceLabel || 'Commander'}</div>
          <div className="text-[11px] text-muted-foreground truncate">{currentGame?.name ?? 'Game'}</div>
        </div>
      </div>
      <NavLink
        to="/games"
        className="flex items-center gap-1.5 px-4 py-2 text-[11px] text-interactive hover:text-[color:var(--color-race-secondary)] border-b border-border"
      >
        <ArrowLeftIcon className="h-3 w-3" aria-hidden /> All Games
      </NavLink>
      <NavMenu />
    </>
  )
}
```

- [ ] **Step 3: Typecheck and build**

```bash
cd /tmp/bge-work/sco-flair-redesign/src/ReactClient
npx tsc --noEmit && npm run build
```

- [ ] **Step 4: Commit**

```bash
cd /tmp/bge-work/sco-flair-redesign
git add src/ReactClient/src/layouts/GameLayout.tsx
git commit -m "feat(ui): GameLayout — restore '← All Games' link in sidebar"
```

## Task 12: Final full-repo health check

- [ ] **Step 1: Build everything**

```bash
cd /tmp/bge-work/sco-flair-redesign
cd src && dotnet build --configuration Release && cd ..
cd src/ReactClient && npm run build && cd ../..
```

- [ ] **Step 2: Run tests**

```bash
cd /tmp/bge-work/sco-flair-redesign
cd src && dotnet test && cd ..
cd src/ReactClient && npx tsc --noEmit && npx vitest run && cd ../..
```

Expected: build green, all tests pass.

- [ ] **Step 3: Scan for stale references**

```bash
cd /tmp/bge-work/sco-flair-redesign
grep -rn "GameLobby\.tsx\|JoinGame\.tsx\|Spectator\.tsx\|/lobby'\|\"/lobby\"" src/ReactClient/src src/ReactClient/e2e 2>&1 | head -30 || true
```

Any hits indicate stale references — update them. The `e2e/` suite is the most likely place to find leftover selectors or URL strings.

- [ ] **Step 4: Push to PR #319**

```bash
cd /tmp/bge-work/sco-flair-redesign
git push
```

No new PR — commits land on the existing `feat/sco-flair-redesign` branch already tracked by PR #319.

---

## Done criteria

- [ ] `grep -rn "GameLobby\b\|JoinGame\|Spectator" src/ReactClient/src/pages src/ReactClient/src/App.tsx` → 0 hits
- [ ] `/lobby` in a browser → 404 (no fallback redirect needed; internal tool)
- [ ] `/games/:id/join` → 404 (same)
- [ ] `/games/:id/briefing` renders correctly for anon, upcoming-not-registered, upcoming-registered, active-not-enrolled, active-enrolled, and redirects to `/results` for finished
- [ ] `/games` table displays three sections with state-specific columns; enrolled rows get the green left-border tint
- [ ] Sidebar of any in-game page shows "← All Games" link right above the nav list
- [ ] `dotnet build`, `dotnet test`, `npm run build`, `npx vitest run`, `npx tsc --noEmit` all green
