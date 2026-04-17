import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'

interface CountdownProps {
  to: string // ISO date
  className?: string
  /** Threshold in ms below which the text is tinted `danger`. Default 60s. */
  dangerBelowMs?: number
}

function format(ms: number): string {
  if (ms <= 0) return 'ended'
  const s = Math.floor(ms / 1000)
  if (s < 60) return `${s}s`
  const m = Math.floor(s / 60)
  const rs = s % 60
  if (m < 60) return rs === 0 ? `${m}m` : `${m}m ${rs}s`
  const h = Math.floor(m / 60)
  const rm = m % 60
  return rm === 0 ? `${h}h` : `${h}h ${rm}m`
}

export function Countdown({ to, className, dangerBelowMs = 60_000 }: CountdownProps) {
  const [now, setNow] = useState(() => Date.now())
  const target = new Date(to).getTime()
  const remaining = target - now
  const danger = remaining > 0 && remaining < dangerBelowMs

  useEffect(() => {
    if (remaining <= 0) return
    const interval = remaining < 600_000 ? 1000 : 30_000
    const id = setInterval(() => setNow(Date.now()), interval)
    return () => clearInterval(id)
  }, [remaining])

  useEffect(() => {
    const onVis = () => { if (!document.hidden) setNow(Date.now()) }
    document.addEventListener('visibilitychange', onVis)
    return () => document.removeEventListener('visibilitychange', onVis)
  }, [])

  return (
    <span className={cn('mono', danger && 'text-danger', className)}>
      {format(remaining)}
    </span>
  )
}
