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
