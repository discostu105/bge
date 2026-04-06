import { useLocation } from 'react-router'

const links = [
  { path: '/admin/games', label: 'Games' },
  { path: '/admin/players', label: 'Players' },
  { path: '/admin/ticks', label: 'Tick Control' },
  { path: '/admin/metrics', label: 'Metrics' },
  { path: '/admin/audit', label: 'Audit Log' },
  { path: '/admin/reports', label: 'Reports' },
]

export function AdminNav() {
  const location = useLocation()
  return (
    <nav className="flex gap-1 mb-6 border-b border-border pb-2 flex-wrap">
      {links.map(({ path, label }) => (
        <a
          key={path}
          href={path}
          className={`px-3 py-1.5 rounded text-sm ${
            location.pathname === path
              ? 'bg-primary text-primary-foreground'
              : 'hover:bg-muted text-muted-foreground'
          }`}
        >
          {label}
        </a>
      ))}
    </nav>
  )
}
