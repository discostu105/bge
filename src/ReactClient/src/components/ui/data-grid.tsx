import type { ReactNode, CSSProperties } from 'react'
import { cn } from '@/lib/utils'

interface DataGridProps {
  cols?: number
  className?: string
  children: ReactNode
}

/**
 * Hex-grid style container: items separated by hair-thin border-colored rules.
 * Achieved with gap:1px over a border-colored background.
 */
export function DataGrid({ cols = 3, className, children }: DataGridProps) {
  const style: CSSProperties = {
    gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`,
  }
  return (
    <div
      className={cn('grid gap-px bg-border border border-border', className)}
      style={style}
    >
      {children}
    </div>
  )
}

/**
 * Cell used inside a DataGrid — handles the background so the gap shows as a grid line.
 */
export function DataGridCell({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div className={cn('bg-background/50 p-3', className)}>{children}</div>
  )
}
