import { cn } from '@/lib/utils'
import { CostBadge } from '@/components/CostBadge'
import type { CostViewModel } from '@/api/types'

interface AffordabilityChipProps {
  cost: CostViewModel | Record<string, number>
  affordable: boolean
  /** Stretch to fill its row; otherwise wraps its content. */
  block?: boolean
  className?: string
}

export function AffordabilityChip({ cost, affordable, block, className }: AffordabilityChipProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs',
        affordable
          ? 'border-success/40 bg-success/10 text-foreground'
          : 'border-destructive/40 bg-destructive/10 text-foreground',
        block && 'w-full justify-between',
        className,
      )}
      title={affordable ? 'Affordable' : 'Not enough resources'}
    >
      <CostBadge cost={cost} />
      {!affordable && <span className="label text-destructive">Short</span>}
    </span>
  )
}
