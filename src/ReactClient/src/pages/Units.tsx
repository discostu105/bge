import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import axios from 'axios'
import type { ColumnDef } from '@tanstack/react-table'
import apiClient from '@/api/client'
import type { UnitsViewModel, UnitViewModel } from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { AttackDialog } from '@/components/AttackDialog'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { DataTable } from '@/components/ui/data-table'
import { Stat } from '@/components/ui/stat'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { formatNumber } from '@/lib/formatters'

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
    <div className="flex items-center gap-2 flex-wrap">
      {error && <span className="text-destructive text-xs">{error}</span>}
      <Input
        type="number"
        min={1}
        max={initCount - 1}
        value={splitCount}
        onChange={(e) => setSplitCount(Number(e.target.value))}
        className="w-24 text-sm"
      />
      <Button
        size="sm"
        variant="secondary"
        onClick={() => splitMutation.mutate()}
        disabled={splitMutation.isPending}
      >
        Confirm Split
      </Button>
      <Button size="sm" variant="ghost" onClick={onDone}>
        Cancel
      </Button>
    </div>
  )
}

export function Units({ gameId }: UnitsProps) {
  const queryClient = useQueryClient()
  const [mergeError, setMergeError] = useState<string | null>(null)
  const [attackUnit, setAttackUnit] = useState<UnitViewModel | null>(null)
  const [splittingUnitId, setSplittingUnitId] = useState<string | null>(null)

  const { data, isLoading, error: queryError } = useQuery<UnitsViewModel>({
    queryKey: ['units', gameId],
    queryFn: () => apiClient.get('/api/units').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const mergeAllMutation = useMutation({
    mutationFn: () => apiClient.post('/api/units/merge', null),
    onSuccess: () => {
      setMergeError(null)
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setMergeError((err.response?.data as string) ?? 'Merge failed')
    },
  })

  const mergeMutation = useMutation({
    mutationFn: (unitDefId: string) =>
      apiClient.post(`/api/units/merge?unitDefId=${unitDefId}`, null),
    onSuccess: () => {
      setMergeError(null)
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setMergeError((err.response?.data as string) ?? 'Merge failed')
    },
  })

  const deployed = useMemo(
    () =>
      (data?.units.filter((u) => u.positionPlayerId != null) ?? [])
        .slice()
        .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name)),
    [data],
  )

  const atHome = useMemo(
    () =>
      (data?.units.filter((u) => u.positionPlayerId == null) ?? [])
        .slice()
        .sort((a, b) => b.count - a.count || a.definition.name.localeCompare(b.definition.name)),
    [data],
  )

  // canMerge: at least 2 stacks share the same unit definition
  const canMerge = useMemo(() => {
    if (!data) return false
    const counts = new Map<string, number>()
    for (const u of data.units) {
      counts.set(u.definition.id, (counts.get(u.definition.id) ?? 0) + 1)
    }
    return [...counts.values()].some((c) => c >= 2)
  }, [data])

  const totalUnits = data?.units.reduce((s, u) => s + u.count, 0) ?? 0
  const deployedCount = deployed.reduce((s, u) => s + u.count, 0)
  const atHomeCount = atHome.reduce((s, u) => s + u.count, 0)

  const splittingUnit = splittingUnitId != null
    ? atHome.find((u) => u.unitId === splittingUnitId) ?? null
    : null

  const homeColumns: ColumnDef<UnitViewModel, unknown>[] = [
    {
      id: 'name',
      header: 'Unit',
      accessorFn: (u) => u.definition.name,
      cell: ({ row }) => (
        <span className="font-medium">{row.original.definition.name}</span>
      ),
    },
    {
      id: 'count',
      header: () => <span className="block text-right">Count</span>,
      accessorFn: (u) => u.count,
      cell: ({ row }) => (
        <span className="block text-right mono tabular-nums">
          {formatNumber(row.original.count)}
        </span>
      ),
    },
    {
      id: 'attack',
      header: 'Attack',
      accessorFn: (u) => u.definition.attack,
      cell: ({ row }) => (
        <Stat label="Atk" value={row.original.definition.attack} />
      ),
    },
    {
      id: 'defense',
      header: 'Defense',
      accessorFn: (u) => u.definition.defense,
      cell: ({ row }) => (
        <Stat label="Def" value={row.original.definition.defense} />
      ),
    },
    {
      id: 'hp',
      header: 'HP',
      accessorFn: (u) => u.definition.hitpoints,
      cell: ({ row }) => (
        <Stat label="HP" value={row.original.definition.hitpoints} />
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const unit = row.original
        return (
          <div className="flex flex-wrap gap-1.5">
            <Button
              type="button"
              variant="destructive"
              size="sm"
              onClick={() => setAttackUnit(unit)}
            >
              Attack
            </Button>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => mergeMutation.mutate(unit.definition.id)}
              disabled={mergeMutation.isPending}
            >
              Merge
            </Button>
            {unit.count > 1 && (
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() =>
                  setSplittingUnitId((prev) =>
                    prev === unit.unitId ? null : unit.unitId,
                  )
                }
              >
                Split
              </Button>
            )}
          </div>
        )
      },
    },
  ]

  const deployedColumns: ColumnDef<UnitViewModel, unknown>[] = [
    {
      id: 'name',
      header: 'Unit',
      accessorFn: (u) => u.definition.name,
      cell: ({ row }) => (
        <span className="font-medium">{row.original.definition.name}</span>
      ),
    },
    {
      id: 'count',
      header: () => <span className="block text-right">Count</span>,
      accessorFn: (u) => u.count,
      cell: ({ row }) => (
        <span className="block text-right mono tabular-nums">
          {formatNumber(row.original.count)}
        </span>
      ),
    },
    {
      id: 'attack',
      header: 'Attack',
      accessorFn: (u) => u.definition.attack,
      cell: ({ row }) => (
        <Stat label="Atk" value={row.original.definition.attack} />
      ),
    },
    {
      id: 'defense',
      header: 'Defense',
      accessorFn: (u) => u.definition.defense,
      cell: ({ row }) => (
        <Stat label="Def" value={row.original.definition.defense} />
      ),
    },
    {
      id: 'hp',
      header: 'HP',
      accessorFn: (u) => u.definition.hitpoints,
      cell: ({ row }) => (
        <Stat label="HP" value={row.original.definition.hitpoints} />
      ),
    },
    {
      id: 'location',
      header: 'Location',
      cell: ({ row }) => {
        const unit = row.original
        return (
          <Link
            to={`/games/${gameId}/enemybase/${encodeURIComponent(unit.positionPlayerId!)}`}
            className="text-primary hover:underline text-sm"
          >
            {unit.positionPlayerName ?? unit.positionPlayerId}
          </Link>
        )
      },
    },
    {
      id: 'status',
      header: 'Status',
      cell: () => (
        <Badge variant="destructive">In Combat</Badge>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Units"
        actions={
          <Button
            variant="secondary"
            onClick={() => mergeAllMutation.mutate()}
            disabled={mergeAllMutation.isPending || !canMerge}
            title="Combine all unit stacks of the same type into single stacks"
          >
            Merge All
          </Button>
        }
      />

      {mergeError && <div className="text-destructive text-sm">{mergeError}</div>}

      <BuildQueue gameId={gameId} />

      {/* Summary stats */}
      {data && data.units.length > 0 && (
        <Section boxed>
          <div className="flex flex-wrap gap-6">
            <Stat label="Total Units" value={formatNumber(totalUnits)} />
            <Stat label="At Home" value={formatNumber(atHomeCount)} />
            <Stat label="Deployed" value={formatNumber(deployedCount)} />
          </div>
        </Section>
      )}

      {/* Deployed units */}
      {(isLoading || deployed.length > 0 || (data && data.units.length === 0)) && (
        <Section
          title={
            deployed.length > 0
              ? `Deployed (${formatNumber(deployedCount)} units)`
              : 'Deployed'
          }
        >
          <DataTable<UnitViewModel>
            columns={deployedColumns}
            rows={deployed}
            loading={isLoading}
            error={queryError && !data ? 'Failed to load units.' : undefined}
            empty="No units are currently deployed."
            sortable
            getRowId={(u) => u.unitId}
          />
        </Section>
      )}

      {/* Home base units */}
      <Section
        title={
          isLoading || !data
            ? 'Home Base'
            : `Home Base (${formatNumber(atHomeCount)} units)`
        }
        actions={undefined}
      >
        <DataTable<UnitViewModel>
          columns={homeColumns}
          rows={atHome}
          loading={isLoading}
          error={queryError && !data ? 'Failed to load units.' : undefined}
          empty="No units at home base. All units are deployed, or you have not built any units yet."
          sortable
          getRowId={(u) => u.unitId}
        />
      </Section>

      {/* Split form — renders below tables, keyed to the unit being split */}
      {splittingUnit && (
        <Section title={`Splitting: ${splittingUnit.definition.name} (${formatNumber(splittingUnit.count)})`}>
          <SplitUnitForm
            unitId={splittingUnit.unitId}
            initCount={splittingUnit.count}
            gameId={gameId}
            onDone={() => setSplittingUnitId(null)}
          />
        </Section>
      )}

      <AttackDialog
        open={attackUnit !== null}
        onOpenChange={(open) => { if (!open) setAttackUnit(null) }}
        gameId={gameId}
        unit={attackUnit}
      />
    </div>
  )
}
