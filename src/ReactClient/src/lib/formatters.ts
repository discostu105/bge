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

/** Compact one-decimal ratio used for land/worker readouts. */
export function formatRatio(value: number): string {
  if (!isFinite(value)) return '—'
  return value.toFixed(1)
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

export function formatTicks(ticks: number, tickDurationMs: number): string {
  const suffix = ticks === 1 ? 'tick' : 'ticks'
  if (ticks <= 0) return `0 ticks`
  const totalSec = Math.round((ticks * tickDurationMs) / 1000)
  let approx: string
  if (totalSec >= 3600) approx = `~${Math.round(totalSec / 3600)}h`
  else if (totalSec >= 120) approx = `~${Math.round(totalSec / 60)}m`
  else if (totalSec >= 60) {
    const m = Math.floor(totalSec / 60)
    const s = totalSec - m * 60
    approx = s === 0 ? `~${m}m` : `~${m}m ${s}s`
  } else approx = `~${totalSec}s`
  return `${ticks} ${suffix} (${approx})`
}
