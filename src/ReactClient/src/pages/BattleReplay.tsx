import { useQuery } from '@tanstack/react-query'
import { useParams, Link } from 'react-router'
import apiClient from '@/api/client'
import type { BattleReportDetailViewModel, UnitCountViewModel } from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonLine } from '@/components/Skeleton'
import { useTouchZoomPan } from '@/hooks/useTouchZoomPan'

interface BattleReplayProps {
  gameId: string
}

function UnitList({ units, label }: { units: UnitCountViewModel[]; label?: string }) {
  if (units.length === 0) return <span className="text-muted-foreground text-xs">None</span>
  return (
    <span>
      {label && <span className="text-xs text-muted-foreground mr-1">{label}</span>}
      {units.map((u, i) => (
        <span key={u.unitName}>
          {i > 0 && ', '}
          {u.count}x {u.unitName}
        </span>
      ))}
    </span>
  )
}

function OutcomeBadge({ outcome }: { outcome: string }) {
  const isVictory = outcome === 'Attacker won'
  const isDraw = outcome === 'Draw'
  const cls = isVictory
    ? 'bg-green-500/20 text-green-400 border-green-500/30'
    : isDraw
      ? 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30'
      : 'bg-red-500/20 text-red-400 border-red-500/30'
  return (
    <span className={`inline-block rounded border px-2 py-0.5 text-sm font-semibold ${cls}`}>
      {isVictory ? 'Victory' : isDraw ? 'Draw' : 'Defeat'}
    </span>
  )
}

export function BattleReplay({ gameId }: BattleReplayProps) {
  const { reportId } = useParams<{ reportId: string }>()
  const { containerProps, wrapperStyle } = useTouchZoomPan()

  const { data: report, isLoading, error, refetch } = useQuery<BattleReportDetailViewModel>({
    queryKey: ['battle-report', gameId, reportId],
    queryFn: () =>
      apiClient
        .get(`/api/battle/report?reportId=${encodeURIComponent(reportId ?? '')}`)
        .then((r) => r.data),
    enabled: !!reportId,
  })

  if (isLoading) return (
    <div className="max-w-3xl space-y-6 animate-pulse" aria-hidden="true">
      <div className="space-y-2">
        <div className="flex items-center gap-3">
          <SkeletonLine className="h-6 w-36" />
          <div className="h-6 w-16 rounded bg-muted" />
        </div>
        <SkeletonLine className="h-3.5 w-40" />
        <SkeletonLine className="h-3.5 w-64" />
      </div>
      <div className="rounded-lg border p-4 space-y-3">
        <SkeletonLine className="h-4 w-28" />
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <SkeletonLine className="h-3 w-32" />
            <SkeletonLine className="h-3 w-full" />
          </div>
          <div className="space-y-2">
            <SkeletonLine className="h-3 w-32" />
            <SkeletonLine className="h-3 w-full" />
          </div>
        </div>
      </div>
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="rounded-lg border p-3 space-y-2">
          <div className="h-5 w-16 rounded bg-muted" />
          <div className="grid grid-cols-2 gap-4">
            <SkeletonLine className="h-3 w-full" />
            <SkeletonLine className="h-3 w-full" />
          </div>
        </div>
      ))}
    </div>
  )
  if (error) return <ApiError message="Battle report not found." onRetry={() => void refetch()} />
  if (!report) return null

  const date = new Date(report.createdAt)

  return (
    <div {...containerProps}>
    <div className="max-w-3xl space-y-6" style={wrapperStyle}>
      {/* Header */}
      <div className="space-y-2">
        <div className="flex items-center gap-3">
          <h1 className="text-xl font-bold">Battle Report</h1>
          <OutcomeBadge outcome={report.outcome} />
        </div>
        <p className="text-sm text-muted-foreground">
          {date.toLocaleDateString()} {date.toLocaleTimeString()}
        </p>
        <p className="text-sm">
          <Link to={`/games/${gameId}/player/${report.attackerId}`} className="text-primary hover:underline font-medium">
            {report.attackerName}
          </Link>
          <span className="text-muted-foreground"> ({report.attackerRace})</span>
          {' vs '}
          <Link to={`/games/${gameId}/player/${report.defenderId}`} className="text-primary hover:underline font-medium">
            {report.defenderName}
          </Link>
          <span className="text-muted-foreground"> ({report.defenderRace})</span>
        </p>
      </div>

      {/* Initial Forces */}
      <div className="rounded-lg border p-4 space-y-3">
        <h2 className="font-semibold text-sm">Initial Forces</h2>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <div className="text-xs text-muted-foreground mb-1">Attacker (Strength: {report.totalAttackerStrengthBefore})</div>
            <UnitList units={report.attackerUnitsInitial} />
          </div>
          <div>
            <div className="text-xs text-muted-foreground mb-1">Defender (Strength: {report.totalDefenderStrengthBefore})</div>
            <UnitList units={report.defenderUnitsInitial} />
          </div>
        </div>
      </div>

      {/* Round-by-round */}
      <div className="space-y-3">
        <h2 className="font-semibold text-sm">Round-by-Round Replay</h2>
        <div className="space-y-2">
          {report.rounds.map((round) => (
            <div key={round.roundNumber} className="rounded-lg border p-3">
              <div className="flex items-center gap-2 mb-2">
                <span className="text-xs font-semibold bg-secondary px-2 py-0.5 rounded">
                  Round {round.roundNumber}
                </span>
              </div>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div className="space-y-1">
                  <div className="text-xs text-muted-foreground">Attacker</div>
                  {round.attackerCasualties.length > 0 && (
                    <div className="text-red-400 text-xs">
                      Lost: {round.attackerCasualties.map((u, i) => (
                        <span key={u.unitName}>{i > 0 && ', '}{u.count}x {u.unitName}</span>
                      ))}
                    </div>
                  )}
                  <div>
                    <UnitList units={round.attackerUnitsRemaining} label="Remaining:" />
                  </div>
                </div>
                <div className="space-y-1">
                  <div className="text-xs text-muted-foreground">Defender</div>
                  {round.defenderCasualties.length > 0 && (
                    <div className="text-red-400 text-xs">
                      Lost: {round.defenderCasualties.map((u, i) => (
                        <span key={u.unitName}>{i > 0 && ', '}{u.count}x {u.unitName}</span>
                      ))}
                    </div>
                  )}
                  <div>
                    <UnitList units={round.defenderUnitsRemaining} label="Remaining:" />
                  </div>
                </div>
              </div>
              {round.attackerUnitsRemaining.length === 0 && (
                <div className="mt-1 text-xs text-red-400 font-medium">Attacker eliminated</div>
              )}
              {round.defenderUnitsRemaining.length === 0 && (
                <div className="mt-1 text-xs text-green-400 font-medium">Defender eliminated</div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Spoils */}
      {(report.landTransferred > 0 || report.workersCaptured > 0 || Object.keys(report.resourcesStolen).length > 0) && (
        <div className="rounded-lg border p-4 space-y-2">
          <h2 className="font-semibold text-sm">Spoils of War</h2>
          <div className="flex flex-wrap gap-2 text-sm">
            {report.landTransferred > 0 && (
              <span className="rounded bg-green-500/20 text-green-400 border border-green-500/30 px-2 py-0.5 text-xs">
                +{report.landTransferred} land captured
              </span>
            )}
            {report.workersCaptured > 0 && (
              <span className="rounded bg-green-500/20 text-green-400 border border-green-500/30 px-2 py-0.5 text-xs">
                +{report.workersCaptured} workers captured
              </span>
            )}
            {Object.entries(report.resourcesStolen).map(([resource, amount]) => (
              <span key={resource} className="rounded bg-green-500/20 text-green-400 border border-green-500/30 px-2 py-0.5 text-xs">
                +{amount} {resource} pillaged
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
    </div>
  )
}
