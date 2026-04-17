import { cn } from '@/lib/utils'
import { formatTicks } from '@/lib/formatters'
import { useTickDuration } from '@/hooks/useTickDuration'

interface TicksProps {
  n: number
  className?: string
  /** Hide the approximate wall-clock suffix. */
  numberOnly?: boolean
}

export function Ticks({ n, className, numberOnly }: TicksProps) {
  const duration = useTickDuration()
  const full = formatTicks(n, duration)
  const label = numberOnly ? `${n} ${n === 1 ? 'tick' : 'ticks'}` : full
  return <span className={cn('mono', className)}>{label}</span>
}
