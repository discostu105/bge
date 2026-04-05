import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Link, useParams } from 'react-router'
import axios from 'axios'
import apiClient from '@/api/client'
import type { EnemyBaseViewModel, BattleResultViewModel, SpyReportViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface EnemyBaseProps {
  gameId: string
}

export function EnemyBase({ gameId }: EnemyBaseProps) {
  const { enemyPlayerId } = useParams<{ enemyPlayerId: string }>()
  const [lastError, setLastError] = useState<string | null>(null)
  const [battleResult, setBattleResult] = useState<BattleResultViewModel | null>(null)
  const [spyReport, setSpyReport] = useState<SpyReportViewModel | null>(null)

  const { data: model, isLoading, error: queryError, refetch } = useQuery<EnemyBaseViewModel>({
    queryKey: ['enemybase', gameId, enemyPlayerId],
    queryFn: () =>
      apiClient
        .get(`/api/battle/enemybase?enemyPlayerId=${encodeURIComponent(enemyPlayerId ?? '')}`)
        .then((r) => r.data),
    refetchInterval: 10_000,
    enabled: !!enemyPlayerId,
  })

  const attackMutation = useMutation({
    mutationFn: () =>
      apiClient
        .post(`/api/battle/attack?enemyPlayerId=${encodeURIComponent(enemyPlayerId ?? '')}`, null)
        .then((r) => r.data as BattleResultViewModel),
    onSuccess: (result) => {
      setLastError(null)
      setBattleResult(result)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Attack failed')
    },
  })

  const spyMutation = useMutation({
    mutationFn: () =>
      apiClient
        .post(`/api/spy/execute?targetPlayerId=${encodeURIComponent(enemyPlayerId ?? '')}`, null)
        .then((r) => r.data as SpyReportViewModel),
    onSuccess: (report) => {
      setLastError(null)
      setSpyReport(report)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Spy failed')
    },
  })

  if (isLoading) return <PageLoader message="Loading enemy base..." />
  if (queryError) return <ApiError message="Failed to load enemy base." onRetry={() => void refetch()} />
  if (!model) return null

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Attack</h1>

      <Link
        to={`/games/${gameId}/units`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        ← Request Reinforcements
      </Link>

      <div className="flex flex-wrap gap-3">
        <button
          onClick={() => attackMutation.mutate()}
          disabled={attackMutation.isPending}
          className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
        >
          Attack
        </button>
        <button
          onClick={() => spyMutation.mutate()}
          disabled={spyMutation.isPending}
          className="rounded bg-secondary px-4 py-1.5 text-sm text-secondary-foreground hover:opacity-90 disabled:opacity-50"
        >
          {spyMutation.isPending ? 'Spying…' : `Send Spy (${model.spyCostLabel})`}
        </button>
      </div>

      {lastError && <div className="text-destructive text-sm">{lastError}</div>}

      {/* Spy Report */}
      {spyReport && (
        <details open className="rounded-lg border bg-card p-4">
          <summary className="cursor-pointer font-semibold text-sm">
            Spy Report — {spyReport.targetPlayerName}{' '}
            <span className="text-muted-foreground font-normal">
              ({new Date(spyReport.reportTime).toLocaleTimeString()})
            </span>
          </summary>
          <div className="mt-3 space-y-2 text-sm">
            <p>
              <strong>Approximate Resources:</strong> ~{Math.round(spyReport.approximateMinerals)}{' '}
              minerals, ~{Math.round(spyReport.approximateGas)} gas
            </p>
            {spyReport.unitEstimates.length > 0 ? (
              <table className="w-full text-sm">
                <thead className="border-b">
                  <tr>
                    <th className="py-1 text-left font-medium text-muted-foreground">Unit</th>
                    <th className="py-1 text-left font-medium text-muted-foreground">~Count</th>
                  </tr>
                </thead>
                <tbody>
                  {spyReport.unitEstimates
                    .slice()
                    .sort((a, b) => b.approximateCount - a.approximateCount)
                    .map((u) => (
                      <tr key={u.unitDefId} className="border-b border-border">
                        <td className="py-1">{u.unitTypeName}</td>
                        <td className="py-1">~{u.approximateCount}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
            ) : (
              <p className="text-muted-foreground">No units detected at base.</p>
            )}
            <p className="text-xs text-muted-foreground">
              Next spy available: {new Date(spyReport.cooldownExpiresAt).toLocaleTimeString()}
            </p>
          </div>
        </details>
      )}

      {/* Battle Result */}
      {battleResult ? (
        <div className="space-y-4">
          <h2 className="text-xl font-bold">Battle Result</h2>
          <p
            className={`text-2xl font-bold ${
              battleResult.outcome === 'AttackerWon'
                ? 'text-green-400'
                : battleResult.outcome === 'DefenderWon'
                  ? 'text-red-400'
                  : 'text-yellow-400'
            }`}
          >
            {battleResult.outcome === 'AttackerWon'
              ? 'Victory!'
              : battleResult.outcome === 'DefenderWon'
                ? 'Defeat'
                : 'Draw'}
          </p>
          <p className="text-sm">
            {battleResult.attackerId ? (
              <Link
                to={`/games/${gameId}/player/${battleResult.attackerId}`}
                className="font-bold hover:underline"
              >
                {battleResult.attackerName}
              </Link>
            ) : (
              <strong>{battleResult.attackerName}</strong>
            )}{' '}
            vs{' '}
            {battleResult.defenderId ? (
              <Link
                to={`/games/${gameId}/player/${battleResult.defenderId}`}
                className="font-bold hover:underline"
              >
                {battleResult.defenderName}
              </Link>
            ) : (
              <strong>{battleResult.defenderName}</strong>
            )}
          </p>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <strong>Attacker strength:</strong> {battleResult.totalAttackerStrengthBefore}
            </div>
            <div>
              <strong>Defender strength:</strong> {battleResult.totalDefenderStrengthBefore}
            </div>
          </div>

          {(battleResult.landTransferred > 0 ||
            battleResult.workersCaptured > 0 ||
            Object.keys(battleResult.resourcesPillaged).length > 0) && (
            <div>
              <h4 className="font-semibold text-sm mb-2">Spoils of War</h4>
              {battleResult.landTransferred > 0 && (
                <span className="rounded bg-green-800/50 px-2 py-0.5 text-xs text-green-300 mr-2">
                  +{battleResult.landTransferred} land captured
                </span>
              )}
              {battleResult.workersCaptured > 0 && (
                <span className="rounded bg-green-800/50 px-2 py-0.5 text-xs text-green-300 mr-2">
                  +{battleResult.workersCaptured} workers captured
                </span>
              )}
              {Object.entries(battleResult.resourcesPillaged).map(([res, amt]) => (
                <span
                  key={res}
                  className="rounded bg-yellow-800/50 px-2 py-0.5 text-xs text-yellow-200 mr-2"
                >
                  +{amt} {res} pillaged
                </span>
              ))}
            </div>
          )}

          <details className="rounded-lg border bg-card p-4">
            <summary className="cursor-pointer font-semibold text-sm">Unit Losses</summary>
            <div className="grid grid-cols-2 gap-4 mt-3">
              <div>
                <h5 className="font-medium text-sm mb-2">Your losses ({battleResult.attackerName})</h5>
                {battleResult.unitsLostByAttacker.length === 0 ? (
                  <p className="text-muted-foreground text-xs">No losses</p>
                ) : (
                  <table className="w-full text-xs">
                    <thead className="border-b">
                      <tr>
                        <th className="py-1 text-left font-medium text-muted-foreground">Unit</th>
                        <th className="py-1 text-left font-medium text-muted-foreground">Lost</th>
                      </tr>
                    </thead>
                    <tbody>
                      {battleResult.unitsLostByAttacker
                        .slice()
                        .sort((a, b) => b.count - a.count)
                        .map((l, i) => (
                          <tr key={i} className="border-b border-border">
                            <td className="py-1">{l.unitName}</td>
                            <td className="py-1">{l.count}</td>
                          </tr>
                        ))}
                    </tbody>
                  </table>
                )}
              </div>
              <div>
                <h5 className="font-medium text-sm mb-2">Enemy losses ({battleResult.defenderName})</h5>
                {battleResult.unitsLostByDefender.length === 0 ? (
                  <p className="text-muted-foreground text-xs">No losses</p>
                ) : (
                  <table className="w-full text-xs">
                    <thead className="border-b">
                      <tr>
                        <th className="py-1 text-left font-medium text-muted-foreground">Unit</th>
                        <th className="py-1 text-left font-medium text-muted-foreground">Lost</th>
                      </tr>
                    </thead>
                    <tbody>
                      {battleResult.unitsLostByDefender
                        .slice()
                        .sort((a, b) => b.count - a.count)
                        .map((l, i) => (
                          <tr key={i} className="border-b border-border">
                            <td className="py-1">{l.unitName}</td>
                            <td className="py-1">{l.count}</td>
                          </tr>
                        ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </details>
        </div>
      ) : (
        <>
          {/* Your Troops */}
          <div className="rounded-lg border bg-card overflow-hidden">
            <div className="border-b px-4 py-3 bg-secondary/30">
              <strong className="text-sm">Your Troops</strong>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="border-b">
                  <tr>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Count</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Stats</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Position</th>
                  </tr>
                </thead>
                <tbody>
                  {model.playerAttackingUnits.units
                    .slice()
                    .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name))
                    .map((u) => (
                      <tr key={u.unitId} className="border-b border-border">
                        <td className="py-2 px-3">{u.definition.name}</td>
                        <td className="py-2 px-3 tabular-nums">{u.count.toLocaleString()}</td>
                        <td className="py-2 px-3 text-xs text-muted-foreground">
                          ⚔️{u.definition.attack} 🛡️{u.definition.defense}
                        </td>
                        <td className="py-2 px-3 text-xs text-muted-foreground">
                          {u.positionPlayerId ?? '—'}
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Enemy Troops */}
          <div className="rounded-lg border bg-card overflow-hidden">
            <div className="border-b px-4 py-3 bg-secondary/30">
              <strong className="text-sm">Enemy Troops</strong>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="border-b">
                  <tr>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Count</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Stats</th>
                    <th className="py-2 px-3 text-left font-medium text-muted-foreground">Position</th>
                  </tr>
                </thead>
                <tbody>
                  {model.enemyDefendingUnits.units
                    .slice()
                    .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name))
                    .map((u) => (
                      <tr key={u.unitId} className="border-b border-border">
                        <td className="py-2 px-3">{u.definition.name}</td>
                        <td className="py-2 px-3 tabular-nums">{u.count.toLocaleString()}</td>
                        <td className="py-2 px-3 text-xs text-muted-foreground">
                          ⚔️{u.definition.attack} 🛡️{u.definition.defense}
                        </td>
                        <td className="py-2 px-3 text-xs text-muted-foreground">
                          {u.positionPlayerId ?? '—'}
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
