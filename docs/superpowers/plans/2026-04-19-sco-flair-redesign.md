# SCO Flair Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the BGE React client to feel like the 2003 StarCraft Online — dark-space atmosphere, per-race chrome, dense information design, SVG silhouette icons — while keeping the modern SPA foundation (shadcn, Tailwind, Radix, SignalR) intact.

**Architecture:** Layered incremental refactor in five phases. Phase 1 lays token/primitive/shell foundation. Phase 2 adds the SVG icon kit. Phase 3 rewrites page bodies. Phase 4 polishes login + join screens. Phase 5 removes retired code. Each phase is independently mergeable — after each phase the app still builds, tests pass, and the UX is coherent (not half-redesigned).

**Tech Stack:** React 19 + TypeScript, Vite 6, Tailwind CSS v4 (`@theme` blocks), Radix primitives, Lucide icons (being scoped down), Vitest for unit, Playwright for E2E. Backend is ASP.NET Core — Phase 1 touches it once to expose player race on the profile endpoint.

**Reference:** [`docs/superpowers/specs/2026-04-19-sco-flair-redesign-design.md`](../specs/2026-04-19-sco-flair-redesign-design.md) (approved spec).

**Branch strategy:** One branch + PR per phase (as per `CLAUDE.md` workflow). Start from `master`. Example: `git worktree add /tmp/bge-work/sco-flair-phase1 -b feat/sco-flair-phase1-foundation master`.

---

## File structure (what this plan creates or modifies)

### New files

- `src/ReactClient/src/components/icons/` — new directory; all SVG-in-TSX icon components
  - `nav/` — 12 nav icons
  - `units/` — unit silhouettes per race
  - `buildings/` — building silhouettes per race
  - `actions/` — attack, defend, recall, split, merge
  - `resources/` — minerals, gas, land (and large variants)
  - `emblems/` — race emblems (Terran, Zerg, Protoss)
  - `index.ts` — re-exports
- `src/ReactClient/src/components/ui/data-grid.tsx` — new primitive: `<DataGrid cols={N}>` with gap-1px border-bg styling
- `src/ReactClient/src/components/ui/timer-ring.tsx` — new primitive: circular SVG countdown
- `src/ReactClient/src/components/ui/starfield.tsx` — new primitive: reusable starfield backdrop wrapper
- `src/ReactClient/src/hooks/usePlayerRace.ts` — hook that reads race from `/api/playerprofile` and applies `data-race` to `<html>`
- `src/ReactClient/src/lib/race.ts` — race constants and helpers
- `src/ReactClient/test/components/icons.test.tsx` — smoke tests for icon renders
- `src/ReactClient/test/components/data-grid.test.tsx` — Section/DataGrid tests
- `src/ReactClient/test/hooks/usePlayerRace.test.tsx` — race hook test

### Modified files

- `src/ReactClient/src/index.css` — replace `@theme` values; drop `.light`, `.bg-grid`; add race palettes, starfield utility
- `src/ReactClient/src/components/ui/section.tsx` — default `boxed={false}` and add label-only rendering
- `src/ReactClient/src/layouts/GameLayout.tsx` — major rewrite: new sidebar, new header, drop theme toggle
- `src/ReactClient/src/components/NavMenu.tsx` — 6 primary + utility footer
- `src/ReactClient/src/components/PlayerResources.tsx` — green mono + glow styling
- `src/ReactClient/src/pages/Base.tsx` — refactor to Section/DataGrid, drop card wrappers
- `src/ReactClient/src/pages/Units.tsx` — DataGrid home list + deployed block
- `src/ReactClient/src/pages/PlayerRanking.tsx` — tier typography, race-colored names, merge Alliance Ranking as tab
- `src/ReactClient/src/pages/AllianceRanking.tsx` — consumed by Ranking as a tab (keep logic, export content component)
- `src/ReactClient/src/pages/Research.tsx` — track restyle
- `src/ReactClient/src/pages/Market.tsx` — add Direct tab (merges Trade)
- `src/ReactClient/src/pages/Trade.tsx` — wrap content in an exported component so Market can embed it
- `src/ReactClient/src/pages/Chat.tsx` — race-colored name rendering
- `src/ReactClient/src/pages/SignIn.tsx` — CSS splash background
- `src/ReactClient/src/pages/JoinGame.tsx` — race emblem tiles
- `src/ReactClient/src/App.tsx` — drop ThemeProvider wrapping
- `src/ReactClient/src/main.tsx` — remove any theme-related boot
- `src/ReactClient/src/contexts/ThemeContext.tsx` — DELETE (Phase 5 confirms no consumers left)
- `src/BrowserGameEngine.Shared/PlayerProfileViewModel.cs` — add `PlayerType` property
- `src/BrowserGameEngine.FrontendServer/Controllers/PlayerProfileController.cs` — populate `PlayerType` in `Get()`
- `src/ReactClient/src/api/types.ts` — mirror the added `playerType` on `PlayerProfileViewModel`

### E2E updates

- `src/ReactClient/e2e/**` — selectors that depend on `bg-grid`, theme toggle, or specific nav item text (Trade, Alliance Ranking) need updating alongside Phase 1 and Phase 3 changes.

---

## Phase 1 — Foundation (tokens + shell)

**Outcome:** Fresh branch, merged PR. After merge, the app builds and renders correctly: new color tokens active, starfield visible, race data flows from backend, sidebar has 6 primary + utility footer, header has 4 regions (no theme toggle, no connection pill), circular timer visible. Page bodies still look like the old design (Phase 3's job).

### Task 1: Create worktree and branch

**Files:** none — git only.

- [ ] **Step 1: Create the worktree off master**

```bash
cd /home/chris/repos/my/bge
git worktree add /tmp/bge-work/sco-flair-phase1 -b feat/sco-flair-phase1-foundation master
cd /tmp/bge-work/sco-flair-phase1
```

- [ ] **Step 2: Verify clean baseline**

```bash
cd src && dotnet build --configuration Release && cd ..
cd src/ReactClient && npm ci && npm run build && cd ../..
```

Expected: both builds succeed. If not, fix before proceeding — baseline must be green.

### Task 2: Backend — expose PlayerType on profile

**Files:**
- Modify: `src/BrowserGameEngine.Shared/PlayerProfileViewModel.cs`
- Modify: `src/BrowserGameEngine.FrontendServer/Controllers/PlayerProfileController.cs:45-61`

- [ ] **Step 1: Add `PlayerType` property to the DTO**

Modify `src/BrowserGameEngine.Shared/PlayerProfileViewModel.cs`:

```csharp
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerProfileViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public string? PlayerType { get; set; }
		public decimal Land { get; set; }
		public int ProtectionTicksRemaining { get; set; }
		public bool IsOnline { get; set; }
		public DateTime? LastOnline { get; set; }
		public bool TutorialCompleted { get; set; }
	}
}
```

- [ ] **Step 2: Populate `PlayerType` in the controller**

In `PlayerProfileController.cs` inside `Get()`, after `var player = playerRepository.Get(currentUserContext.PlayerId!);` add:

```csharp
			return new PlayerProfileViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				PlayerType = player.PlayerType.Id,
				Land = resourceRepository.GetLand(player.PlayerId),
				ProtectionTicksRemaining = player.State.ProtectionTicksRemaining,
				IsOnline = onlineStatusRepository.IsOnline(player.PlayerId),
				LastOnline = player.LastOnline,
				TutorialCompleted = player.State.TutorialCompleted
			};
```

- [ ] **Step 3: Build and run backend tests**

```bash
cd src
dotnet build --configuration Release
dotnet test --filter "FullyQualifiedName~PlayerProfile"
```

Expected: build succeeds. Tests (if any exist for this controller) pass.

- [ ] **Step 4: Update the frontend type**

Modify `src/ReactClient/src/api/types.ts` — find `PlayerProfileViewModel` (around line 410) and add `playerType`:

```typescript
export interface PlayerProfileViewModel {
  playerId: string | null
  playerName: string | null
  playerType: string | null
  land: number
  protectionTicksRemaining: number
  isOnline: boolean
  lastOnline: string | null
  tutorialCompleted: boolean
}
```

- [ ] **Step 5: Typecheck & commit**

```bash
cd src/ReactClient && npx tsc --noEmit && cd ../..
git add src/BrowserGameEngine.Shared/PlayerProfileViewModel.cs src/BrowserGameEngine.FrontendServer/Controllers/PlayerProfileController.cs src/ReactClient/src/api/types.ts
git commit -m "feat(api): expose PlayerType on PlayerProfileViewModel"
```

### Task 3: Race constants and types on the frontend

**Files:**
- Create: `src/ReactClient/src/lib/race.ts`
- Test: `src/ReactClient/test/lib/race.test.ts`

- [ ] **Step 1: Write the failing test**

Create `src/ReactClient/test/lib/race.test.ts`:

```typescript
import { describe, it, expect } from 'vitest'
import { normalizeRace, RACES, type Race } from '@/lib/race'

describe('race helpers', () => {
  it('exposes the three canonical races', () => {
    expect(RACES).toEqual(['terran', 'zerg', 'protoss'])
  })

  it('normalizes known ids case-insensitively', () => {
    expect(normalizeRace('Terran')).toBe('terran')
    expect(normalizeRace('ZERG')).toBe('zerg')
    expect(normalizeRace('protoss')).toBe('protoss')
  })

  it('returns null for unknown or empty input', () => {
    expect(normalizeRace(null)).toBeNull()
    expect(normalizeRace('')).toBeNull()
    expect(normalizeRace('undead')).toBeNull()
  })
})
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd src/ReactClient && npx vitest run test/lib/race.test.ts
```

Expected: fails — `@/lib/race` does not exist yet.

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/lib/race.ts`:

```typescript
export const RACES = ['terran', 'zerg', 'protoss'] as const
export type Race = (typeof RACES)[number]

export function normalizeRace(id: string | null | undefined): Race | null {
  if (!id) return null
  const lower = id.toLowerCase()
  return (RACES as readonly string[]).includes(lower) ? (lower as Race) : null
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd src/ReactClient && npx vitest run test/lib/race.test.ts
```

Expected: all 3 pass.

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/lib/race.ts src/ReactClient/test/lib/race.test.ts
git commit -m "feat(client): race constants and normalizer"
```

### Task 4: `usePlayerRace` hook that sets `data-race` on `<html>`

**Files:**
- Create: `src/ReactClient/src/hooks/usePlayerRace.ts`
- Test: `src/ReactClient/test/hooks/usePlayerRace.test.tsx`

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/hooks/usePlayerRace.test.tsx`:

```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { usePlayerRace } from '@/hooks/usePlayerRace'
import apiClient from '@/api/client'

vi.mock('@/api/client', () => ({ default: { get: vi.fn() } }))

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>
}

describe('usePlayerRace', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-race')
    vi.clearAllMocks()
  })

  it('sets data-race on <html> when API returns terran', async () => {
    ;(apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { playerType: 'terran', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
    })
    renderHook(() => usePlayerRace(), { wrapper })
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-race')).toBe('terran')
    })
  })

  it('does not set data-race for unknown races', async () => {
    ;(apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { playerType: 'alien', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
    })
    renderHook(() => usePlayerRace(), { wrapper })
    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalled()
    })
    expect(document.documentElement.getAttribute('data-race')).toBeNull()
  })
})
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd src/ReactClient && npx vitest run test/hooks/usePlayerRace.test.tsx
```

Expected: fails — module not found.

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/hooks/usePlayerRace.ts`:

```typescript
import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel } from '@/api/types'
import { normalizeRace, type Race } from '@/lib/race'

export function usePlayerRace(): Race | null {
  const { data } = useQuery<PlayerProfileViewModel>({
    queryKey: ['playerprofile'],
    queryFn: () => apiClient.get('/api/playerprofile').then((r) => r.data),
    staleTime: 60_000,
  })

  const race = normalizeRace(data?.playerType ?? null)

  useEffect(() => {
    const root = document.documentElement
    if (race) {
      root.setAttribute('data-race', race)
    } else {
      root.removeAttribute('data-race')
    }
    return () => {
      root.removeAttribute('data-race')
    }
  }, [race])

  return race
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd src/ReactClient && npx vitest run test/hooks/usePlayerRace.test.tsx
```

Expected: 2 pass.

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/hooks/usePlayerRace.ts src/ReactClient/test/hooks/usePlayerRace.test.tsx
git commit -m "feat(client): usePlayerRace hook"
```

### Task 5: Rewrite `index.css` tokens, race palette, starfield, drop light theme

**Files:**
- Modify: `src/ReactClient/src/index.css` (full rewrite)

- [ ] **Step 1: Replace the entire file**

Overwrite `src/ReactClient/src/index.css`:

```css
@import "tailwindcss";

@theme {
  /* Neutral / surfaces */
  --color-background: #050810;
  --color-foreground: #F3F6FB;
  --color-card: rgba(5, 10, 20, 0.75);
  --color-card-foreground: #F3F6FB;
  --color-popover: #0b1324;
  --color-popover-foreground: #F3F6FB;
  --color-muted: hsl(222 40% 12%);
  --color-muted-foreground: #94a3b8;
  --color-secondary: hsl(222 40% 14%);
  --color-secondary-foreground: #F3F6FB;
  --color-border: hsl(222 40% 15%);
  --color-input: hsl(222 40% 13%);
  --color-accent: #3B82F6;
  --color-accent-foreground: #F3F6FB;
  --color-destructive: #EF4444;
  --color-destructive-foreground: #F3F6FB;
  --color-ring: #F59E0B;
  --radius: 0.25rem;

  /* SCO semantic roles */
  --color-resource: #22C55E;
  --color-interactive: #3B82F6;
  --color-prestige: #F59E0B;
  --color-enemy: #fca5a5;
  --color-success: #22C55E;
  --color-success-foreground: #052e14;
  --color-warning: #F59E0B;
  --color-warning-foreground: #2a1902;
  --color-danger: #EF4444;
  --color-danger-foreground: #2a0606;
  --color-info: #3B82F6;
  --color-info-foreground: #06132a;

  /* Primary = prestige (amber). Interactive is the blue token, not primary. */
  --color-primary: #F59E0B;
  --color-primary-foreground: #2a1902;

  /* Race defaults — overridden by [data-race="..."] on <html> */
  --color-race-primary: #CBD5E1;
  --color-race-secondary: #F59E0B;
  --color-race-emblem-fill: #1f2937;

  --font-sans: 'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, Menlo, Consolas, monospace;
}

/* Per-race overrides */
html[data-race="terran"] {
  --color-race-primary: #CBD5E1;
  --color-race-secondary: #F59E0B;
  --color-race-emblem-fill: #1f2937;
}
html[data-race="zerg"] {
  --color-race-primary: #B45309;
  --color-race-secondary: #ea580c;
  --color-race-emblem-fill: #3a1a08;
}
html[data-race="protoss"] {
  --color-race-primary: #FBBF24;
  --color-race-secondary: #0e7490;
  --color-race-emblem-fill: #0c1e2e;
}

* { border-color: var(--color-border); }

body {
  background-color: var(--color-background);
  color: var(--color-foreground);
  font-family: var(--font-sans);
  font-size: 13px;
  line-height: 1.5;
  overflow-x: hidden;
}

.label {
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 0.18em;
  color: var(--color-muted-foreground);
  font-weight: 700;
}

.mono, .tabular {
  font-variant-numeric: tabular-nums;
  font-family: var(--font-mono);
}

.text-resource { color: var(--color-resource); text-shadow: 0 0 6px rgba(34,197,94,0.35); }
.text-interactive { color: var(--color-interactive); }
.text-prestige { color: var(--color-prestige); }
.text-race-primary { color: var(--color-race-primary); }
.text-race-secondary { color: var(--color-race-secondary); }

.bg-starfield {
  background-color: #050810;
  background-image:
    radial-gradient(1px 1px at 20% 30%, rgba(255,255,255,0.8), transparent),
    radial-gradient(1px 1px at 70% 60%, rgba(255,255,255,0.6), transparent),
    radial-gradient(1.5px 1.5px at 40% 80%, rgba(220,220,255,0.7), transparent),
    radial-gradient(1px 1px at 90% 20%, rgba(255,255,255,0.5), transparent),
    radial-gradient(2px 2px at 10% 70%, rgba(200,220,255,0.7), transparent),
    radial-gradient(1px 1px at 55% 10%, rgba(255,255,255,0.6), transparent),
    radial-gradient(1px 1px at 30% 50%, rgba(255,255,255,0.7), transparent),
    radial-gradient(2px 2px at 80% 85%, rgba(220,220,255,0.5), transparent),
    radial-gradient(1px 1px at 15% 15%, rgba(255,255,255,0.6), transparent),
    radial-gradient(1px 1px at 65% 40%, rgba(255,255,255,0.5), transparent);
  background-size: 300px 300px, 200px 200px, 250px 250px, 280px 280px, 400px 400px, 350px 350px, 180px 180px, 320px 320px, 260px 260px, 220px 220px;
}

@keyframes page-fade-in { from { opacity: 0; } to { opacity: 1; } }
.page-enter { animation: page-fade-in 100ms ease-out; }

@media (prefers-reduced-motion: reduce) {
  .page-enter { animation: none; }
  *, *::before, *::after {
    transition-duration: 0ms !important;
    animation-duration: 0ms !important;
    animation-iteration-count: 1 !important;
  }
}
```

Note: the old `.light` selector block and the old `.bg-grid` utility are **gone** — intentional.

- [ ] **Step 2: Build and visually verify**

```bash
cd src/ReactClient && npm run build
```

Expected: build succeeds.

- [ ] **Step 3: Dev server sanity check (optional but recommended)**

```bash
cd src/ReactClient && npm run dev
# visit http://localhost:5173 — expect a near-black starfield body. Light theme toggle in the header will not work yet; that's fine, it's removed in Task 9.
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/index.css
git commit -m "feat(ui): SCO tokens, race palettes, starfield; drop light theme"
```

### Task 6: Race emblems (3 SVG components)

**Files:**
- Create: `src/ReactClient/src/components/icons/emblems/TerranEmblem.tsx`
- Create: `src/ReactClient/src/components/icons/emblems/ZergEmblem.tsx`
- Create: `src/ReactClient/src/components/icons/emblems/ProtossEmblem.tsx`
- Create: `src/ReactClient/src/components/icons/emblems/index.ts`
- Test: `src/ReactClient/test/components/emblems.test.tsx`

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/components/emblems.test.tsx`:

```typescript
import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'

describe('race emblems', () => {
  it('Terran emblem renders an svg with role=img', () => {
    const { getByRole } = render(<TerranEmblem aria-label="Terran" />)
    expect(getByRole('img')).toBeDefined()
  })

  it('Zerg emblem accepts custom size via width/height', () => {
    const { container } = render(<ZergEmblem width={48} height={48} />)
    const svg = container.querySelector('svg')!
    expect(svg.getAttribute('width')).toBe('48')
    expect(svg.getAttribute('height')).toBe('48')
  })

  it('Protoss emblem uses currentColor for stroke', () => {
    const { container } = render(<ProtossEmblem />)
    expect(container.innerHTML).toContain('currentColor')
  })
})
```

- [ ] **Step 2: Run test — expect failure (module missing)**

```bash
cd src/ReactClient && npx vitest run test/components/emblems.test.tsx
```

- [ ] **Step 3: Implement the three emblems**

Create `src/ReactClient/src/components/icons/emblems/TerranEmblem.tsx`:

```tsx
import type { SVGProps } from 'react'

export function TerranEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg
      role="img"
      width={width}
      height={height}
      viewBox="0 0 32 32"
      fill="none"
      {...rest}
    >
      <path d="M6 28 L6 10 L16 4 L26 10 L26 28 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <path d="M6 18 L26 18" stroke="var(--color-race-secondary)" strokeWidth="1.2" />
      <path d="M10 28 L10 22 L22 22 L22 28" stroke="currentColor" strokeWidth="1" />
    </svg>
  )
}
```

Create `src/ReactClient/src/components/icons/emblems/ZergEmblem.tsx`:

```tsx
import type { SVGProps } from 'react'

export function ZergEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg role="img" width={width} height={height} viewBox="0 0 32 32" fill="none" {...rest}>
      <path d="M16 3 C8 5 5 12 7 20 C9 26 14 28 16 28 C18 28 23 26 25 20 C27 12 24 5 16 3 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <path d="M11 12 Q14 14 13 18 M21 12 Q18 14 19 18" stroke="var(--color-race-secondary)" strokeWidth="0.8" fill="none" />
    </svg>
  )
}
```

Create `src/ReactClient/src/components/icons/emblems/ProtossEmblem.tsx`:

```tsx
import type { SVGProps } from 'react'

export function ProtossEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg role="img" width={width} height={height} viewBox="0 0 32 32" fill="none" {...rest}>
      <path d="M16 2 L26 10 L26 22 L16 30 L6 22 L6 10 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <circle cx="16" cy="16" r="4" fill="currentColor" opacity="0.5" />
      <circle cx="16" cy="16" r="2" fill="currentColor" />
    </svg>
  )
}
```

Create `src/ReactClient/src/components/icons/emblems/index.ts`:

```typescript
export { TerranEmblem } from './TerranEmblem'
export { ZergEmblem } from './ZergEmblem'
export { ProtossEmblem } from './ProtossEmblem'
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd src/ReactClient && npx vitest run test/components/emblems.test.tsx
```

Expected: 3 pass.

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/components/icons/emblems/ src/ReactClient/test/components/emblems.test.tsx
git commit -m "feat(ui): race emblem SVG components (Terran, Zerg, Protoss)"
```

### Task 7: `DataGrid` primitive

**Files:**
- Create: `src/ReactClient/src/components/ui/data-grid.tsx`
- Test: `src/ReactClient/test/components/data-grid.test.tsx`

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/components/data-grid.test.tsx`:

```typescript
import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import { DataGrid } from '@/components/ui/data-grid'

describe('DataGrid', () => {
  it('renders children inside a grid', () => {
    const { container } = render(
      <DataGrid cols={2}><div>a</div><div>b</div></DataGrid>
    )
    const grid = container.firstChild as HTMLElement
    expect(grid.style.gridTemplateColumns).toBe('repeat(2, minmax(0, 1fr))')
  })

  it('defaults to 3 columns', () => {
    const { container } = render(<DataGrid><div>a</div></DataGrid>)
    const grid = container.firstChild as HTMLElement
    expect(grid.style.gridTemplateColumns).toBe('repeat(3, minmax(0, 1fr))')
  })
})
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd src/ReactClient && npx vitest run test/components/data-grid.test.tsx
```

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/components/ui/data-grid.tsx`:

```tsx
import type { ReactNode, CSSProperties } from 'react'
import { cn } from '@/lib/utils'

interface DataGridProps {
  cols?: number
  className?: string
  children: ReactNode
}

/**
 * Hex-grid style container: items separated by hair-thin border-colored rules.
 * Achieved with gap:1px over a border-colored background.
 */
export function DataGrid({ cols = 3, className, children }: DataGridProps) {
  const style: CSSProperties = {
    gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`,
  }
  return (
    <div
      className={cn('grid gap-px bg-border border border-border', className)}
      style={style}
    >
      {children}
    </div>
  )
}

/**
 * Cell used inside a DataGrid — handles the background so the gap shows as a grid line.
 */
export function DataGridCell({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div className={cn('bg-background/50 p-3', className)}>{children}</div>
  )
}
```

- [ ] **Step 4: Run tests**

```bash
cd src/ReactClient && npx vitest run test/components/data-grid.test.tsx
```

Expected: 2 pass.

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/components/ui/data-grid.tsx src/ReactClient/test/components/data-grid.test.tsx
git commit -m "feat(ui): DataGrid + DataGridCell primitive"
```

### Task 8: `TimerRing` primitive (circular countdown)

**Files:**
- Create: `src/ReactClient/src/components/ui/timer-ring.tsx`
- Test: `src/ReactClient/test/components/timer-ring.test.tsx`

- [ ] **Step 1: Write the test**

Create `src/ReactClient/test/components/timer-ring.test.tsx`:

```typescript
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { TimerRing } from '@/components/ui/timer-ring'

describe('TimerRing', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('renders mm:ss format when time remaining is above 60s', () => {
    const to = new Date(Date.now() + 3 * 60_000 + 42_000).toISOString()
    render(<TimerRing to={to} size={34} />)
    expect(screen.getByText(/^3:42$/)).toBeDefined()
  })

  it('renders 0:ss format when time remaining is under 60s', () => {
    const to = new Date(Date.now() + 15_000).toISOString()
    render(<TimerRing to={to} size={34} />)
    expect(screen.getByText(/^0:15$/)).toBeDefined()
  })

  it('renders the stroke arc proportional to remaining fraction', () => {
    const to = new Date(Date.now() + 50_000).toISOString()
    const { container } = render(<TimerRing to={to} size={34} periodMs={100_000} />)
    const arc = container.querySelectorAll('circle')[1]
    // half elapsed = half remaining = offset half of dasharray
    expect(arc.getAttribute('stroke-dashoffset')).toBeDefined()
  })
})
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd src/ReactClient && npx vitest run test/components/timer-ring.test.tsx
```

- [ ] **Step 3: Implement**

Create `src/ReactClient/src/components/ui/timer-ring.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'

interface TimerRingProps {
  /** ISO datetime of the next tick */
  to: string
  /** Period of the cycle in ms, used to compute arc progress. Defaults to 5 minutes. */
  periodMs?: number
  size?: number
  strokeWidth?: number
  className?: string
}

function formatMmSs(ms: number): string {
  if (ms <= 0) return '0:00'
  const total = Math.floor(ms / 1000)
  const m = Math.floor(total / 60)
  const s = total % 60
  return `${m}:${s.toString().padStart(2, '0')}`
}

export function TimerRing({ to, periodMs = 5 * 60_000, size = 34, strokeWidth = 2.5, className }: TimerRingProps) {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(id)
  }, [])

  const target = new Date(to).getTime()
  const remaining = Math.max(0, target - now)
  const r = size / 2 - strokeWidth
  const c = 2 * Math.PI * r
  const fraction = Math.min(1, remaining / periodMs)
  const offset = c * (1 - fraction)

  return (
    <div className={cn('relative inline-flex items-center justify-center', className)} style={{ width: size, height: size }}>
      <svg width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
        <circle cx={size / 2} cy={size / 2} r={r} stroke="var(--color-border)" strokeWidth={strokeWidth} fill="none" />
        <circle cx={size / 2} cy={size / 2} r={r} stroke="var(--color-race-secondary)" strokeWidth={strokeWidth} fill="none" strokeDasharray={c} strokeDashoffset={offset} strokeLinecap="round" />
      </svg>
      <span className="absolute inset-0 flex items-center justify-center mono text-[10px] font-semibold text-race-secondary" style={{ color: 'var(--color-race-secondary)' }}>
        {formatMmSs(remaining)}
      </span>
    </div>
  )
}
```

- [ ] **Step 4: Run tests**

```bash
cd src/ReactClient && npx vitest run test/components/timer-ring.test.tsx
```

Expected: 3 pass.

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/components/ui/timer-ring.tsx src/ReactClient/test/components/timer-ring.test.tsx
git commit -m "feat(ui): TimerRing circular countdown primitive"
```

### Task 9: New NavMenu — 6 primary + utility footer

**Files:**
- Modify: `src/ReactClient/src/components/NavMenu.tsx` (full rewrite)

- [ ] **Step 1: Rewrite the component**

Overwrite `src/ReactClient/src/components/NavMenu.tsx`:

```tsx
import { NavLink } from 'react-router'
import {
  LayersIcon, ShieldIcon, FlaskConicalIcon, ShoppingCartIcon,
  RadioIcon, UsersIcon, BarChart2Icon, MessageSquareIcon, MailIcon,
  BookOpenIcon, HelpCircleIcon, UserIcon,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useCurrentGame } from '@/contexts/CurrentGameContext'
import { useNavBadges } from '@/hooks/useNavBadges'
import { Badge } from '@/components/ui/badge'

function PrimaryItem({
  to, icon: Icon, label, badge,
}: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  label: string
  badge?: number
}) {
  return (
    <li>
      <NavLink
        to={to}
        className={({ isActive }) => cn(
          'relative flex items-center gap-2 pl-4 pr-3 py-2.5 text-sm transition-colors min-h-[44px]',
          isActive
            ? 'text-foreground bg-[linear-gradient(90deg,var(--color-race-primary)_0%,transparent_100%)]/10 before:absolute before:left-0 before:top-1.5 before:bottom-1.5 before:w-[2px] before:bg-[color:var(--color-race-secondary)]'
            : 'text-muted-foreground hover:text-foreground hover:bg-secondary/40',
        )}
      >
        <Icon className="h-4 w-4 shrink-0 text-race-secondary" aria-hidden />
        <span className="flex-1 truncate">{label}</span>
        {badge != null && badge > 0 && (
          <Badge variant="secondary" className="h-5 px-1.5 text-[10px] tabular-nums">{badge}</Badge>
        )}
      </NavLink>
    </li>
  )
}

function UtilityLink({ to, icon: Icon, label, badge }: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  label: string
  badge?: number
}) {
  return (
    <NavLink to={to} className={({ isActive }) => cn(
      'inline-flex items-center gap-1.5 text-[11px] text-interactive hover:text-[color:var(--color-race-secondary)] py-1 pr-3',
      isActive && 'text-foreground',
    )}>
      <Icon className="h-3 w-3" aria-hidden /> {label}
      {badge != null && badge > 0 && <span className="ml-1 text-[10px] text-muted-foreground">({badge})</span>}
    </NavLink>
  )
}

export function NavMenu() {
  const { gameId } = useCurrentGame()
  const badges = useNavBadges()
  if (!gameId) return null
  const g = (path: string) => `/games/${gameId}/${path}`

  return (
    <nav aria-label="Game navigation" className="flex flex-col h-full">
      <ul className="space-y-0.5 pt-2">
        <PrimaryItem to={g('base')} icon={LayersIcon} label="Base" />
        <PrimaryItem to={g('units')} icon={ShieldIcon} label="Units" />
        <PrimaryItem to={g('research')} icon={FlaskConicalIcon} label="Research" />
        <PrimaryItem to={g('market')} icon={ShoppingCartIcon} label="Market" />
        <PrimaryItem to={g('live')} icon={RadioIcon} label="Live View" />
        <PrimaryItem to={g('alliances')} icon={UsersIcon} label="Alliance" badge={badges.alliances} />
      </ul>
      <div className="mt-auto border-t border-border px-4 pt-3 pb-4 text-[11px] leading-relaxed">
        <div className="flex flex-wrap gap-x-3 gap-y-1">
          <UtilityLink to={g('ranking')} icon={BarChart2Icon} label="Ranking" />
          <UtilityLink to={g('messages')} icon={MailIcon} label="Messages" badge={badges.messages} />
          <UtilityLink to={g('chat')} icon={MessageSquareIcon} label="Chat" />
          <UtilityLink to={g('unitdefinitions')} icon={BookOpenIcon} label="Codex" />
          <UtilityLink to={g('profile')} icon={UserIcon} label="Profile" />
          <UtilityLink to={g('help')} icon={HelpCircleIcon} label="Help" />
        </div>
      </div>
    </nav>
  )
}
```

- [ ] **Step 2: Typecheck & test existing suites**

```bash
cd src/ReactClient && npx tsc --noEmit && npx vitest run
```

Expected: passes. Some E2E tests may reference the old nav items — those are updated in Task 13.

- [ ] **Step 3: Commit**

```bash
git add src/ReactClient/src/components/NavMenu.tsx
git commit -m "feat(ui): 6-item nav + utility footer (SCO structure)"
```

### Task 10: Rewrite GameLayout — 4-region header, race chrome, no theme toggle

**Files:**
- Modify: `src/ReactClient/src/layouts/GameLayout.tsx` (full rewrite)

- [ ] **Step 1: Rewrite**

Overwrite `src/ReactClient/src/layouts/GameLayout.tsx`:

```tsx
import { Suspense, useState, useCallback, useEffect, useRef } from 'react'
import { Outlet, useParams, Navigate, useLocation, useNavigate, NavLink } from 'react-router'
import {
  LogOutIcon, MenuIcon, XIcon, UserIcon, HistoryIcon, LineChartIcon, Trophy,
} from 'lucide-react'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { useCurrentUser } from '@/contexts/CurrentUserContext'
import { usePlayerRace } from '@/hooks/usePlayerRace'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { NotificationToasts, useNotificationToasts } from '@/components/NotificationToast'
import { TutorialOverlay } from '@/components/TutorialOverlay'
import { PageLoader } from '@/components/PageLoader'
import { GameStatusBanner } from '@/components/GameStatusBanner'
import { TimerRing } from '@/components/ui/timer-ring'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'
import { useSignalR } from '@/hooks/useSignalR'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog'
import {
  DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'
import type { Race } from '@/lib/race'

interface GameFinalizedPayload {
  gameId: string
  winnerId: string | null
  winnerName: string | null
}

function RaceEmblem({ race, size = 24 }: { race: Race | null; size?: number }) {
  const style = { color: 'var(--color-race-primary)' }
  if (race === 'zerg') return <ZergEmblem width={size} height={size} style={style} />
  if (race === 'protoss') return <ProtossEmblem width={size} height={size} style={style} />
  return <TerranEmblem width={size} height={size} style={style} />
}

function RaceStripe() {
  return (
    <div
      className="h-[6px]"
      style={{
        background: 'linear-gradient(90deg, transparent 0%, var(--color-race-primary) 30%, var(--color-race-secondary) 50%, var(--color-race-primary) 70%, transparent 100%)',
      }}
    />
  )
}

function GameOverDialog({ payload, gameId, onClose }: { payload: GameFinalizedPayload; gameId: string; onClose: () => void }) {
  const navigate = useNavigate()
  const [countdown, setCountdown] = useState(8)
  useEffect(() => {
    if (countdown <= 0) { navigate(`/games/${gameId}/results`); return }
    const t = setTimeout(() => setCountdown((c) => c - 1), 1000)
    return () => clearTimeout(t)
  }, [countdown, navigate, gameId])

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <div className="flex justify-center mb-2"><Trophy className="h-10 w-10 text-prestige" aria-hidden /></div>
          <DialogTitle className="text-center text-2xl">{payload.winnerName ? `${payload.winnerName} Wins!` : 'Game Over!'}</DialogTitle>
        </DialogHeader>
        <p className="text-sm text-muted-foreground text-center">Redirecting to results in {countdown}s…</p>
        <DialogFooter><Button onClick={() => navigate(`/games/${gameId}/results`)}>View Results Now</Button></DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function UserMenu({ race }: { race: Race | null }) {
  const { user } = useCurrentUser()
  const navigate = useNavigate()
  const { gameId } = useCurrentGame()
  if (!user) return null
  const g = (p: string) => (gameId ? `/games/${gameId}/${p}` : `/${p}`)
  const name = user.displayName ?? user.playerName ?? 'Commander'
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button className="flex items-center gap-2 text-sm pr-1 hover:opacity-80">
          <span className="font-semibold" style={{ color: 'var(--color-race-primary)' }}>{name}</span>
          {race && <span className="text-[11px] text-muted-foreground capitalize">· {race}</span>}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel className="text-xs text-muted-foreground">{name}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => navigate(g('profile'))}><UserIcon className="mr-2 h-4 w-4" />Profile</DropdownMenuItem>
        <DropdownMenuItem onSelect={() => navigate(g('history'))}><HistoryIcon className="mr-2 h-4 w-4" />History</DropdownMenuItem>
        <DropdownMenuItem onSelect={() => navigate(g('stats'))}><LineChartIcon className="mr-2 h-4 w-4" />Stats</DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => { window.location.href = '/signout' }}><LogOutIcon className="mr-2 h-4 w-4" />Sign out</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

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
      <NavMenu />
    </>
  )
}

function GameLayoutInner() {
  const { gameId, currentGame } = useCurrentGame()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [gameFinalized, setGameFinalized] = useState<GameFinalizedPayload | null>(null)
  const location = useLocation()
  const signalR = useSignalR('/gamehub')
  const { toasts, addToast, dismiss } = useNotificationToasts()
  const gameFinalizedHandlerRef = useRef<((...args: unknown[]) => void) | null>(null)
  const race = usePlayerRace()

  const onRealTimeNotification = useCallback(
    (n: { type: string; title: string; body?: string; createdAt: string }) => addToast(n),
    [addToast],
  )

  useEffect(() => {
    if (!signalR) return
    const handler = (...args: unknown[]) => {
      const p = args[0] as GameFinalizedPayload
      if (p?.gameId === gameId) setGameFinalized(p)
    }
    gameFinalizedHandlerRef.current = handler
    signalR.on('GameFinalized', handler)
    return () => { signalR.off('GameFinalized', handler) }
  }, [signalR, gameId])

  useEffect(() => { setSidebarOpen(false) }, [location.pathname])

  if (!gameId) return <Navigate to="/games" />

  const nextTick = currentGame?.nextTickAt ?? null

  return (
    <div className="flex h-screen overflow-hidden bg-starfield">
      <aside className="hidden md:flex w-56 shrink-0 flex-col bg-card/40 backdrop-blur-sm border-r border-border overflow-y-auto" aria-label="Game sidebar">
        <SidebarContent race={race} />
      </aside>

      {sidebarOpen && (
        <div className="fixed inset-0 z-40 md:hidden" onClick={() => setSidebarOpen(false)} aria-hidden>
          <div className="absolute inset-0 bg-black/60" />
        </div>
      )}
      <aside className={cn(
        'fixed inset-y-0 left-0 z-50 w-56 flex flex-col border-r border-border bg-card overflow-y-auto transition-transform duration-200 md:hidden',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full',
      )} aria-label="Game sidebar">
        <div className="flex items-center justify-end p-2 border-b border-border">
          <Button variant="ghost" size="icon" onClick={() => setSidebarOpen(false)} aria-label="Close menu">
            <XIcon className="h-5 w-5" />
          </Button>
        </div>
        <SidebarContent race={race} />
      </aside>

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <header className="shrink-0 border-b border-border bg-background/40 backdrop-blur-sm">
          <div className="h-[52px] flex items-center justify-between px-3 sm:px-4 gap-3">
            <div className="flex items-center gap-3 min-w-0">
              <Button variant="ghost" size="icon" onClick={() => setSidebarOpen(true)} aria-label="Open menu" className="md:hidden -ml-1">
                <MenuIcon className="h-5 w-5" />
              </Button>
              <UserMenu race={race} />
              {nextTick && <TimerRing to={nextTick} />}
            </div>
            <div className="flex items-center gap-3 sm:gap-4 min-w-0">
              <div className="hidden md:block"><PlayerResources gameId={gameId} /></div>
              <NotificationBell signalR={signalR} onRealTimeNotification={onRealTimeNotification} />
            </div>
          </div>
          <div className="md:hidden border-t border-border px-3 py-1.5">
            <PlayerResources gameId={gameId} />
          </div>
        </header>

        {currentGame && <GameStatusBanner game={currentGame} />}

        <main className="flex-1 overflow-y-auto p-3 sm:p-4">
          <Suspense fallback={<PageLoader />}>
            <div key={location.pathname} className="page-enter">
              <Outlet />
            </div>
          </Suspense>
        </main>
      </div>

      <NotificationToasts toasts={toasts} dismiss={dismiss} gameId={gameId} />
      <TutorialOverlay />
      {gameFinalized && <GameOverDialog payload={gameFinalized} gameId={gameId} onClose={() => setGameFinalized(null)} />}
    </div>
  )
}

export function GameLayout() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return <Navigate to="/games" />
  return (
    <CurrentGameProvider>
      <GameLayoutInner />
    </CurrentGameProvider>
  )
}
```

- [ ] **Step 2: Confirm the next-tick ISO field exists on `GameDetailViewModel`**

```bash
grep -n "nextTick\|Tick" src/ReactClient/src/api/types.ts | head
```

If not present under that exact name, find the equivalent field (likely `nextTickTime` or `nextTickAtUtc`) and swap the `<TimerRing to={nextTick}>` prop. If no such field exists, render `nextTick ? <TimerRing … /> : null`, and open a follow-up issue to add it in a separate PR.

Also pass `periodMs` equal to the current tick interval if known (e.g., from `currentGame.settings.tickIntervalSeconds * 1000`). If not readily available, leave the default of 5 minutes — the ring just won't perfectly fill but is still a useful visual.

- [ ] **Step 3: Typecheck, build**

```bash
cd src/ReactClient && npx tsc --noEmit && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/layouts/GameLayout.tsx
git commit -m "feat(ui): rewrite GameLayout — race chrome, 4-region header, timer ring, drop theme toggle"
```

### Task 11: PlayerResources — green mono + glow

**Files:**
- Modify: `src/ReactClient/src/components/PlayerResources.tsx` (full rewrite)

- [ ] **Step 1: Rewrite**

Overwrite `src/ReactClient/src/components/PlayerResources.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'
import { formatNumber } from '@/lib/formatters'

export function PlayerResources({ gameId }: { gameId: string }) {
  const { data } = useQuery({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get<PlayerResourcesViewModel>('/api/resources').then((r) => r.data),
    refetchInterval: 10_000,
  })
  if (!data) return null

  const all = [
    ...Object.entries(data.primaryResource.cost),
    ...Object.entries(data.secondaryResources.cost),
  ].filter(([, v]) => v !== undefined) as [string, number][]

  return (
    <div className="flex items-center gap-4 whitespace-nowrap">
      {all.map(([name, value]) => {
        const isLand = name.toLowerCase() === 'land'
        return (
          <div key={name} className="flex items-baseline gap-1.5">
            <span className="label text-[9px]">{name}</span>
            <span
              className={`mono text-[13px] font-semibold ${isLand ? 'text-interactive' : 'text-resource'}`}
              style={isLand ? undefined : { textShadow: '0 0 6px rgba(34,197,94,0.35)' }}
            >
              {formatNumber(value)}
            </span>
          </div>
        )
      })}
    </div>
  )
}
```

- [ ] **Step 2: Typecheck**

```bash
cd src/ReactClient && npx tsc --noEmit
```

- [ ] **Step 3: Commit**

```bash
git add src/ReactClient/src/components/PlayerResources.tsx
git commit -m "feat(ui): PlayerResources green mono + glow for resource values"
```

### Task 12: Drop `ThemeProvider` from App composition

**Files:**
- Modify: `src/ReactClient/src/App.tsx`

- [ ] **Step 1: Find and remove ThemeProvider**

```bash
grep -n "ThemeProvider" src/ReactClient/src/App.tsx
```

Open the file and remove the `<ThemeProvider>` wrapper + its import. The composition becomes (example):

```tsx
// Before:
// <ErrorBoundary><ThemeProvider><CurrentUserProvider><Outlet/></CurrentUserProvider></ThemeProvider></ErrorBoundary>
// After:
// <ErrorBoundary><CurrentUserProvider><Outlet/></CurrentUserProvider></ErrorBoundary>
```

The `ThemeContext.tsx` file itself is retained until Phase 5 cleanup (as an abandoned module with no imports). This avoids rebuilding anything that still transiently imports it.

- [ ] **Step 2: Typecheck**

```bash
cd src/ReactClient && npx tsc --noEmit
```

Any errors → file still imports `useTheme` or similar. Remove those usages (the theme toggle was already removed from GameLayout in Task 10).

- [ ] **Step 3: Commit**

```bash
git add src/ReactClient/src/App.tsx
git commit -m "refactor(client): drop ThemeProvider (dark-only)"
```

### Task 13: Update E2E selectors that reference removed nav items / theme toggle

**Files:**
- Modify: whichever Playwright specs reference "Trade" nav link, "Alliance Ranking" nav link, or `aria-label="Switch to light mode"` etc.

- [ ] **Step 1: Find broken selectors**

```bash
cd src/ReactClient
grep -rn "Switch to light mode\|Alliance Ranking\|'Trade'" e2e/ || true
```

- [ ] **Step 2: Update each match**

- Replace `Switch to light mode` / `Switch to dark mode` references → delete those lines (toggle gone).
- Replace `Alliance Ranking` nav → navigate via Ranking page tab (Phase 3 will add this; for now the E2E can navigate directly to `/games/{id}/allianceranking`).
- Replace `Trade` nav → navigate directly to `/games/{id}/trade` (Phase 3 adds the Market tab).

- [ ] **Step 3: Run e2e smoke locally if possible (skip if no browser)**

```bash
cd src/ReactClient && npx playwright test --list | head -20
# Sanity: list passes. Full run may be skipped in this phase.
```

- [ ] **Step 4: Commit**

```bash
git add -A src/ReactClient/e2e
git commit -m "test(e2e): update selectors for shell redesign"
```

### Task 14: Manual visual smoke pass + screenshots for PR

- [ ] **Step 1: Launch dev server**

```bash
cd src/ReactClient && npm run dev
```

Open http://localhost:5173 and sign in. Join or open a game as Terran. Confirm:
- Starfield is visible behind main content.
- Sidebar has race stripe + Terran emblem + race label.
- Active nav item has amber left border.
- Header shows name · race · timer ring · resources · bell.
- Theme toggle is gone.
- Utility links at the bottom of the sidebar work.

Repeat for Zerg and Protoss accounts (or force `data-race="zerg"` in DevTools to verify the chrome shifts).

- [ ] **Step 2: Capture screenshots for the PR description**

Any 3 representative pages (Base, Units, Ranking — still the old body design). Attach to the PR.

### Task 15: Open PR for Phase 1

- [ ] **Step 1: Push**

```bash
git push -u origin feat/sco-flair-phase1-foundation
```

- [ ] **Step 2: Open PR**

```bash
gh pr create --title "feat(ui): SCO flair — Phase 1 foundation (tokens, shell, race chrome)" --body "$(cat <<'EOF'
## Summary

Lands the foundation layer of the SCO flair redesign per [`docs/superpowers/specs/2026-04-19-sco-flair-redesign-design.md`](docs/superpowers/specs/2026-04-19-sco-flair-redesign-design.md).

- Token system overhauled (SCO palette: resource green, interactive blue, prestige amber, race primary/secondary).
- Starfield background utility; retires `bg-grid`.
- Backend: `PlayerType` exposed on `/api/playerprofile`.
- `usePlayerRace` hook sets `data-race` on `<html>`.
- Sidebar: 6 primary + utility footer, race stripe and emblem header.
- Header: 4 regions — commander/race + TimerRing + resources + notification bell.
- Drops theme toggle and connection pill.

Page bodies still look like the old design — that's Phase 3.

## Test plan

- [ ] `dotnet build --configuration Release` passes
- [ ] `dotnet test` passes
- [ ] `npm run build` passes
- [ ] `npx vitest run` passes
- [ ] Manual: sign in as Terran, Zerg, Protoss — confirm chrome shifts per race
- [ ] Manual: confirm starfield visible, sidebar utility links work, timer ring updates
EOF
)"
```

- [ ] **Step 3: Clean up the worktree after PR is open**

```bash
cd /home/chris/repos/my/bge
git worktree remove /tmp/bge-work/sco-flair-phase1
```

---

## Phase 2 — SVG icon kit

**Outcome:** All custom SVG icon components exist as TSX files under `src/ReactClient/src/components/icons/`. Each icon accepts standard SVG props (width, height, className). No page consumes them yet — Phase 3's job. The goal of Phase 2 is the icon kit as a pure asset library.

**Worktree:**

```bash
git worktree add /tmp/bge-work/sco-flair-phase2 -b feat/sco-flair-phase2-icons master
# After Phase 1 is merged, rebase from master
```

### Task 16: Nav icon replacements (12 icons)

**Files:** Create `src/ReactClient/src/components/icons/nav/*.tsx` — one file per icon.

The icons visually match the nav-bar art from the brainstorming mockups. Each is a 16×16 viewBox and uses `stroke="currentColor"` so the NavMenu's `text-race-secondary` class paints them.

- [ ] **Step 1: Create the files**

For each of Base, Units, Research, Market, LiveView, Alliance, Ranking, Messages, Chat, Codex, Profile, Help — create a component. Pattern:

`src/ReactClient/src/components/icons/nav/BaseIcon.tsx`:

```tsx
import type { SVGProps } from 'react'
export function BaseIcon(p: SVGProps<SVGSVGElement>) {
  return (
    <svg width={16} height={16} viewBox="0 0 16 16" fill="none" {...p}>
      <rect x="2" y="5" width="12" height="9" stroke="currentColor" strokeWidth="1" />
      <path d="M4 5 L4 3 L12 3 L12 5" stroke="currentColor" strokeWidth="1" />
    </svg>
  )
}
```

`UnitsIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M8 2 L14 6 L14 14 L2 14 L2 6 Z" stroke="currentColor" strokeWidth="1" />
</svg>
```

`ResearchIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <circle cx="8" cy="8" r="5" stroke="currentColor" strokeWidth="1" />
  <circle cx="8" cy="8" r="1.5" fill="currentColor" />
</svg>
```

`MarketIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M2 8 L14 8 M6 4 L2 8 L6 12 M10 12 L14 8 L10 4" stroke="currentColor" strokeWidth="1" />
</svg>
```

`LiveViewIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M8 2 L14 8 L8 14 L2 8 Z" stroke="currentColor" strokeWidth="1" />
</svg>
```

`AllianceIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <circle cx="5" cy="7" r="2.5" stroke="currentColor" strokeWidth="1" />
  <circle cx="11" cy="7" r="2.5" stroke="currentColor" strokeWidth="1" />
  <path d="M2 14 C2 11 4 10 5 10 C6 10 8 11 8 14 M8 14 C8 11 10 10 11 10 C12 10 14 11 14 14" stroke="currentColor" strokeWidth="1" />
</svg>
```

`RankingIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M8 2 L9.8 6.5 L14.6 6.7 L10.8 9.7 L12.2 14.3 L8 11.6 L3.8 14.3 L5.2 9.7 L1.4 6.7 L6.2 6.5 Z" stroke="currentColor" strokeWidth="1" />
</svg>
```

`MessagesIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <rect x="2" y="4" width="12" height="9" stroke="currentColor" strokeWidth="1" />
  <path d="M2 4 L8 9 L14 4" stroke="currentColor" strokeWidth="1" />
</svg>
```

`ChatIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M2 4 L14 4 L14 11 L9 11 L6 14 L6 11 L2 11 Z" stroke="currentColor" strokeWidth="1" />
</svg>
```

`CodexIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <path d="M3 2 L13 2 L13 14 L3 14 Z M3 5 L13 5 M3 9 L13 9" stroke="currentColor" strokeWidth="1" />
</svg>
```

`ProfileIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <circle cx="8" cy="6" r="3" stroke="currentColor" strokeWidth="1" />
  <path d="M3 14 C3 11 5 10 8 10 C11 10 13 11 13 14" stroke="currentColor" strokeWidth="1" />
</svg>
```

`HelpIcon`:
```tsx
<svg viewBox="0 0 16 16" fill="none" width={16} height={16} {...p}>
  <circle cx="8" cy="8" r="6" stroke="currentColor" strokeWidth="1" />
  <path d="M6 6 Q6 4 8 4 Q10 4 10 6 Q10 7 8 8 L8 10" stroke="currentColor" strokeWidth="1" fill="none" />
  <circle cx="8" cy="12" r="0.5" fill="currentColor" />
</svg>
```

Wrap each in `export function XxxIcon(p: SVGProps<SVGSVGElement>) { return <svg>...</svg> }`. Each takes the default 16×16 and passes `{...p}` for overrides.

- [ ] **Step 2: Create `src/ReactClient/src/components/icons/nav/index.ts`**

```typescript
export { BaseIcon } from './BaseIcon'
export { UnitsIcon } from './UnitsIcon'
export { ResearchIcon } from './ResearchIcon'
export { MarketIcon } from './MarketIcon'
export { LiveViewIcon } from './LiveViewIcon'
export { AllianceIcon } from './AllianceIcon'
export { RankingIcon } from './RankingIcon'
export { MessagesIcon } from './MessagesIcon'
export { ChatIcon } from './ChatIcon'
export { CodexIcon } from './CodexIcon'
export { ProfileIcon } from './ProfileIcon'
export { HelpIcon } from './HelpIcon'
```

- [ ] **Step 3: Smoke test**

Create `src/ReactClient/test/components/nav-icons.test.tsx`:

```tsx
import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import * as NavIcons from '@/components/icons/nav'

describe('nav icons', () => {
  Object.entries(NavIcons).forEach(([name, Icon]) => {
    it(`${name} renders an svg`, () => {
      const { container } = render(<Icon />)
      expect(container.querySelector('svg')).not.toBeNull()
    })
  })
})
```

Run:

```bash
cd src/ReactClient && npx vitest run test/components/nav-icons.test.tsx
```

Expected: 12 tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/components/icons/nav/ src/ReactClient/test/components/nav-icons.test.tsx
git commit -m "feat(ui): nav icon SVG kit (12 icons)"
```

### Task 17: Action icons (5 icons)

**Files:** `src/ReactClient/src/components/icons/actions/{AttackIcon,DefendIcon,RecallIcon,SplitIcon,MergeIcon}.tsx` + `index.ts`

- [ ] **Step 1: Create each file**

`AttackIcon`:
```tsx
import type { SVGProps } from 'react'
export function AttackIcon(p: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" width={20} height={20} fill="none" {...p}>
      <circle cx="10" cy="10" r="7" stroke="currentColor" strokeWidth="1.2" />
      <path d="M10 3 L10 7 M10 13 L10 17 M3 10 L7 10 M13 10 L17 10" stroke="currentColor" strokeWidth="1.2" />
      <circle cx="10" cy="10" r="1.5" fill="currentColor" />
    </svg>
  )
}
```

`DefendIcon`:
```tsx
<svg viewBox="0 0 20 20" width={20} height={20} fill="none" {...p}>
  <path d="M10 2 L17 5 L17 11 C17 15 13 18 10 18 C7 18 3 15 3 11 L3 5 Z" stroke="currentColor" strokeWidth="1.2" />
</svg>
```

`RecallIcon`:
```tsx
<svg viewBox="0 0 20 20" width={20} height={20} fill="none" {...p}>
  <path d="M3 10 L17 10 M9 4 L3 10 L9 16" stroke="currentColor" strokeWidth="1.5" />
</svg>
```

`SplitIcon`:
```tsx
<svg viewBox="0 0 20 20" width={20} height={20} fill="none" {...p}>
  <path d="M10 3 L10 9 M10 9 L5 15 M10 9 L15 15" stroke="currentColor" strokeWidth="1.5" />
</svg>
```

`MergeIcon`:
```tsx
<svg viewBox="0 0 20 20" width={20} height={20} fill="none" {...p}>
  <path d="M5 5 L10 11 M15 5 L10 11 M10 11 L10 17" stroke="currentColor" strokeWidth="1.5" />
</svg>
```

- [ ] **Step 2: `index.ts` re-exports**

- [ ] **Step 3: Smoke test (extend `icons.test.tsx` or add a new one analogous to Task 16 Step 3)**

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/components/icons/actions/
git commit -m "feat(ui): action icon SVG kit (attack, defend, recall, split, merge)"
```

### Task 18: Resource icons (3 small + 3 large)

**Files:** `src/ReactClient/src/components/icons/resources/{MineralsIcon,GasIcon,LandIcon,MineralsLarge,GasLarge,LandLarge}.tsx` + `index.ts`

- [ ] **Step 1: Create small (16×16) and large (96×96) variants**

`MineralsIcon`:
```tsx
import type { SVGProps } from 'react'
export function MineralsIcon(p: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} fill="none" {...p}>
      <path d="M5 3 L11 3 L14 8 L11 13 L5 13 L2 8 Z" stroke="var(--color-resource)" strokeWidth="1" fill="rgba(34,197,94,0.15)" />
      <path d="M5 3 L8 8 L11 3 M2 8 L8 8 L14 8 M5 13 L8 8 L11 13" stroke="var(--color-resource)" strokeWidth="0.6" opacity="0.7" />
    </svg>
  )
}
```

`GasIcon`:
```tsx
<svg viewBox="0 0 16 16" width={16} height={16} fill="none" {...p}>
  <ellipse cx="8" cy="11" rx="5" ry="3" stroke="var(--color-resource)" strokeWidth="1" fill="rgba(34,197,94,0.15)" />
  <path d="M5 11 Q6 6 8 3 Q10 6 11 11" stroke="var(--color-resource)" strokeWidth="1" fill="none" />
</svg>
```

`LandIcon`:
```tsx
<svg viewBox="0 0 16 16" width={16} height={16} fill="none" {...p}>
  <path d="M2 12 L14 12 L12 6 L10 8 L8 4 L6 9 L4 7 Z" stroke="var(--color-interactive)" strokeWidth="1" fill="rgba(59,130,246,0.15)" />
</svg>
```

The "Large" variants re-use the same paths scaled to `viewBox="0 0 96 96"` with thicker strokes (`strokeWidth="3"`) and brighter fills. Implement the three analogous large components.

- [ ] **Step 2: `index.ts` re-exports**

- [ ] **Step 3: Commit**

```bash
git add src/ReactClient/src/components/icons/resources/
git commit -m "feat(ui): resource icon SVG kit (minerals, gas, land; small + large)"
```

### Task 19: Unit silhouettes — per race (~30 components)

**Files:** `src/ReactClient/src/components/icons/units/{terran,zerg,protoss}/*Icon.tsx` + `index.ts`

The unit lists come from `GameDefinition.SCO`. Expected per-race unit set:
- Terran: SCV, Marine, Firebat, Ghost, Vulture, Goliath, Siegetank, Wraith, Battlecruiser, MissileTurret
- Zerg: Drone, Zergling, Hydralisk, Lurker, Ultralisk, Mutalisk, Guardian, Devourer, SunkenColony
- Protoss: Probe, Zealot, Dragoon, Templar, Archon, Scout, Carrier, Observer, Reaver, Photon

- [ ] **Step 1: Generate the 30 components**

Each component follows the same shape:

```tsx
import type { SVGProps } from 'react'
export function MarineIcon(p: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 32 32" width={28} height={28} fill="none" {...p}>
      <rect x="10" y="8" width="12" height="18" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <circle cx="16" cy="14" r="2" fill="var(--color-race-secondary)" />
      <rect x="6" y="14" width="4" height="6" stroke="currentColor" strokeWidth="1" />
      <rect x="22" y="14" width="4" height="6" stroke="currentColor" strokeWidth="1" />
    </svg>
  )
}
```

Shape guidelines, per race:
- **Terran** (angular/industrial): rectangles, triangles, hazard stripe accent (`stroke="var(--color-race-secondary)"` short segment).
- **Zerg** (organic/curved): béziers, blobby fills, asymmetric elements.
- **Protoss** (geometric/crystalline): hexagons/rhombi, inner glow via opacity-reduced fills.

Keep each icon ~15 lines of JSX. Do NOT reference StarCraft IP images — draw from silhouette reference only.

- [ ] **Step 2: Re-exports per race + top-level**

`src/ReactClient/src/components/icons/units/terran/index.ts`:

```typescript
export { ScvIcon } from './ScvIcon'
export { MarineIcon } from './MarineIcon'
// ... all 10
```

`src/ReactClient/src/components/icons/units/index.ts`:

```typescript
import * as Terran from './terran'
import * as Zerg from './zerg'
import * as Protoss from './protoss'

export { Terran, Zerg, Protoss }

export function unitIcon(race: string, unitId: string) {
  // lookup by race + normalized unit id; returns component or a fallback ShieldIcon
  // implement as a switch/map — write explicit entries, no dynamic imports
}
```

Implement `unitIcon` as a plain switch over `(race, unitId)` returning the corresponding component (or `null` if unknown). Example:

```typescript
const TABLE: Record<string, Record<string, React.ComponentType<SVGProps<SVGSVGElement>>>> = {
  terran: { scv: Terran.ScvIcon, marine: Terran.MarineIcon, /* ... */ },
  zerg: { drone: Zerg.DroneIcon, /* ... */ },
  protoss: { probe: Protoss.ProbeIcon, /* ... */ },
}
export function unitIcon(race: string, unitId: string) {
  return TABLE[race.toLowerCase()]?.[unitId.toLowerCase()] ?? null
}
```

- [ ] **Step 3: Smoke test**

Assert the table has the expected unit keys. Add to `test/components/unit-icons.test.tsx`:

```typescript
import { describe, it, expect } from 'vitest'
import { unitIcon } from '@/components/icons/units'

describe('unit icons', () => {
  it('returns a component for every Terran unit', () => {
    ['scv','marine','firebat','ghost','vulture','goliath','siegetank','wraith','battlecruiser','missileturret']
      .forEach(u => expect(unitIcon('terran', u)).not.toBeNull())
  })
  it('returns null for unknown unit', () => {
    expect(unitIcon('terran', 'stormtrooper')).toBeNull()
  })
})
```

Add analogous blocks for Zerg and Protoss with their canonical unit lists.

- [ ] **Step 4: Run tests, typecheck**

```bash
cd src/ReactClient && npx vitest run test/components/unit-icons.test.tsx && npx tsc --noEmit
```

- [ ] **Step 5: Commit**

```bash
git add src/ReactClient/src/components/icons/units/ src/ReactClient/test/components/unit-icons.test.tsx
git commit -m "feat(ui): unit silhouette SVG kit (Terran, Zerg, Protoss)"
```

### Task 20: Building silhouettes — per race (~18 components)

**Files:** `src/ReactClient/src/components/icons/buildings/{terran,zerg,protoss}/*Icon.tsx` + `index.ts`

Expected per-race buildings:
- Terran: CommandCenter, Barracks, Factory, Starport, Academy, Armory, ScienceFacility
- Zerg: Hatchery (Hive), SpawningPool, HydraliskDen, EvolutionChamber, UltraliskCavern, GreaterSpire
- Protoss: Nexus, Gateway, Forge, CyberneticsCore, RoboticsFacility, Stargate, Observatory, TemplarArchives

- [ ] **Step 1: Generate components**

Same pattern as Task 19 but ~30–40 lines of JSX each. Example Barracks:

```tsx
export function BarracksIcon(p: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 32 32" width={28} height={28} fill="none" {...p}>
      <path d="M4 26 L4 12 L10 8 L22 8 L28 12 L28 26 Z" stroke="currentColor" strokeWidth="1.2" fill="var(--color-race-emblem-fill)" />
      <path d="M4 20 L28 20" stroke="var(--color-race-secondary)" strokeWidth="1" />
      <rect x="13" y="22" width="6" height="4" stroke="currentColor" strokeWidth="0.8" />
    </svg>
  )
}
```

- [ ] **Step 2: `index.ts` + `buildingIcon(race, assetId)` lookup function — same shape as `unitIcon`**

- [ ] **Step 3: Smoke tests for all three races**

- [ ] **Step 4: Commit**

```bash
git add src/ReactClient/src/components/icons/buildings/ src/ReactClient/test/components/building-icons.test.tsx
git commit -m "feat(ui): building silhouette SVG kit (Terran, Zerg, Protoss)"
```

### Task 21: Top-level icons index

**Files:** Create `src/ReactClient/src/components/icons/index.ts`

- [ ] **Step 1: Re-export everything**

```typescript
export * from './emblems'
export * from './nav'
export * from './actions'
export * from './resources'
export { unitIcon, Terran as TerranUnits, Zerg as ZergUnits, Protoss as ProtossUnits } from './units'
export { buildingIcon, Terran as TerranBuildings, Zerg as ZergBuildings, Protoss as ProtossBuildings } from './buildings'
```

- [ ] **Step 2: Build**

```bash
cd src/ReactClient && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add src/ReactClient/src/components/icons/index.ts
git commit -m "feat(ui): consolidate icon kit exports"
```

### Task 22: Open PR for Phase 2

- [ ] **Step 1: Push and PR**

```bash
git push -u origin feat/sco-flair-phase2-icons
gh pr create --title "feat(ui): SCO flair — Phase 2 SVG icon kit" --body "$(cat <<'EOF'
## Summary

Adds the complete custom SVG icon kit required by Phase 3 page redesigns.

- 12 nav icons (replace Lucide where appropriate)
- 5 action icons (attack/defend/recall/split/merge)
- 6 resource icons (minerals/gas/land, small + large)
- ~30 unit silhouettes across three races
- ~18 building silhouettes across three races
- `unitIcon(race, id)` and `buildingIcon(race, id)` lookup helpers

No page consumes them yet. All icons use `stroke="currentColor"` or the race CSS custom properties so they theme automatically.

## Test plan

- [ ] Unit tests pass (smoke render assertions for every icon)
- [ ] `npm run build` passes, no unused-export warnings
- [ ] Visual inspection in a sandbox page (optional Storybook-style quick review)
EOF
)"
```

- [ ] **Step 2: Remove worktree**

```bash
cd /home/chris/repos/my/bge
git worktree remove /tmp/bge-work/sco-flair-phase2
```

---

## Phase 3 — Page patterns

**Outcome:** One PR per page. Each redesigns the body of one page to match the mockups from brainstorming. The shell (Phase 1) and icons (Phase 2) are consumed here. After Phase 3 merges, every primary page visually aligns with the spec.

Each task in Phase 3 uses this shape:
1. New branch and worktree
2. Rewrite the page
3. Verify manually (dev server)
4. Update any tests that assert old structure
5. PR + worktree removal

### Task 23: Base page refactor

**Files:** Modify `src/ReactClient/src/pages/Base.tsx` (~600 lines currently; the refactor both reduces and restructures).

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-base -b feat/sco-flair-phase3-base master
```

- [ ] **Step 2: Rewrite**

Replace `Base.tsx` with a version that:

1. Drops `<Card>` wrappers everywhere; uses `<section>` + a label.
2. Moves the 3-stat overview out of `<Card>` containers — renders as 3 columns of `label + big mono number + hint` with no borders, separated from the next section by a `border-b`.
3. Renders `grouped.built + grouped.queued + grouped.ready + grouped.locked` inside one `DataGrid` with 3 columns, not four separate groups in separate cards.
4. Uses `buildingIcon(race, asset.definition.id)` when available, falling back to a default hex SVG.
5. The BuildQueue becomes an inline mono list, not a bordered card.
6. Resource history chart stays, but its wrapping `<Section boxed>` becomes `<section>` with a label header only.
7. Recent activity is a plain inline list, default shown = 3 rows, "show more" link expands inline. No accordion button.

Key structure skeleton:

```tsx
return (
  <div className="space-y-6">
    <PageHeader title="Base" />

    {isFinished && (
      <div className="rounded border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
        Game ended. <a className="underline" href={`/games/${gameId}/results`}>View results</a>
      </div>
    )}

    {lastError && <div className="text-destructive text-sm">{lastError}</div>}

    {/* Overview row — no cards */}
    <section className="grid grid-cols-1 sm:grid-cols-3 gap-6 border-b border-border pb-4">
      <OverviewStat label="Workers" value={formatNumber(totalWorkers)} hint={idleHint} onAction={() => setWorkersOpen(true)} actionLabel="assign" />
      <OverviewStat label="Land" value={formatNumber(land)} tone="resource" hint={landHint} onAction={() => setColonizeOpen(true)} actionLabel="colonize" />
      <OverviewStat label="Build Queue" value={queueCount === 0 ? '—' : formatNumber(queueCount)} hint={queueHint} />
    </section>

    {/* Buildings grid */}
    <section>
      <SectionHeader label="Buildings" meta={`${buildingsBuilt} / ${buildingsTotal}`} />
      <DataGrid cols={3}>
        {allAssetsSorted.map((a) => (
          <BuildingCell key={a.definition.id} asset={a} race={race} ... />
        ))}
      </DataGrid>
    </section>

    <section>
      <SectionHeader label="Build Queue" meta={`${queueCount} items`} />
      <QueueList entries={queueData?.entries ?? []} onCancel={cancelQueueMutation.mutate} />
    </section>

    <section>
      <SectionHeader label="Resource history" />
      <Suspense fallback={<SkeletonLine />}>
        <ResourceHistoryChart gameId={gameId} />
      </Suspense>
    </section>

    <section>
      <SectionHeader label="Recent" />
      <RecentLog notifications={notifications ?? []} onClear={clearNotificationsMutation.mutate} />
    </section>

    <TrainUnitsDialog ... />
    <WorkerAssignmentDialog ... />
    <ColonizeDialog ... />
  </div>
)
```

Implement the helper components (`OverviewStat`, `SectionHeader`, `BuildingCell`, `QueueList`, `RecentLog`) inline in `Base.tsx` OR extract them into `src/ReactClient/src/components/base/` (preferred if they're larger than ~40 lines each).

`SectionHeader` pattern:

```tsx
function SectionHeader({ label, meta, action }: { label: string; meta?: ReactNode; action?: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between border-b border-border mb-3 pb-1.5">
      <h2 className="label">{label}</h2>
      {meta && <div className="text-xs text-muted-foreground mono">{meta}</div>}
      {action}
    </div>
  )
}
```

`BuildingCell` pattern (~50 lines):

```tsx
function BuildingCell({ asset, race, state, onBuild, onQueue, onTrainUnits, onCancelQueue, queueEntryId }: {
  asset: AssetViewModel
  race: Race | null
  state: AssetState
  onBuild: (defId: string) => void
  onQueue: (defId: string) => void
  onTrainUnits: (asset: AssetViewModel) => void
  onCancelQueue: (entryId: string) => void
  queueEntryId?: string
}) {
  const Icon = buildingIcon(race ?? 'terran', asset.definition.id)
  return (
    <DataGridCell className={cn('flex items-start gap-3', state === 'locked' && 'opacity-55', state === 'queued' && 'bg-warning/5')}>
      <div className="w-10 h-10 flex items-center justify-center bg-background/60 border border-border shrink-0">
        {Icon ? <Icon width={32} height={32} style={{ color: 'var(--color-race-primary)' }} /> : <span className="text-muted-foreground text-xs">◆</span>}
      </div>
      <div className="min-w-0 flex-1">
        <div className="text-sm font-semibold flex items-center gap-1.5">
          {asset.definition.name}
          {state === 'built' && <span className="text-resource text-xs">●</span>}
        </div>
        <div className="text-[11px] text-muted-foreground mono">
          {state === 'built' && (asset.availableUnits.length > 0 ? `Trains ${asset.availableUnits.slice(0, 2).map(u => u.name).join(' · ')}` : 'Ready')}
          {state === 'queued' && `Building · ${asset.ticksLeftForBuild}r`}
          {state === 'ready' && `${asset.cost.minerals ?? 0} min · ${asset.cost.gas ?? 0} gas`}
          {state === 'locked' && (asset.prerequisites ? `Requires ${asset.prerequisites}` : 'Locked')}
        </div>
        {state === 'built' && asset.availableUnits.length > 0 && (
          <button className="text-[11px] text-interactive hover:underline mt-1" onClick={() => onTrainUnits(asset)}>Train ›</button>
        )}
        {state === 'queued' && queueEntryId && (
          <button className="text-[11px] text-muted-foreground hover:underline mt-1" onClick={() => onCancelQueue(queueEntryId)}>cancel</button>
        )}
        {state === 'ready' && (
          <div className="flex gap-3 mt-1">
            <button className="text-[11px] text-interactive hover:underline disabled:opacity-50" disabled={!asset.canAfford} onClick={() => onBuild(asset.definition.id)}>Build</button>
            <button className="text-[11px] text-interactive hover:underline" onClick={() => onQueue(asset.definition.id)}>Queue</button>
          </div>
        )}
      </div>
    </DataGridCell>
  )
}
```

Remove:
- `Card`, `CardContent` imports (not used on this page)
- `Section` (replaced by local `<section>` + `SectionHeader`)
- `AssetGroup` inner helper — replaced by single DataGrid of all assets sorted by state

- [ ] **Step 3: Verify manually**

```bash
cd src/ReactClient && npm run dev
```

Log in, open Base. Confirm:
- No rounded borders around sections
- Buildings grid is a single connected grid with 1px dividers
- `TrainUnitsDialog`, `WorkerAssignmentDialog`, `ColonizeDialog` all still open from their controls
- Build queue inline mono list works, cancel removes entries
- Resource chart still renders
- Recent section shows 3 rows + "show more"

- [ ] **Step 4: Run tests**

```bash
cd src/ReactClient && npx vitest run
```

If any test selects by old text or Card classes, update.

- [ ] **Step 5: Commit, push, PR, cleanup**

```bash
git add src/ReactClient/src/pages/Base.tsx src/ReactClient/src/components/base/ 2>/dev/null || true
git commit -m "feat(ui): Base page — SCO flair refactor (no cards, DataGrid buildings, inline queue+recent)"
git push -u origin feat/sco-flair-phase3-base
gh pr create --title "feat(ui): SCO flair — Phase 3a Base page" --body "Refactors Base to use Section/DataGrid primitives, race-tinted building icons, inline build queue + recent log. Visual regression screenshots attached."
cd /home/chris/repos/my/bge && git worktree remove /tmp/bge-work/sco-flair-phase3-base
```

### Task 24: Units page refactor

**Files:** Modify `src/ReactClient/src/pages/Units.tsx`.

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-units -b feat/sco-flair-phase3-units master
```

- [ ] **Step 2: Rewrite**

Key changes:

1. Home units rendered inside a `<DataGrid cols={2}>` of `UnitStackCell` components (sprite + name + stats with gold upgrade bonuses + large mono count + inline actions).
2. Deployed units rendered as vertically stacked tinted blocks (`bg-destructive/5 border border-destructive/30 p-3`).
3. Merge/Split/Attack actions as text links, not filled buttons.

Template for `UnitStackCell`:

```tsx
function UnitStackCell({ stack, race, onAttack, onSplit }: {...}) {
  const Icon = unitIcon(race ?? 'terran', stack.unitId)
  return (
    <DataGridCell className="grid grid-cols-[40px_1fr_auto] gap-3 items-center">
      <div className="w-10 h-10 flex items-center justify-center bg-background/60 border border-border">
        {Icon ? <Icon width={32} height={32} style={{ color: 'var(--color-race-primary)' }} /> : null}
      </div>
      <div>
        <div className="text-sm font-semibold">{stack.unitName}</div>
        <div className="text-[11px] text-muted-foreground mono">
          atk {stack.attack} {stack.attackBonus > 0 && <span className="text-prestige">+{stack.attackBonus}</span>}
          {' · '}def {stack.defense} {stack.defenseBonus > 0 && <span className="text-prestige">+{stack.defenseBonus}</span>}
          {' · '}hp {stack.hp}
        </div>
        <div className="text-[11px] mt-1 flex gap-3">
          <button className="text-interactive hover:underline" onClick={() => onAttack(stack)}>Attack</button>
          <button className="text-interactive hover:underline" onClick={() => onSplit(stack)}>Split</button>
        </div>
      </div>
      <div className="mono text-[22px] font-bold text-right">{stack.count}</div>
    </DataGridCell>
  )
}
```

Deployed block:

```tsx
function DeployedSquad({ squad }: { squad: DeployedSquadViewModel }) {
  return (
    <div className="bg-destructive/5 border border-destructive/30 p-3 space-y-1">
      <div className="mono text-xs text-[color:var(--color-enemy)]">→ {squad.targetPlayerName}</div>
      <div className="mono text-[11px]">{squad.units.map(u => `${u.count} ${u.name}`).join(' · ')}</div>
      <div className="text-[11px] text-prestige">
        {squad.returning ? `Returning in ${squad.ticksLeft} rounds` : `Arrival in ${squad.ticksLeft} round${squad.ticksLeft === 1 ? '' : 's'}`}
        {squad.canRecall && <button className="ml-3 text-interactive hover:underline">recall</button>}
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Verify, test, commit, PR, cleanup** (same shape as Task 23)

### Task 25: Ranking page — tier typography, race colors, alliance tab merge

**Files:**
- Modify: `src/ReactClient/src/pages/PlayerRanking.tsx`
- Modify: `src/ReactClient/src/pages/AllianceRanking.tsx` (export a `AllianceRankingContent` component)
- Modify: `src/ReactClient/src/App.tsx` (drop the `/allianceranking` route OR leave it as a deep link into the Ranking tab)

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-ranking -b feat/sco-flair-phase3-ranking master
```

- [ ] **Step 2: Extract `AllianceRankingContent` from AllianceRanking.tsx**

Move the JSX body (minus the page shell) into an exported function component. The page `export function AllianceRanking()` becomes a thin wrapper that renders `<div><h1>Alliance Ranking</h1><AllianceRankingContent gameId={gameId}/></div>`.

- [ ] **Step 3: Rewrite PlayerRanking with tabs**

```tsx
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { AllianceRankingContent } from './AllianceRanking'
// ...

export function PlayerRanking({ gameId }: PlayerRankingProps) {
  // existing queries and state
  return (
    <div className="space-y-4">
      <PageHeader title="Standings" description={`Round ${round}/${totalRounds} · ${total} commanders`} />
      <Tabs defaultValue="players">
        <TabsList>
          <TabsTrigger value="players">Commanders</TabsTrigger>
          <TabsTrigger value="alliances">Alliances</TabsTrigger>
        </TabsList>
        <TabsContent value="players">
          <RankingTable entries={filtered} filter={filter} setFilter={setFilter} />
        </TabsContent>
        <TabsContent value="alliances">
          <AllianceRankingContent gameId={gameId} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
```

`RankingTable` renders rows per tier. Tier helper:

```tsx
function rankTier(rank: number): 'r1' | 'r2-5' | 'r6-10' | 'r11-20' | 'r21' {
  if (rank === 1) return 'r1'
  if (rank <= 5) return 'r2-5'
  if (rank <= 10) return 'r6-10'
  if (rank <= 20) return 'r11-20'
  return 'r21'
}

const TIER_CLASSES: Record<ReturnType<typeof rankTier>, string> = {
  'r1':     'py-2 text-[32px] font-extrabold tracking-tight',
  'r2-5':   'py-1 text-[18px] font-bold',
  'r6-10':  'py-1 text-[15px] font-semibold',
  'r11-20': 'py-0.5 text-[13px] font-medium',
  'r21':    'py-0.5 text-[11.5px] font-normal text-muted-foreground',
}

const RACE_CLASSES: Record<string, string> = {
  terran:  'text-[color:var(--color-race-primary)]', // slate-ish when terran
  zerg:    'text-[#B45309]',
  protoss: 'text-[#FBBF24]',
}

function RankRow({ p, rank }: { p: PublicPlayerViewModel; rank: number }) {
  const tier = rankTier(rank)
  const race = (p as any).playerType?.toLowerCase?.() ?? 'terran' // Fallback if missing
  return (
    <div
      className={cn(
        'grid grid-cols-[48px_24px_1fr_90px_110px] items-baseline px-2 hover:bg-white/5 border-l-2 border-transparent transition-colors',
        TIER_CLASSES[tier],
        p.isCurrentPlayer && 'bg-resource/10 border-l-resource',
      )}
    >
      <span className="mono text-muted-foreground text-right pr-3">{rank}</span>
      <span className={cn('w-2 h-2 rounded-full mx-auto', p.isOnline ? 'bg-resource shadow-[0_0_6px_var(--color-resource)]' : 'bg-muted-foreground/50')} />
      <span className="truncate">
        <Link to={`/games/${gameId}/player/${p.playerId}`} className={cn('font-semibold', p.isCurrentPlayer ? 'text-resource' : RACE_CLASSES[race] ?? '')}>
          {p.playerName}
        </Link>
        <span className="text-[10px] text-muted-foreground ml-2 font-normal capitalize">{race}</span>
      </span>
      <span className="mono text-right text-interactive">{Math.floor(p.land).toLocaleString()}</span>
      <span className="mono text-right text-muted-foreground text-xs">{p.approxHomeUnitCount ?? '?'} units</span>
    </div>
  )
}
```

Note: `playerType` must be included in `PublicPlayerViewModel` for race coloring. Check `api/types.ts:420-432`. If not present, **update the backend** in this task: `PublicPlayerViewModel.cs` + the controller that serializes it. Same pattern as Task 2.

- [ ] **Step 4: Add a special marker for rank 21+ "n00b" preserved from original**

Optional Easter egg the original had: rank 21+ shows the text "n00b" as a tiny tag. Implement only if it's faithful; otherwise skip.

- [ ] **Step 5: Verify, test, commit, PR, cleanup**

### Task 26: Research page restyle

**Files:** Modify `src/ReactClient/src/pages/Research.tsx`.

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-research -b feat/sco-flair-phase3-research master
```

- [ ] **Step 2: Rewrite**

- Render Attack and Defense as two horizontal tracks.
- Each row has a left-aligned uppercase label, node dots connected by a horizontal rule, current level highlighted with a ring, levels above in gold.
- In-progress node: amber `animate-pulse` dot.
- CTA is a text link (`className="text-interactive hover:underline"`), not a filled button.

The component structure (replaces current card-based version):

```tsx
function ResearchTrack({ label, levels, currentLevel, inProgressLevel, onResearch, canAfford, cost }: {
  label: string
  levels: number[]
  currentLevel: number
  inProgressLevel: number | null
  onResearch: () => void
  canAfford: boolean
  cost: CostViewModel
}) {
  return (
    <div className="flex items-center gap-4 py-4 border-b border-border">
      <div className="label w-28 shrink-0" style={{ color: 'var(--color-prestige)' }}>{label}</div>
      <div className="flex-1 flex items-center gap-3 relative">
        <div className="absolute left-0 right-0 top-1/2 h-px bg-border -z-10" />
        {levels.map((lvl) => {
          const done = lvl <= currentLevel
          const inProgress = inProgressLevel === lvl
          return (
            <div key={lvl} className={cn(
              'w-8 h-8 rounded-full flex items-center justify-center mono text-xs bg-background border',
              done ? 'border-[color:var(--color-prestige)] text-[color:var(--color-prestige)] font-semibold' : 'border-border text-muted-foreground',
              inProgress && 'animate-pulse border-[color:var(--color-prestige)]',
            )}>
              {lvl}
            </div>
          )
        })}
      </div>
      <div className="text-xs text-muted-foreground mono shrink-0">
        {inProgressLevel != null
          ? <span className="text-prestige">researching…</span>
          : <button className={cn('text-interactive hover:underline', !canAfford && 'opacity-50')} onClick={onResearch} disabled={!canAfford}>
              Research · {cost.minerals ?? 0}m / {cost.gas ?? 0}g
            </button>}
      </div>
    </div>
  )
}
```

Keep the existing `UpgradeTrack` component file but replace its body with the new render. Don't refactor its data flow — same props surface.

- [ ] **Step 3: Verify, test, commit, PR, cleanup**

### Task 27: Market + Trade merge (Market page gets a "Direct" tab)

**Files:**
- Modify: `src/ReactClient/src/pages/Market.tsx`
- Modify: `src/ReactClient/src/pages/Trade.tsx` (export content component; optionally keep the route for deep links)

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-market -b feat/sco-flair-phase3-market master
```

- [ ] **Step 2: Extract `TradeContent` from Trade.tsx**

Move the JSX body out of `export function Trade()` into an exported `TradeContent({ gameId })` component.

- [ ] **Step 3: Add tabs to Market**

```tsx
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { TradeContent } from './Trade'

export function Market({ gameId }: MarketProps) {
  return (
    <div className="space-y-4">
      <PageHeader title="Market" />
      <Tabs defaultValue="orders">
        <TabsList>
          <TabsTrigger value="orders">Open orders</TabsTrigger>
          <TabsTrigger value="direct">Direct trade</TabsTrigger>
        </TabsList>
        <TabsContent value="orders">
          <MarketOrdersContent gameId={gameId} />
        </TabsContent>
        <TabsContent value="direct">
          <TradeContent gameId={gameId} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
```

Keep `/games/:gameId/trade` route as an alias that renders `<Market defaultTab="direct" />` (or just redirects to `/market?tab=direct`) to avoid breaking any bookmarks or links in chat history.

- [ ] **Step 4: Verify, test, commit, PR, cleanup**

### Task 28: Chat — race-colored names

**Files:** Modify `src/ReactClient/src/pages/Chat.tsx`.

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase3-chat -b feat/sco-flair-phase3-chat master
```

- [ ] **Step 2: Rewrite the name render**

Replace the `raceIcon` emoji prefix with race-colored name text:

```tsx
function RaceName({ name, race }: { name: string; race: string }) {
  const cls = race === 'zerg' ? 'text-[#B45309]' : race === 'protoss' ? 'text-[#FBBF24]' : 'text-[color:var(--color-race-primary)]'
  return <span className={cn('font-semibold', cls)}>{name}</span>
}
```

Drop the emoji `raceIcon` function entirely — leaves cleaner visual design.

- [ ] **Step 3: Verify, test, commit, PR, cleanup**

---

## Phase 4 — Login / JoinGame polish

**Outcome:** Login has a proper SCO-style splash; JoinGame shows large race-emblem tiles for race selection.

### Task 29: Login splash composition

**Files:** Modify `src/ReactClient/src/pages/SignIn.tsx`.

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase4-signin -b feat/sco-flair-phase4-signin master
```

- [ ] **Step 2: Add a full-bleed CSS composition behind the form**

Wrap the existing sign-in card in a `bg-starfield min-h-screen relative` div. Add `::before` and `::after` pseudo-elements in a `<style>` block or via inline Tailwind arbitrary values for:

- A radial gradient "planet" bottom-right (30vw dark desaturated blue, soft edge).
- A subtle horizontal gradient band top (cool light grade).

```tsx
export function SignIn() {
  return (
    <div className="min-h-screen bg-starfield relative flex items-center justify-center p-4">
      <div
        aria-hidden
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'radial-gradient(circle at 85% 80%, rgba(56,90,140,0.35) 0%, transparent 45%), linear-gradient(180deg, rgba(14,116,144,0.05) 0%, transparent 30%)',
        }}
      />
      {/* existing form markup */}
    </div>
  )
}
```

- [ ] **Step 3: Verify, commit, PR, cleanup**

### Task 30: JoinGame — large race-emblem tiles

**Files:** Modify `src/ReactClient/src/pages/JoinGame.tsx`.

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase4-join -b feat/sco-flair-phase4-join master
```

- [ ] **Step 2: Replace the race dropdown with a 3-tile picker**

```tsx
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'

const EMBLEMS = { terran: TerranEmblem, zerg: ZergEmblem, protoss: ProtossEmblem } as const
const ACCENT_STYLE = {
  terran:  { '--color-race-primary': '#CBD5E1', '--color-race-secondary': '#F59E0B', '--color-race-emblem-fill': '#1f2937' },
  zerg:    { '--color-race-primary': '#B45309', '--color-race-secondary': '#ea580c', '--color-race-emblem-fill': '#3a1a08' },
  protoss: { '--color-race-primary': '#FBBF24', '--color-race-secondary': '#0e7490', '--color-race-emblem-fill': '#0c1e2e' },
} as const

function RaceTile({ id, name, selected, onSelect }: {...}) {
  const Emblem = EMBLEMS[id as keyof typeof EMBLEMS] ?? TerranEmblem
  return (
    <button
      type="button"
      onClick={() => onSelect(id)}
      style={ACCENT_STYLE[id as keyof typeof ACCENT_STYLE] as React.CSSProperties}
      className={cn(
        'flex flex-col items-center gap-2 p-4 border-2 transition-colors min-h-[180px] w-[160px]',
        selected ? 'border-[color:var(--color-race-secondary)] bg-[color:var(--color-race-primary)]/5' : 'border-border hover:border-[color:var(--color-race-primary)]/60',
      )}
    >
      <Emblem width={80} height={80} style={{ color: 'var(--color-race-primary)' }} />
      <div className="font-semibold text-sm">{name}</div>
    </button>
  )
}
```

Replace the existing `<select>` with a `<div className="flex gap-3">` of three `RaceTile` components.

- [ ] **Step 3: Verify, commit, PR, cleanup**

---

## Phase 5 — Cleanup

**Outcome:** Dead code removed. Codebase is consistent post-redesign.

### Task 31: Delete ThemeContext

**Files:**
- Delete: `src/ReactClient/src/contexts/ThemeContext.tsx`

- [ ] **Step 1: Worktree**

```bash
git worktree add /tmp/bge-work/sco-flair-phase5-cleanup -b feat/sco-flair-phase5-cleanup master
```

- [ ] **Step 2: Confirm no imports remain**

```bash
cd src/ReactClient
grep -rn "ThemeContext\|useTheme\|ThemeProvider" src/ || echo "clean"
```

Expected: only `ThemeContext.tsx` file itself.

- [ ] **Step 3: Delete the file**

```bash
git rm src/ReactClient/src/contexts/ThemeContext.tsx
```

- [ ] **Step 4: Typecheck, build**

```bash
cd src/ReactClient && npx tsc --noEmit && npm run build
```

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor(client): delete ThemeContext (dark-only UI)"
```

### Task 32: Audit unused Lucide imports

**Files:** Multiple — wherever an icon was replaced by a custom SVG in Phase 3.

- [ ] **Step 1: Find unused Lucide imports**

```bash
cd src/ReactClient
npx tsc --noEmit 2>&1 | grep -i "declared but" | head -50 || true
```

If none, check manually — iterate the files modified in Phase 3 and look for Lucide imports that are no longer referenced.

- [ ] **Step 2: Clean each file**

Remove the unused imports, re-run typecheck.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore(client): drop unused Lucide imports after icon kit replacement"
```

### Task 33: Drop BuildQueue standalone wrapper

**Files:** Modify `src/ReactClient/src/components/BuildQueue.tsx` if it still renders its own `<Card>` wrapper — Phase 3 Base moved the queue inline, so any standalone consumer should be updated.

- [ ] **Step 1: Inspect usage**

```bash
grep -rn "BuildQueue" src/ReactClient/src --include="*.tsx"
```

- [ ] **Step 2: If still used standalone, wrap in `<section>` with `<SectionHeader>` like other places**

- [ ] **Step 3: Commit**

```bash
git commit -am "refactor(ui): BuildQueue uses section pattern, no standalone card"
```

### Task 34: Open cleanup PR

- [ ] **Step 1: Push and PR**

```bash
git push -u origin feat/sco-flair-phase5-cleanup
gh pr create --title "chore(ui): SCO flair — Phase 5 cleanup" --body "Deletes ThemeContext, drops unused Lucide imports, removes residual Card wrappers."
cd /home/chris/repos/my/bge && git worktree remove /tmp/bge-work/sco-flair-phase5-cleanup
```

---

## Done criteria

- [ ] All five phases merged to master.
- [ ] `grep -c "rounded-lg border bg-card" src/ReactClient/src/pages` → ≤ 2 (modals only, if any).
- [ ] Nav has 6 primary items + utility footer; no sidebar section headers ("PLAY", "INFO", etc.).
- [ ] `html[data-race="..."]` toggles correctly when switching players between games.
- [ ] Ranking page rank 1 renders at 32px bold; rank 21+ at 11.5px muted.
- [ ] Resource numbers render green with glow; land renders blue; rank 1 and research upgrades render gold.
- [ ] No `.light` class, no `ThemeContext` file, no `.bg-grid` utility.
- [ ] Unit tests pass; Playwright tests pass.
