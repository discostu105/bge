import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import axios from 'axios'
import apiClient from '@/api/client'
import type { UnitsViewModel, UnitViewModel } from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { ApiError } from '@/components/ApiError'
import { SkeletonRow } from '@/components/Skeleton'

interface UnitsProps {
  gameId: string
}

function SplitUnitForm({
  unitId,
  initCount,
  gameId,
  onDone,
}: {
  unitId: string
  initCount: number
  gameId: string
  onDone: () => void
}) {
  const queryClient = useQueryClient()
  const [splitCount, setSplitCount] = useState(Math.max(1, Math.floor(initCount / 2)))
  const [error, setError] = useState<string | null>(null)

  const splitMutation = useMutation({
    mutationFn: () =>
      apiClient.post(`/api/units/split?unitId=${unitId}&count=${splitCount}`, ''),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
      onDone()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Split failed')
    },
  })

  return (
    <div className="flex items-center gap-2 mt-1">
      {error && <span className="text-destructive text-xs">{error}</span>}
      <input
        type="number"
        min={1}
        max={initCount - 1}
        value={splitCount}
        onChange={(e) => setSplitCount(Number(e.target.value))}
        className="w-20 rounded border bg-input px-2 py-1 text-xs"
      />
      <button
        onClick={() => splitMutation.mutate()}
        disabled={splitMutation.isPending}
        className="rounded bg-secondary min-h-[36px] px-3 py-1.5 text-xs text-secondary-foreground hover:opacity-90"
      >
        Split
      </button>
      <button
        onClick={onDone}
        className="rounded min-h-[36px] px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground"
      >
        Cancel
      </button>
    </div>
  )
}

function UnitRow({
  unit,
  gameId,
  onMerge,
}: {
  unit: UnitViewModel
  gameId: string
  onMerge: (defId: string) => void
}) {
  const [showSplit, setShowSplit] = useState(false)

  return (
    <tr className="border-b border-border">
      <td className="py-2 px-3 font-medium">{unit.definition?.name}</td>
      <td className="py-2 px-3 text-right tabular-nums">{unit.count.toLocaleString()}</td>
      <td className="py-2 px-3 text-xs text-muted-foreground hidden sm:table-cell">
        ⚔️{unit.definition?.attack} 🛡️{unit.definition?.defense} ❤️{unit.definition?.hitpoints}
      </td>
      {unit.positionPlayerId && (
        <td className="py-2 px-3 text-xs">
          <Link
            to={`/games/${gameId}/enemybase/${encodeURIComponent(unit.positionPlayerId)}`}
            className="text-primary hover:underline"
          >
            {unit.positionPlayerName ?? unit.positionPlayerId}
          </Link>
        </td>
      )}
      <td className="py-2 px-3">
        <div className="flex flex-wrap gap-1.5">
          {!unit.positionPlayerId && (
            <Link
              to={`/games/${gameId}/selectenemy/${unit.unitId}`}
              className="rounded bg-destructive min-h-[36px] px-3 py-1.5 text-xs text-destructive-foreground hover:opacity-90 inline-flex items-center"
            >
              Attack
            </Link>
          )}
          <button
            onClick={() => onMerge(unit.definition.id)}
            className="rounded bg-secondary min-h-[36px] px-3 py-1.5 text-xs text-secondary-foreground hover:opacity-90"
          >
            Merge
          </button>
          {!unit.positionPlayerId && unit.count > 1 && (
            <>
              {!showSplit ? (
                <button
                  onClick={() => setShowSplit(true)}
                  className="rounded bg-secondary min-h-[36px] px-3 py-1.5 text-xs text-secondary-foreground hover:opacity-90"
                >
                  Split
                </button>
              ) : (
                <SplitUnitForm
                  unitId={unit.unitId}
                  initCount={unit.count}
                  gameId={gameId}
                  onDone={() => setShowSplit(false)}
                />
              )}
            </>
          )}
        </div>
      </td>
    </tr>
  )
}

export function Units({ gameId }: UnitsProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const { data, isLoading, error: queryError, refetch } = useQuery<UnitsViewModel>({
    queryKey: ['units', gameId],
    queryFn: () => apiClient.get('/api/units').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const mergeAllMutation = useMutation({
    mutationFn: () => apiClient.post('/api/units/merge', null),
    onSuccess: () => {
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Merge failed')
    },
  })

  const mergeMutation = useMutation({
    mutationFn: (unitDefId: string) =>
      apiClient.post(`/api/units/merge?unitDefId=${unitDefId}`, null),
    onSuccess: () => {
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Merge failed')
    },
  })

  const deployed = data?.units.filter((u) => u.positionPlayerId != null) ?? []
  const atHome = data?.units.filter((u) => u.positionPlayerId == null) ?? []

  const totalUnits = data?.units.reduce((s, u) => s + u.count, 0) ?? 0
  const stackCount = data?.units.length ?? 0

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Units</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}

      <BuildQueue gameId={gameId} />

      {isLoading ? (
        <div className="rounded-lg border bg-card overflow-hidden" aria-hidden="true">
          <table className="w-full text-sm">
            <tbody>
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonRow key={i} cols={5} />
              ))}
            </tbody>
          </table>
        </div>
      ) : queryError && !data ? (
        <ApiError message="Failed to load units." onRetry={() => void refetch()} />
      ) : !data || data.units.length === 0 ? (
        <div className="rounded-lg border bg-card p-8 text-center">
          <div className="text-4xl mb-3">🪖</div>
          <div className="font-semibold mb-1">No units yet</div>
          <div className="text-sm text-muted-foreground">
            Build units from your base buildings to start training your army.
          </div>
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-center gap-3">
            <button
              onClick={() => mergeAllMutation.mutate()}
              disabled={mergeAllMutation.isPending}
              className="rounded bg-secondary min-h-[44px] px-4 py-2 text-sm text-secondary-foreground hover:opacity-90 disabled:opacity-50"
              title="Combine all unit stacks of the same type into single stacks"
            >
              Merge All
            </button>
            <span className="text-sm text-muted-foreground">
              {totalUnits.toLocaleString()} units in {stackCount} stacks
            </span>
          </div>

          {deployed.length > 0 && (
            <div className="rounded-lg border bg-card overflow-hidden">
              <div className="flex items-center justify-between px-4 py-3 border-b bg-secondary/30">
                <span className="font-semibold text-sm">
                  Deployed ({deployed.reduce((s, u) => s + u.count, 0).toLocaleString()} units)
                </span>
                <span className="rounded-full bg-red-900/50 px-2 py-0.5 text-xs text-red-300">
                  In Combat
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b">
                    <tr>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Unit</th>
                      <th scope="col" className="py-2 px-3 text-right font-medium text-muted-foreground">Count</th>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Stats</th>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Location</th>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deployed
                      .slice()
                      .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name))
                      .map((unit) => (
                        <UnitRow
                          key={unit.unitId}
                          unit={unit}
                          gameId={gameId}
                          onMerge={(defId) => mergeMutation.mutate(defId)}
                        />
                      ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          <div className="rounded-lg border bg-card overflow-hidden">
            <div className="flex items-center justify-between px-4 py-3 border-b bg-secondary/30">
              <span className="font-semibold text-sm">
                Home Base ({atHome.reduce((s, u) => s + u.count, 0).toLocaleString()} units)
              </span>
              <span className="rounded-full bg-green-900/50 px-2 py-0.5 text-xs text-green-300">
                Defending
              </span>
            </div>
            {atHome.length === 0 ? (
              <div className="p-6 text-center text-sm text-muted-foreground">
                All units are deployed.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b">
                    <tr>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Unit</th>
                      <th scope="col" className="py-2 px-3 text-right font-medium text-muted-foreground">Count</th>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Stats</th>
                      <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {atHome
                      .slice()
                      .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name))
                      .map((unit) => (
                        <UnitRow
                          key={unit.unitId}
                          unit={unit}
                          gameId={gameId}
                          onMerge={(defId) => mergeMutation.mutate(defId)}
                        />
                      ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
