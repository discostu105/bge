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
      <span className="absolute inset-0 flex items-center justify-center mono text-[10px] font-semibold" style={{ color: 'var(--color-race-secondary)' }}>
        {formatMmSs(remaining)}
      </span>
    </div>
  )
}
