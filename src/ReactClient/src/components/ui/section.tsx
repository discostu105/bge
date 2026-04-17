import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'

interface SectionHeaderProps {
  title: string
  description?: ReactNode
  actions?: ReactNode
  className?: string
}

export function SectionHeader({ title, description, actions, className }: SectionHeaderProps) {
  return (
    <div className={cn('mb-3 flex items-start justify-between gap-3', className)}>
      <div className="min-w-0">
        <h2 className="text-lg font-semibold">{title}</h2>
        {description && <p className="text-sm text-muted-foreground mt-0.5">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
    </div>
  )
}

interface SectionProps {
  title?: string
  description?: ReactNode
  actions?: ReactNode
  children: ReactNode
  /** When false, renders without the Card wrapper. Default true. */
  boxed?: boolean
  className?: string
  bodyClassName?: string
}

export function Section({
  title, description, actions, children, boxed = true, className, bodyClassName,
}: SectionProps) {
  const body = boxed ? (
    <Card><CardContent className={cn('p-4', bodyClassName)}>{children}</CardContent></Card>
  ) : (
    <div className={cn(bodyClassName)}>{children}</div>
  )
  return (
    <section className={cn('mb-6', className)}>
      {title && <SectionHeader title={title} description={description} actions={actions} />}
      {body}
    </section>
  )
}
