import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import axios from 'axios'
import { SwordsIcon, MergeIcon, SplitIcon, HammerIcon } from 'lucide-react'
import type { ColumnDef } from '@tanstack/react-table'
import apiClient from '@/api/client'
import type { UnitsViewModel, UnitViewModel } from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { AttackDialog } from '@/components/AttackDialog'
import { TrainUnitsDialog } from '@/components/TrainUnitsDialog'
import { SplitUnitDialog } from '@/components/SplitUnitDialog'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { DataTable } from '@/components/ui/data-table'
import { Stat } from '@/components/ui/stat'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { formatNumber } from '@/lib/formatters'

interface UnitsProps {
  gameId: string
}

export function Units({ gameId }: UnitsProps) {
  const queryClient = useQueryClient()
  const [mergeError, setMergeError] = useState<string | null>(null)
  const [attackUnit, setAttackUnit] = useState<UnitViewModel | null>(null)
  const [splitUnit, setSplitUnit] = useState<UnitViewModel | null>(null)
  const [trainOpen, setTrainOpen] = useState(false)

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

  const mergeTypeMutation = useMutation({
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

  const stackCountByDefId = useMemo(() => {
    const counts = new Map<string, number>()
    for (const u of atHome) counts.set(u.definition.id, (counts.get(u.definition.id) ?? 0) + 1)
    return counts
  }, [atHome])

  const canMergeAny = useMemo(
    () => [...stackCountByDefId.values()].some((c) => c >= 2),
    [stackCountByDefId],
  )

  const totalUnits = data?.units.reduce((s, u) => s + u.count, 0) ?? 0
  const deployedCount = deployed.reduce((s, u) => s + u.count, 0)
  const atHomeCount = atHome.reduce((s, u) => s + u.count, 0)

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
        const canMergeType = (stackCountByDefId.get(unit.definition.id) ?? 0) >= 2
        return (
          <div className="flex flex-wrap gap-1.5 justify-end">
            <Button
              type="button"
              variant="destructive"
              size="sm"
              onClick={() => setAttackUnit(unit)}
              title="Attack an enemy with this stack"
            >
              <SwordsIcon className="h-3.5 w-3.5" />
              Attack
            </Button>
            {unit.count > 1 && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setSplitUnit(unit)}
                title="Split this stack"
              >
                <SplitIcon className="h-3.5 w-3.5" />
                Split
              </Button>
            )}
            {canMergeType && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => mergeTypeMutation.mutate(unit.definition.id)}
                disabled={mergeTypeMutation.isPending}
                title={`Merge all ${unit.definition.name} stacks into one`}
              >
                <MergeIcon className="h-3.5 w-3.5" />
                Merge
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

  const hasAnyUnits = (data?.units.length ?? 0) > 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="Units"
        actions={
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              onClick={() => mergeAllMutation.mutate()}
              disabled={mergeAllMutation.isPending || !canMergeAny}
              title={canMergeAny
                ? 'Combine all stacks of the same type into single stacks'
                : 'Nothing to merge — each unit type only has one stack'}
            >
              <MergeIcon className="h-4 w-4" />
              Merge all
            </Button>
            <Button
              onClick={() => setTrainOpen(true)}
              title="Open the training panel"
            >
              <HammerIcon className="h-4 w-4" />
              Train units
            </Button>
          </div>
        }
      />

      {mergeError && <div className="text-destructive text-sm">{mergeError}</div>}

      {!isLoading && !hasAnyUnits && (
        <Section boxed>
          <EmptyState
            icon={<SwordsIcon />}
            title="No units yet"
            description="Train your first units to start building an army."
            action={{ label: 'Train units', onClick: () => setTrainOpen(true) }}
          />
        </Section>
      )}

      {data && data.units.length > 0 && (
        <Section boxed>
          <div className="flex flex-wrap gap-6">
            <Stat label="Total Units" value={formatNumber(totalUnits)} />
            <Stat label="At Home" value={formatNumber(atHomeCount)} />
            <Stat label="Deployed" value={formatNumber(deployedCount)} />
          </div>
        </Section>
      )}

      <BuildQueue gameId={gameId} />

      {(isLoading || deployed.length > 0) && (
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

      {hasAnyUnits && (
        <Section
          title={
            isLoading || !data
              ? 'Home Base'
              : `Home Base (${formatNumber(atHomeCount)} units)`
          }
        >
          <DataTable<UnitViewModel>
            columns={homeColumns}
            rows={atHome}
            loading={isLoading}
            error={queryError && !data ? 'Failed to load units.' : undefined}
            empty="All units are deployed."
            sortable
            getRowId={(u) => u.unitId}
          />
        </Section>
      )}

      <TrainUnitsDialog
        open={trainOpen}
        onOpenChange={setTrainOpen}
        gameId={gameId}
      />
      <SplitUnitDialog
        open={splitUnit !== null}
        onOpenChange={(open) => { if (!open) setSplitUnit(null) }}
        gameId={gameId}
        unit={splitUnit}
      />
      <AttackDialog
        open={attackUnit !== null}
        onOpenChange={(open) => { if (!open) setAttackUnit(null) }}
        gameId={gameId}
        unit={attackUnit}
      />
    </div>
  )
}
