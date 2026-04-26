import { useLocation } from 'react-router'

interface NavLink {
  path: string
  label: string
  perGame?: boolean
}

const links: NavLink[] = [
  { path: '/admin/games', label: 'Games' },
  { path: '/admin/players', label: 'Players', perGame: true },
  { path: '/admin/ticks', label: 'Tick Control', perGame: true },
  { path: '/admin/stats', label: 'Stats', perGame: true },
  { path: '/admin/metrics', label: 'Metrics' },
  { path: '/admin/audit', label: 'Audit Log' },
  { path: '/admin/reports', label: 'Reports' },
]

export function AdminNav({ currentGameId }: { currentGameId?: string | null } = {}) {
  const location = useLocation()
  return (
    <nav className="flex gap-1 mb-6 border-b border-border pb-2 flex-wrap">
      {links.map(({ path, label, perGame }) => {
        const href = perGame && currentGameId ? `${path}/${currentGameId}` : path
        const isActive = location.pathname === path || location.pathname.startsWith(`${path}/`)
        return (
          <a
            key={path}
            href={href}
            className={`px-3 py-1.5 rounded text-sm ${
              isActive
                ? 'bg-primary text-primary-foreground'
                : 'hover:bg-muted text-muted-foreground'
            }`}
          >
            {label}
          </a>
        )
      })}
    </nav>
  )
}
