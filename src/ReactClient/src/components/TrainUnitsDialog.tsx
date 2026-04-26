import { useMemo, useState, useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { HammerIcon, PlusCircleIcon, SwordsIcon, LockIcon, CheckCircle2Icon, BuildingIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  AssetsViewModel,
  AssetViewModel,
  PlayerResourcesViewModel,
  UnitDefinitionViewModel,
  AddToQueueRequest,
  CostViewModel,
} from '@/api/types'
import { formatNumber } from '@/lib/formatters'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { NumberStepper } from '@/components/ui/number-stepper'
import { Stat } from '@/components/ui/stat'
import { ScrollArea } from '@/components/ui/scroll-area'
import { CostBadge } from '@/components/CostBadge'
import { ResourceIcon } from '@/components/ui/resource-icon'
import { cn } from '@/lib/utils'

interface TrainUnitsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
  /** If provided, focuses the dialog on this building. Otherwise, browses all units across all production buildings. */
  asset?: AssetViewModel | null
}

interface UnitWithBuilding {
  unit: UnitDefinitionViewModel
  building: AssetViewModel
}

const PRESETS = [1, 5, 10, 25, 50]

function totalCost(unitCost: CostViewModel | undefined, count: number): Record<string, number> {
  if (!unitCost) return {}
  const result: Record<string, number> = {}
  for (const [k, v] of Object.entries(unitCost.cost)) {
    result[k] = v * Math.max(0, count)
  }
  return result
}

function canAffordTotal(total: Record<string, number>, haves: Record<string, number>): boolean {
  for (const [k, need] of Object.entries(total)) {
    if ((haves[k] ?? 0) < need) return false
  }
  return true
}

function maxAffordable(unitCost: CostViewModel | undefined, haves: Record<string, number>): number {
  if (!unitCost) return 0
  const entries = Object.entries(unitCost.cost).filter(([, v]) => v > 0)
  if (entries.length === 0) return 99999
  return entries.reduce((best, [k, perUnit]) => {
    const have = haves[k] ?? 0
    const fit = Math.floor(have / perUnit)
    return Math.min(best, fit)
  }, Number.POSITIVE_INFINITY) | 0
}

function combineHaves(resources: PlayerResourcesViewModel | undefined): Record<string, number> {
  if (!resources) return {}
  return {
    ...resources.primaryResource.cost,
    ...resources.secondaryResources.cost,
  }
}

export function TrainUnitsDialog({ open, onOpenChange, gameId, asset }: TrainUnitsDialogProps) {
  const queryClient = useQueryClient()
  const [selectedUnitId, setSelectedUnitId] = useState<string | null>(null)
  const [count, setCount] = useState(1)
  const [error, setError] = useState<string | null>(null)

  // Always fetch assets so we can show the catalog (and look up building info even in focused mode).
  const { data: assetsData } = useQuery<AssetsViewModel>({
    queryKey: ['assets', gameId],
    queryFn: () => apiClient.get('/api/assets').then((r) => r.data),
    enabled: open,
  })

  const { data: resources } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    enabled: open,
    refetchInterval: open ? 10_000 : false,
  })

  // Production buildings — those that train any unit. Built ones first, then unbuilt (so users see what's locked).
  const productionBuildings = useMemo<AssetViewModel[]>(() => {
    const source = asset ? [asset] : (assetsData?.assets ?? [])
    return source
      .filter((a) => a.availableUnits.length > 0)
      .slice()
      .sort((a, b) => {
        if (a.built !== b.built) return a.built ? -1 : 1
        return a.definition.name.localeCompare(b.definition.name)
      })
  }, [asset, assetsData])

  // Flat catalog: all units across all production buildings, each tagged with its required building.
  const catalog = useMemo<UnitWithBuilding[]>(() => {
    const result: UnitWithBuilding[] = []
    for (const b of productionBuildings) {
      for (const u of b.availableUnits) result.push({ unit: u, building: b })
    }
    return result
  }, [productionBuildings])

  const selectedEntry = useMemo<UnitWithBuilding | undefined>(() => {
    if (catalog.length === 0) return undefined
    const found = catalog.find(({ unit }) => unit.id === selectedUnitId)
    if (found) return found
    // Default: first trainable unit (building built + prereqs met), else first.
    return catalog.find(({ unit, building }) => building.built && unit.prerequisitesMet) ?? catalog[0]
  }, [catalog, selectedUnitId])

  const selectedUnit = selectedEntry?.unit
  const selectedBuilding = selectedEntry?.building

  // Reset state when dialog opens or asset changes.
  useEffect(() => {
    if (!open) {
      setError(null)
      setCount(1)
      return
    }
    setSelectedUnitId(null)
    setCount(1)
    setError(null)
  }, [open, asset])

  const haves = combineHaves(resources)
  const perUnitCost = selectedUnit?.cost
  const total = useMemo(() => totalCost(perUnitCost, count), [perUnitCost, count])
  const affordable = useMemo(() => canAffordTotal(total, haves), [total, haves])
  const maxCount = useMemo(() => Math.max(1, maxAffordable(perUnitCost, haves)), [perUnitCost, haves])

  const trainableNow = !!selectedUnit
    && !!selectedBuilding?.built
    && !!selectedUnit.prerequisitesMet
    && affordable

  const trainMutation = useMutation({
    mutationFn: () => {
      if (!selectedUnit) throw new Error('No unit selected')
      return apiClient.post(
        `/api/units/build?unitDefId=${selectedUnit.id}&count=${count}`,
        '',
      )
    },
    onSuccess: () => {
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
      onOpenChange(false)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Train failed')
      else setError('Train failed')
    },
  })

  const queueMutation = useMutation({
    mutationFn: () => {
      if (!selectedUnit) throw new Error('No unit selected')
      const request: AddToQueueRequest = { type: 'unit', defId: selectedUnit.id, count }
      return apiClient.post('/api/buildqueue/add', request)
    },
    onSuccess: () => {
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
      onOpenChange(false)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Queue failed')
      else setError('Queue failed')
    },
  })

  const working = trainMutation.isPending || queueMutation.isPending

  const description = asset
    ? `Build units at your ${asset.definition.name}.`
    : 'Pick any unit. The required building is shown beside each one.'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <SwordsIcon className="h-5 w-5 text-primary" aria-hidden /> Train units
          </DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        {/* Catalog */}
        {productionBuildings.length === 0 ? (
          <div className="rounded-md border border-warning/40 bg-warning/10 p-3 text-sm">
            No production buildings are defined for your race.
          </div>
        ) : (
          <ScrollArea className="max-h-[42vh] -mx-1 px-1">
            <div className="space-y-3">
              {productionBuildings.map((b) => (
                <BuildingGroup
                  key={b.definition.id}
                  building={b}
                  selectedUnitId={selectedEntry?.unit.id ?? null}
                  haves={haves}
                  onSelect={(id) => setSelectedUnitId(id)}
                />
              ))}
            </div>
          </ScrollArea>
        )}

        {/* Selected unit details + count + actions */}
        {selectedUnit && selectedBuilding && (
          <div className="space-y-3 border-t pt-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium">{selectedUnit.name}</span>
                <BuildingPill building={selectedBuilding} />
              </div>
              <div className="flex flex-wrap items-center gap-x-5 gap-y-1">
                <Stat label="Atk" value={selectedUnit.attack} />
                <Stat label="Def" value={selectedUnit.defense} />
                <Stat label="HP" value={selectedUnit.hitpoints} />
                <Stat label="Speed" value={selectedUnit.speed} />
              </div>
            </div>

            {!selectedBuilding.built && (
              <div className="rounded-md border border-warning/40 bg-warning/10 p-2 text-xs">
                <span className="font-medium">{selectedBuilding.definition.name}</span> is not built yet.
                You can still queue this unit; it will train once the building is ready.
              </div>
            )}
            {selectedBuilding.built && !selectedUnit.prerequisitesMet && (
              <div className="rounded-md border border-warning/40 bg-warning/10 p-2 text-xs">
                Prerequisites for {selectedUnit.name} are not met. The unit can be queued; it will train once
                requirements are satisfied.
              </div>
            )}

            <div>
              <div className="label mb-1.5">Count</div>
              <NumberStepper
                value={count}
                onChange={setCount}
                min={1}
                max={10000}
                presets={PRESETS}
              />
              <div className="mt-2 flex items-center gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  onClick={() => setCount(Math.max(1, maxCount))}
                  title="Fill to maximum your resources allow"
                  disabled={maxCount <= 0 || !Number.isFinite(maxCount)}
                >
                  Max affordable ({formatNumber(Math.max(1, maxCount))})
                </Button>
              </div>
            </div>

            <div className={cn(
              'rounded-md border px-3 py-2 flex items-center justify-between gap-3',
              affordable ? 'border-success/40 bg-success/5' : 'border-destructive/40 bg-destructive/5',
            )}>
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Total</span>
                <span className="flex flex-wrap items-center gap-3 mono">
                  {Object.entries(total).filter(([, v]) => v > 0).map(([k, v]) => (
                    <span key={k} className="inline-flex items-center gap-1">
                      <ResourceIcon name={k} /> {formatNumber(v)}
                    </span>
                  ))}
                </span>
              </div>
              <Badge variant={affordable ? 'secondary' : 'destructive'}>
                {affordable ? 'Affordable' : 'Short on resources'}
              </Badge>
            </div>
          </div>
        )}

        {error && <div className="text-sm text-destructive">{error}</div>}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={working}>
            Cancel
          </Button>
          <Button
            variant="secondary"
            onClick={() => queueMutation.mutate()}
            disabled={!selectedUnit || count < 1 || working}
            title="Queue this unit; it will train when prerequisites and resources are available."
          >
            <PlusCircleIcon className="h-4 w-4" /> Add to queue
          </Button>
          <Button
            onClick={() => trainMutation.mutate()}
            disabled={!trainableNow || count < 1 || working}
            title={
              !selectedUnit ? 'Select a unit'
                : !selectedBuilding?.built ? `Build a ${selectedBuilding?.definition.name} first — try "Add to queue"`
                : !selectedUnit.prerequisitesMet ? 'Prerequisites not met — try "Add to queue"'
                : !affordable ? 'Not enough resources — try "Add to queue"'
                : 'Train now'
            }
          >
            <HammerIcon className="h-4 w-4" />
            {working ? 'Training…' : `Train ${formatNumber(count)}`}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

function BuildingGroup({
  building,
  selectedUnitId,
  haves,
  onSelect,
}: {
  building: AssetViewModel
  selectedUnitId: string | null
  haves: Record<string, number>
  onSelect: (unitId: string) => void
}) {
  return (
    <div className="rounded-md border bg-card">
      <div className="flex items-center justify-between gap-2 border-b px-3 py-1.5">
        <div className="flex items-center gap-2 min-w-0">
          <BuildingIcon className="h-4 w-4 text-muted-foreground shrink-0" aria-hidden />
          <span className="font-medium truncate">{building.definition.name}</span>
        </div>
        {building.built ? (
          <Badge variant="secondary" className="gap-1">
            <CheckCircle2Icon className="h-3 w-3" /> Built
          </Badge>
        ) : (
          <Badge variant="outline" className="gap-1 text-warning border-warning/40">
            <LockIcon className="h-3 w-3" /> Build to unlock
          </Badge>
        )}
      </div>
      <div className="grid gap-1.5 p-2 sm:grid-cols-2">
        {building.availableUnits.map((u) => (
          <UnitCard
            key={u.id}
            unit={u}
            building={building}
            selected={selectedUnitId === u.id}
            affordable={canAffordTotal(totalCost(u.cost, 1), haves)}
            onSelect={() => onSelect(u.id)}
          />
        ))}
      </div>
    </div>
  )
}

function UnitCard({
  unit,
  building,
  selected,
  affordable,
  onSelect,
}: {
  unit: UnitDefinitionViewModel
  building: AssetViewModel
  selected: boolean
  affordable: boolean
  onSelect: () => void
}) {
  const buildable = building.built && unit.prerequisitesMet
  const blockedReason = !building.built
    ? `Requires ${building.definition.name}`
    : !unit.prerequisitesMet
      ? 'Prerequisites not met'
      : null
  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        'flex flex-col gap-1 rounded-md border px-2.5 py-2 text-left transition-colors',
        'hover:bg-accent hover:text-accent-foreground',
        selected ? 'border-primary ring-1 ring-primary bg-accent/30' : 'border-border',
        !buildable && !selected && 'opacity-90',
      )}
      title={blockedReason ?? `Train ${unit.name}`}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="font-medium truncate">{unit.name}</span>
        {!buildable && (
          <LockIcon className="h-3.5 w-3.5 shrink-0 text-warning" aria-hidden />
        )}
      </div>
      <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-muted-foreground mono">
        <span>Atk {unit.attack}</span>
        <span>Def {unit.defense}</span>
        <span>HP {unit.hitpoints}</span>
      </div>
      <div className="flex items-center justify-between gap-2 mt-0.5">
        <CostBadge
          cost={unit.cost}
          className={cn('text-xs', !affordable && 'text-destructive')}
        />
      </div>
      {blockedReason && (
        <span className="text-xs text-warning">{blockedReason}</span>
      )}
    </button>
  )
}

function BuildingPill({ building }: { building: AssetViewModel }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs',
        building.built
          ? 'border-success/40 bg-success/10 text-foreground'
          : 'border-warning/40 bg-warning/10 text-foreground',
      )}
      title={building.built ? `${building.definition.name} is built` : `${building.definition.name} not built`}
    >
      <BuildingIcon className="h-3 w-3" aria-hidden />
      {building.definition.name}
      {building.built ? (
        <CheckCircle2Icon className="h-3 w-3 text-success" aria-hidden />
      ) : (
        <LockIcon className="h-3 w-3 text-warning" aria-hidden />
      )}
    </span>
  )
}
