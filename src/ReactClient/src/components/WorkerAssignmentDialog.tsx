import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FlaskConicalIcon, GemIcon, UsersIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { WorkerAssignmentViewModel } from '@/api/types'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { formatNumber } from '@/lib/formatters'

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

  const [gasPercent, setGasPercent] = useState(30)

  useEffect(() => {
    if (data && open) {
      setGasPercent(data.gasPercent)
    }
  }, [data, open])

  const total = data?.totalWorkers ?? 0
  const previewGas = Math.round((total * gasPercent) / 100)
  const previewMinerals = total - previewGas

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
