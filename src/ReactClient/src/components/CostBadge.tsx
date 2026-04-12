import { type ReactNode } from 'react'
import { GemIcon, FlaskConicalIcon, CircleIcon } from 'lucide-react'
import type { CostViewModel } from '@/api/types'

interface CostBadgeProps {
  cost: CostViewModel | Record<string, number>
}

function resourceIcon(resourceId: string): ReactNode {
  const cls = 'h-3 w-3'
  switch (resourceId) {
    case 'minerals':
      return <GemIcon className={`${cls} text-blue-400`} aria-hidden="true" />
    case 'gas':
      return <FlaskConicalIcon className={`${cls} text-green-400`} aria-hidden="true" />
    default:
      return <CircleIcon className={`${cls} text-muted-foreground`} aria-hidden="true" />
  }
}

function getCostEntries(cost: CostViewModel | Record<string, number>): [string, number][] {
  if ('cost' in cost && typeof cost.cost === 'object') {
    return Object.entries(cost.cost)
  }
  return Object.entries(cost as Record<string, number>)
}

export function CostBadge({ cost }: CostBadgeProps) {
  const entries = getCostEntries(cost)

  if (entries.length === 0) {
    return <span className="text-muted-foreground text-sm">Free</span>
  }

  return (
    <span className="inline-flex gap-2 flex-wrap">
      {entries.map(([resourceId, amount]) => (
        <span
          key={resourceId}
          className="inline-flex items-center gap-1 rounded bg-secondary px-1.5 py-0.5 text-xs font-medium text-secondary-foreground"
        >
          {resourceIcon(resourceId)}
          <span>{amount.toLocaleString()}</span>
          <span className="text-muted-foreground capitalize">{resourceId}</span>
        </span>
      ))}
    </span>
  )
}
