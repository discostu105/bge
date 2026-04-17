import { Gem, FlaskConical, Coins, Mountain, Circle } from 'lucide-react'
import { cn } from '@/lib/utils'

const ICONS: Record<string, React.ComponentType<{ className?: string }>> = {
  minerals: Gem,
  gas: FlaskConical,
  land: Mountain,
  credits: Coins,
  gold: Coins,
}

interface ResourceIconProps {
  name: string
  className?: string
}

export function ResourceIcon({ name, className }: ResourceIconProps) {
  const Icon = ICONS[name.toLowerCase()] ?? Circle
  return <Icon className={cn('h-3.5 w-3.5 shrink-0', className)} aria-hidden />
}
