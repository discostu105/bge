import { useMemo, useState, useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { HammerIcon, PlusCircleIcon, SwordsIcon } from 'lucide-react'
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
import { AffordabilityChip } from '@/components/AffordabilityChip'
import { ResourceIcon } from '@/components/ui/resource-icon'
import { cn } from '@/lib/utils'

interface TrainUnitsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
  /** If provided, pre-selects this building. Otherwise, a picker is shown for built production buildings. */
  asset?: AssetViewModel | null
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
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(asset?.definition.id ?? null)
  const [selectedUnitId, setSelectedUnitId] = useState<string | null>(null)
  const [count, setCount] = useState(1)
  const [error, setError] = useState<string | null>(null)

  // Fetch all assets when opened in "picker" mode, so the user can choose a building.
  const { data: assetsData } = useQuery<AssetsViewModel>({
    queryKey: ['assets', gameId],
    queryFn: () => apiClient.get('/api/assets').then((r) => r.data),
    enabled: open && !asset,
  })

  const { data: resources } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    enabled: open,
    refetchInterval: open ? 10_000 : false,
  })

  const builtProducers = useMemo<AssetViewModel[]>(() => {
    if (asset) return [asset]
    if (!assetsData) return []
    return assetsData.assets.filter((a) => a.built && a.availableUnits.length > 0)
  }, [asset, assetsData])

  const selectedAsset: AssetViewModel | undefined = useMemo(() => {
    if (asset) return asset
    return builtProducers.find((a) => a.definition.id === selectedAssetId) ?? builtProducers[0]
  }, [asset, builtProducers, selectedAssetId])

  const availableUnits: UnitDefinitionViewModel[] = selectedAsset?.availableUnits ?? []

  const selectedUnit = useMemo<UnitDefinitionViewModel | undefined>(() => {
    if (availableUnits.length === 0) return undefined
    return availableUnits.find((u) => u.id === selectedUnitId) ?? availableUnits[0]
  }, [availableUnits, selectedUnitId])

  // Reset state when dialog opens / asset changes.
  useEffect(() => {
    if (!open) {
      setError(null)
      setCount(1)
      return
    }
    setSelectedAssetId(asset?.definition.id ?? builtProducers[0]?.definition.id ?? null)
  }, [open, asset, builtProducers])

  useEffect(() => {
    if (availableUnits.length === 0) {
      setSelectedUnitId(null)
      return
    }
    if (!selectedUnitId || !availableUnits.find((u) => u.id === selectedUnitId)) {
      setSelectedUnitId(availableUnits[0].id)
    }
  }, [availableUnits, selectedUnitId])

  const haves = combineHaves(resources)
  const perUnitCost = selectedUnit?.cost
  const total = useMemo(() => totalCost(perUnitCost, count), [perUnitCost, count])
  const affordable = useMemo(() => canAffordTotal(total, haves), [total, haves])
  const maxCount = useMemo(() => Math.max(1, maxAffordable(perUnitCost, haves)), [perUnitCost, haves])

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

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <SwordsIcon className="h-5 w-5 text-primary" aria-hidden /> Train units
          </DialogTitle>
          <DialogDescription>
            {asset
              ? `Build units at your ${selectedAsset?.definition.name ?? 'building'}.`
              : 'Pick a production building and a unit type.'}
          </DialogDescription>
        </DialogHeader>

        {/* Building picker (only if no asset was pre-selected) */}
        {!asset && (
          builtProducers.length === 0 ? (
            <div className="rounded-md border border-warning/40 bg-warning/10 p-3 text-sm">
              You have no production buildings yet. Build a Barracks, Gateway, or similar to start training units.
            </div>
          ) : builtProducers.length > 1 ? (
            <div>
              <div className="label mb-1.5">Building</div>
              <div className="flex flex-wrap gap-1.5">
                {builtProducers.map((a) => (
                  <Button
                    key={a.definition.id}
                    type="button"
                    size="sm"
                    variant={selectedAssetId === a.definition.id ? 'default' : 'outline'}
                    onClick={() => { setSelectedAssetId(a.definition.id); setSelectedUnitId(null) }}
                  >
                    {a.definition.name}
                  </Button>
                ))}
              </div>
            </div>
          ) : null
        )}

        {selectedAsset && availableUnits.length === 0 && (
          <div className="rounded-md border border-warning/40 bg-warning/10 p-3 text-sm">
            {selectedAsset.definition.name} does not train any units directly.
          </div>
        )}

        {/* Unit picker */}
        {selectedAsset && availableUnits.length > 0 && (
          <div>
            <div className="label mb-1.5">Unit</div>
            <div className="flex flex-wrap gap-1.5">
              {availableUnits.map((u) => (
                <Button
                  key={u.id}
                  type="button"
                  size="sm"
                  variant={selectedUnit?.id === u.id ? 'default' : 'outline'}
                  onClick={() => setSelectedUnitId(u.id)}
                  disabled={!u.prerequisitesMet}
                  title={!u.prerequisitesMet ? 'Prerequisites not met' : undefined}
                >
                  {u.name}
                </Button>
              ))}
            </div>
          </div>
        )}

        {/* Selected unit stats + per-unit cost */}
        {selectedUnit && (
          <div className="rounded-md border bg-card p-3">
            <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
              <Stat label="Atk" value={selectedUnit.attack} />
              <Stat label="Def" value={selectedUnit.defense} />
              <Stat label="HP" value={selectedUnit.hitpoints} />
              <Stat label="Speed" value={selectedUnit.speed} />
            </div>
            <div className="mt-2 flex items-center justify-between gap-2 text-xs">
              <span className="text-muted-foreground">Per unit</span>
              <AffordabilityChip cost={selectedUnit.cost} affordable={canAffordTotal(totalCost(selectedUnit.cost, 1), haves)} />
            </div>
            {!selectedUnit.prerequisitesMet && (
              <div className="mt-2 text-xs text-warning">
                Prerequisites not met. Unit can be queued; it will train once requirements are satisfied.
              </div>
            )}
          </div>
        )}

        {/* Count + presets */}
        {selectedUnit && (
          <div>
            <div className="label mb-1.5">Count</div>
            <NumberStepper
              value={count}
              onChange={setCount}
              min={1}
              max={10000}
              presets={PRESETS}
              showMax={maxCount > 0 && Number.isFinite(maxCount)}
              // Replace showMax max handling via injecting "max" preset manually;
              // stepper's showMax uses the raw max which is 10000. Use presets to embed max-affordable.
            />
            <div className="mt-2 flex items-center gap-2">
              <Button
                type="button"
                variant="ghost"
                size="xs"
                onClick={() => setCount(Math.max(1, maxCount))}
                title="Fill to maximum your resources allow"
              >
                Max affordable ({formatNumber(Math.max(1, maxCount))})
              </Button>
            </div>
          </div>
        )}

        {/* Total cost preview */}
        {selectedUnit && (
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
          >
            <PlusCircleIcon className="h-4 w-4" /> Add to queue
          </Button>
          <Button
            onClick={() => trainMutation.mutate()}
            disabled={!selectedUnit || count < 1 || !affordable || working}
            title={!affordable ? 'Not enough resources — try "Add to queue" instead' : 'Train now'}
          >
            <HammerIcon className="h-4 w-4" />
            {working ? 'Training…' : `Train ${formatNumber(count)}`}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
