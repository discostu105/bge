import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FlaskConicalIcon, GemIcon, MountainIcon, UsersIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel, WorkerAssignmentViewModel } from '@/api/types'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { formatNumber } from '@/lib/formatters'
import { workerIncome } from '@/lib/economy'

interface WorkerAssignmentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
}

export function WorkerAssignmentDialog({ open, onOpenChange, gameId }: WorkerAssignmentDialogProps) {
  const queryClient = useQueryClient()
  const { data } = useQuery<WorkerAssignmentViewModel>({
    queryKey: ['workers', gameId],
    queryFn: () => apiClient.get('/api/workers').then((r) => r.data),
    enabled: open,
  })

  const { data: resources } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    enabled: open,
  })

  const [gasPercent, setGasPercent] = useState(30)

  useEffect(() => {
    if (data && open) {
      setGasPercent(data.gasPercent)
    }
  }, [data, open])

  const total = data?.totalWorkers ?? 0
  const previewGas = Math.round((total * gasPercent) / 100)
  const previewMinerals = total - previewGas

  const land =
    (resources?.primaryResource.cost.land ?? 0) +
    (resources?.secondaryResources.cost.land ?? 0)
  const formula = resources?.formula
  const mineralsIncome = formula
    ? workerIncome(previewMinerals, land, formula.mineralSweetSpotLandPerWorker, formula)
    : null
  const gasIncome = formula
    ? workerIncome(previewGas, land, formula.gasSweetSpotLandPerWorker, formula)
    : null

  // Tip: warn when a pool with workers is below half its sweet spot. Pools
  // with zero workers don't trigger the tip — they're only producing the flat
  // base income, which is intentional.
  const mineralsBelowHalf =
    formula != null && previewMinerals > 0 &&
    land / previewMinerals < formula.mineralSweetSpotLandPerWorker / 2
  const gasBelowHalf =
    formula != null && previewGas > 0 &&
    land / previewGas < formula.gasSweetSpotLandPerWorker / 2
  const showLowLandTip = mineralsBelowHalf || gasBelowHalf

  const assignMutation = useMutation({
    mutationFn: () =>
      apiClient.post('/api/workers/assign', null, {
        params: { gasPercent },
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workers', gameId] })
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
      onOpenChange(false)
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UsersIcon className="h-5 w-5 text-primary" aria-hidden /> Worker auto-assignment
          </DialogTitle>
          <DialogDescription>
            Workers are split automatically each tick. Pick the percentage you want gathering gas — the rest go to minerals.
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-3 gap-3 text-center">
          <div>
            <div className="text-2xl font-semibold text-primary mono">{formatNumber(total)}</div>
            <div className="label">Total</div>
          </div>
          <div>
            <div className="text-2xl font-semibold text-blue-400 mono">{formatNumber(previewMinerals)}</div>
            <div className="label inline-flex items-center justify-center gap-1">
              <GemIcon className="h-3 w-3 text-blue-400" aria-hidden /> Minerals
            </div>
          </div>
          <div>
            <div className="text-2xl font-semibold text-green-400 mono">{formatNumber(previewGas)}</div>
            <div className="label inline-flex items-center justify-center gap-1">
              <FlaskConicalIcon className="h-3 w-3 text-green-400" aria-hidden /> Gas
            </div>
          </div>
        </div>

        <div>
          <div className="label mb-1.5 flex items-center justify-between">
            <span>Gas share</span>
            <span className="mono text-foreground">{gasPercent}%</span>
          </div>
          <input
            type="range"
            min={0}
            max={100}
            step={1}
            value={gasPercent}
            onChange={(e) => setGasPercent(Number(e.target.value))}
            className="w-full"
          />
        </div>

        {formula && (
          <div className="rounded-md border border-success/20 bg-success/5 px-3 py-2 text-xs space-y-1">
            <div className="label flex items-center justify-between">
              <span>Income next tick</span>
              <span className="text-muted-foreground inline-flex items-center gap-1">
                <MountainIcon className="h-3 w-3 text-[#b8804a]" aria-hidden /> Land {formatNumber(land)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="inline-flex items-center gap-1">
                <GemIcon className="h-3 w-3 text-blue-400" aria-hidden /> Minerals
              </span>
              <span className="mono font-semibold text-success">+{formatNumber(mineralsIncome ?? 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="inline-flex items-center gap-1">
                <FlaskConicalIcon className="h-3 w-3 text-green-400" aria-hidden /> Gas
              </span>
              <span className="mono font-semibold text-success">+{formatNumber(gasIncome ?? 0)}</span>
            </div>
            {showLowLandTip && (
              <div className="text-muted-foreground pt-1">
                Workers are choking on too little land — colonize more to boost income.
              </div>
            )}
          </div>
        )}

        <div className="flex flex-wrap gap-1.5">
          <Button variant="ghost" size="xs" onClick={() => setGasPercent(50)}>
            Balance 50/50
          </Button>
          <Button variant="ghost" size="xs" onClick={() => setGasPercent(0)}>
            All minerals
          </Button>
          <Button variant="ghost" size="xs" onClick={() => setGasPercent(100)}>
            All gas
          </Button>
        </div>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={() => assignMutation.mutate()} disabled={assignMutation.isPending}>
            {assignMutation.isPending ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
