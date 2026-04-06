export function formatDuration(ms: number): string {
  const totalSec = Math.floor(ms / 1000)
  const days = Math.floor(totalSec / 86400)
  const hours = Math.floor((totalSec % 86400) / 3600)
  const mins = Math.floor((totalSec % 3600) / 60)
  if (days >= 1) return `${days}d ${hours}h`
  if (hours >= 1) return `${hours}h ${mins}m`
  return `${mins}m`
}

export function formatDurationFromDates(startIso: string, endIso: string): string {
  const ms = new Date(endIso).getTime() - new Date(startIso).getTime()
  if (ms <= 0) return '—'
  return formatDuration(ms)
}

export function formatNumber(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
  if (value >= 10_000) return `${(value / 1_000).toFixed(1)}K`
  return Math.floor(value).toLocaleString()
}

export function formatDate(d: string): string {
  return new Date(d).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function formatDateTime(d: string | null | undefined): string {
  return d ? new Date(d).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : 'TBD'
}

export function rankBadgeClass(rank: number, players?: number): string {
  if (rank === 1) return 'bg-warning text-black'
  if (rank === 2) return 'bg-muted-foreground text-black'
  if (rank === 3) return 'bg-warning/60 text-white'
  if (players != null && rank > players / 2) return 'bg-secondary text-secondary-foreground opacity-60'
  return 'bg-secondary text-secondary-foreground'
}
