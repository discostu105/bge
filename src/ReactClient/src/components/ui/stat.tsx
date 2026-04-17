import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface StatProps {
  label: ReactNode
  value: ReactNode
  /** Render a delta pill beside the value, e.g. "+42/t" or "-3". */
  delta?: ReactNode
  deltaTone?: 'success' | 'danger' | 'warning' | 'neutral'
  icon?: ReactNode
  tone?: 'default' | 'danger' | 'warning'
  className?: string
}

export function Stat({ label, value, delta, deltaTone = 'neutral', icon, tone = 'default', className }: StatProps) {
  return (
    <div className={cn('flex items-center gap-2 min-w-0', className)}>
      {icon && <span className="shrink-0">{icon}</span>}
      <div className="flex flex-col min-w-0">
        <span className="label leading-tight">{label}</span>
        <span className={cn(
          'mono text-sm font-semibold leading-tight truncate',
          tone === 'danger' && 'text-danger',
          tone === 'warning' && 'text-warning',
        )}>
          {value}
          {delta && (
            <span className={cn(
              'ml-1.5 text-xs font-medium',
              deltaTone === 'success' && 'text-success',
              deltaTone === 'danger' && 'text-danger',
              deltaTone === 'warning' && 'text-warning',
              deltaTone === 'neutral' && 'text-muted-foreground',
            )}>{delta}</span>
          )}
        </span>
      </div>
    </div>
  )
}
