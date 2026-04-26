import { useQuery } from '@tanstack/react-query'
import { useLocation, useNavigate } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel } from '@/api/types'
import { rememberAdminGameId } from './useAdminGameId'

interface NavItem {
  path: string
  label: string
}

const globalLinks: NavItem[] = [
  { path: '/admin/games', label: 'Games' },
  { path: '/admin/audit', label: 'Audit Log' },
  { path: '/admin/reports', label: 'Reports' },
]

const perGameLinks: NavItem[] = [
  { path: '/admin/players', label: 'Players' },
  { path: '/admin/ticks', label: 'Tick Control' },
  { path: '/admin/stats', label: 'Stats' },
  { path: '/admin/metrics', label: 'Metrics' },
]

function isActive(pathname: string, base: string): boolean {
  return pathname === base || pathname.startsWith(`${base}/`)
}

interface AdminNavProps {
  currentGameId?: string | null
}

export function AdminNav({ currentGameId = null }: AdminNavProps) {
  const location = useLocation()
  const navigate = useNavigate()

  const { data } = useQuery<GameListViewModel>({
    queryKey: ['admin-games-nav'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
  })
  const games = data?.games ?? []
  const currentGame = currentGameId ? games.find((g) => g.gameId === currentGameId) : null

  // If we're on a per-game admin page, derive the section so the selector can
  // jump to the same section under the newly selected game.
  const currentSection = perGameLinks.find((l) => isActive(location.pathname, l.path))

  return (
    <nav className="mb-6 border-b border-border pb-3">
      <div className="text-xs uppercase tracking-wide text-muted-foreground mb-1">Admin</div>

      {/* Global section */}
      <div className="flex gap-1 flex-wrap mb-3">
        {globalLinks.map(({ path, label }) => (
          <a
            key={path}
            href={path}
            className={`px-3 py-1.5 rounded text-sm ${
              isActive(location.pathname, path)
                ? 'bg-primary text-primary-foreground'
                : 'hover:bg-muted text-muted-foreground'
            }`}
          >
            {label}
          </a>
        ))}
      </div>

      {/* Per-game section */}
      <div className="flex items-center gap-3 flex-wrap">
        <label className="text-sm flex items-center gap-2">
          <span className="text-muted-foreground">Game:</span>
          <select
            className="rounded border border-input bg-background px-2 py-1 text-sm min-w-[12rem]"
            value={currentGameId ?? ''}
            onChange={(e) => {
              const id = e.target.value
              if (!id) return
              rememberAdminGameId(id)
              const dest = currentSection?.path ?? '/admin/players'
              navigate(`${dest}/${id}`)
            }}
          >
            {!currentGame && <option value="">— select a game —</option>}
            {games.map((g) => (
              <option key={g.gameId} value={g.gameId}>
                {g.name} ({g.status})
              </option>
            ))}
          </select>
        </label>

        <div className="flex gap-1 flex-wrap">
          {perGameLinks.map(({ path, label }) => {
            const active = isActive(location.pathname, path)
            const href = currentGameId ? `${path}/${currentGameId}` : path
            return (
              <a
                key={path}
                href={href}
                className={`px-3 py-1.5 rounded text-sm ${
                  active
                    ? 'bg-primary text-primary-foreground'
                    : currentGameId
                    ? 'hover:bg-muted text-muted-foreground'
                    : 'text-muted-foreground/50 hover:bg-muted/50'
                }`}
                title={currentGameId ? undefined : 'Select a game first'}
              >
                {label}
              </a>
            )
          })}
        </div>
      </div>
    </nav>
  )
}
