import type { CSSProperties } from 'react'
import { cn } from '@/lib/utils'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'
import type { Race } from '@/lib/race'

const DESCRIPTIONS: Record<Race, { name: string; tagline: string }> = {
  terran: { name: 'Terran', tagline: 'Industrial · balanced · mechanized' },
  zerg: { name: 'Zerg', tagline: 'Organic · cheap units · swarm' },
  protoss: { name: 'Protoss', tagline: 'Crystalline · expensive · elite' },
}

const ACCENT_STYLES: Record<Race, CSSProperties> = {
  terran: {
    ['--color-race-primary' as string]: '#CBD5E1',
    ['--color-race-secondary' as string]: '#F59E0B',
    ['--color-race-emblem-fill' as string]: '#1f2937',
  },
  zerg: {
    ['--color-race-primary' as string]: '#B45309',
    ['--color-race-secondary' as string]: '#ea580c',
    ['--color-race-emblem-fill' as string]: '#3a1a08',
  },
  protoss: {
    ['--color-race-primary' as string]: '#FBBF24',
    ['--color-race-secondary' as string]: '#0e7490',
    ['--color-race-emblem-fill' as string]: '#0c1e2e',
  },
}

const EMBLEMS: Record<Race, React.ComponentType<React.SVGProps<SVGSVGElement>>> = {
  terran: TerranEmblem,
  zerg: ZergEmblem,
  protoss: ProtossEmblem,
}

export function RaceTile({
  race,
  selected,
  onSelect,
}: {
  race: Race
  selected: boolean
  onSelect: (race: Race) => void
}) {
  const { name, tagline } = DESCRIPTIONS[race]
  const Emblem = EMBLEMS[race]
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={() => onSelect(race)}
      style={ACCENT_STYLES[race]}
      className={cn(
        'flex-1 flex flex-col items-center gap-1.5 p-4 min-h-[150px] transition-colors bg-background/40 cursor-pointer',
        selected
          ? 'border-2 border-[color:var(--color-race-primary)] bg-[color:var(--color-race-primary)]/5'
          : 'border border-border hover:border-[color:var(--color-race-primary)]/60',
      )}
    >
      <Emblem width={56} height={56} style={{ color: 'var(--color-race-primary)' }} />
      <div className="label mt-1" style={{ color: 'var(--color-race-primary)' }}>{name}</div>
      <div className="text-[10.5px] text-muted-foreground text-center leading-tight">{tagline}</div>
    </button>
  )
}
