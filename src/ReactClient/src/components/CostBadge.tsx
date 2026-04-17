import { cn } from '@/lib/utils'
import { formatNumber } from '@/lib/formatters'
import { ResourceIcon } from '@/components/ui/resource-icon'
import type { CostViewModel } from '@/api/types'

interface CostBadgeProps {
  cost: CostViewModel | Record<string, number>
  /** Show text label next to each amount. Default false (icon + amount only). */
  showLabels?: boolean
  className?: string
}

function getCostEntries(cost: CostViewModel | Record<string, number>): [string, number][] {
  if ('cost' in cost && typeof cost.cost === 'object') {
    return Object.entries(cost.cost)
  }
  return Object.entries(cost as Record<string, number>)
}

export function CostBadge({ cost, showLabels, className }: CostBadgeProps) {
  const entries = getCostEntries(cost).filter(([, v]) => v > 0)

  if (entries.length === 0) {
    return <span className="text-muted-foreground">—</span>
  }

  return (
    <span className={cn('inline-flex items-center gap-2 text-sm', className)}>
      {entries.map(([name, value]) => (
        <span key={name} className="inline-flex items-center gap-1 mono">
          <ResourceIcon name={name} />
          {formatNumber(value)}
          {showLabels && <span className="text-muted-foreground capitalize ml-0.5">{name}</span>}
        </span>
      ))}
    </span>
  )
}
