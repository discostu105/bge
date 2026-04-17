import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Ticks } from '@/components/ui/ticks'
import { CostBadge } from '@/components/CostBadge'
import { SectionHeader } from '@/components/ui/section'
import type { ReactNode } from 'react'

type UpgradeType = 'Attack' | 'Defense'

interface UpgradeTrackProps {
  type: UpgradeType
  currentLevel: number
  maxLevel: number
  upgradeBeingResearched: string // 'None' | 'Attack' | 'Defense'
  researchTimer: number // ticks remaining; 0 when nothing in progress
  /** Cost object for the NEXT level, or null if at max. */
  nextCost: Record<string, number> | null
  /** True when this type's next upgrade cannot be afforded. */
  canAfford: boolean
  isResearching: boolean // true when upgradeMutation is in flight
  onResearch: () => void
  /** Header slot — typically a short description of what the track does. */
  description?: ReactNode
}

export function UpgradeTrack({
  type,
  currentLevel,
  maxLevel,
  upgradeBeingResearched,
  researchTimer,
  nextCost,
  canAfford,
  isResearching,
  onResearch,
  description,
}: UpgradeTrackProps) {
  const inProgressHere = upgradeBeingResearched === type
  const otherInProgress = upgradeBeingResearched !== 'None' && !inProgressHere
  const atMax = currentLevel >= maxLevel
  const disabled = atMax || otherInProgress || !canAfford || isResearching
  let disabledReason: string | undefined
  if (atMax) disabledReason = 'At max level'
  else if (otherInProgress) disabledReason = 'Another upgrade in progress'
  else if (!canAfford) disabledReason = 'Cannot afford'

  return (
    <section className="mb-6">
      <SectionHeader title={`${type} upgrades`} description={description} />
      <Card>
        <CardContent className="p-4 space-y-4">
          {/* Level chain */}
          <div className="flex items-center gap-2 flex-wrap">
            {Array.from({ length: maxLevel }).map((_, i) => {
              const level = i + 1
              const done = level <= currentLevel
              const current = level === currentLevel + 1 && inProgressHere
              return (
                <div key={level} className="flex items-center gap-2">
                  <div className={cn(
                    'flex flex-col items-center justify-center w-16 h-16 rounded-md border text-xs',
                    done && 'bg-success/20 border-success/40',
                    current && 'bg-primary/10 border-primary',
                    !done && !current && 'bg-card border-border text-muted-foreground',
                  )}>
                    <span className="label leading-none">Lv</span>
                    <span className="mono text-lg font-bold leading-tight">{level}</span>
                    {done && <Badge variant="secondary" className="mt-0.5 text-[9px] px-1 py-0">✓</Badge>}
                  </div>
                  {level < maxLevel && <span aria-hidden className="text-muted-foreground">→</span>}
                </div>
              )
            })}
          </div>

          {/* In-progress bar */}
          {inProgressHere && researchTimer > 0 && (
            <div className="space-y-1">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Researching level {currentLevel + 1}…</span>
                <Ticks n={researchTimer} />
              </div>
              <Progress value={Math.max(0, 100 - (researchTimer / (researchTimer + 1)) * 100)} />
            </div>
          )}

          {/* Research action */}
          {!atMax && (
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Next:</span>
                {nextCost && <CostBadge cost={nextCost} />}
              </div>
              <Button
                variant="default"
                size="sm"
                onClick={onResearch}
                disabled={disabled}
                title={disabledReason}
              >
                {inProgressHere ? 'Researching…' : 'Research'}
              </Button>
            </div>
          )}
          {atMax && (
            <div className="text-sm text-muted-foreground">At maximum level ({maxLevel}).</div>
          )}
        </CardContent>
      </Card>
    </section>
  )
}
