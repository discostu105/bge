import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FlaskConicalIcon, GemIcon, UsersIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { WorkerAssignmentViewModel } from '@/api/types'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { NumberStepper } from '@/components/ui/number-stepper'
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

  const [minerals, setMinerals] = useState(0)
  const [gas, setGas] = useState(0)

  useEffect(() => {
    if (data && open) {
      setMinerals(data.mineralWorkers)
      setGas(data.gasWorkers)
    }
  }, [data, open])

  const total = data?.totalWorkers ?? 0
  const idle = Math.max(0, total - minerals - gas)

  const setMin = (v: number) => {
    const clamped = Math.max(0, Math.min(total, v))
    setMinerals(clamped)
    if (clamped + gas > total) setGas(total - clamped)
  }
  const setGas2 = (v: number) => {
    const clamped = Math.max(0, Math.min(total, v))
    setGas(clamped)
    if (minerals + clamped > total) setMinerals(total - clamped)
  }

  const assignMutation = useMutation({
    mutationFn: () =>
      apiClient.post('/api/workers/assign', null, {
        params: { mineralWorkers: minerals, gasWorkers: gas },
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workers', gameId] })
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
      onOpenChange(false)
    },
  })

  const balanced = Math.floor(total / 2)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UsersIcon className="h-5 w-5 text-primary" aria-hidden /> Assign workers
          </DialogTitle>
          <DialogDescription>
            Send workers to mine minerals or harvest gas. Idle workers do nothing.
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-3 gap-3 text-center">
          <div>
            <div className="text-2xl font-semibold text-primary mono">{formatNumber(total)}</div>
            <div className="label">Total</div>
          </div>
          <div>
            <div className="text-2xl font-semibold text-blue-400 mono">{formatNumber(minerals)}</div>
            <div className="label">Minerals</div>
          </div>
          <div>
            <div className="text-2xl font-semibold text-green-400 mono">{formatNumber(gas)}</div>
            <div className="label">Gas</div>
          </div>
        </div>
        <div className={`text-center text-xs ${idle > 0 ? 'text-warning' : 'text-muted-foreground'}`}>
          {idle > 0 ? `${formatNumber(idle)} idle worker${idle === 1 ? '' : 's'}` : 'All workers assigned'}
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <div className="label inline-flex items-center gap-1 mb-1.5">
              <GemIcon className="h-3 w-3 text-blue-400" aria-hidden /> Minerals
            </div>
            <NumberStepper
              value={minerals}
              onChange={setMin}
              min={0}
              max={total}
              step={Math.max(1, Math.floor(total / 20))}
            />
          </div>
          <div>
            <div className="label inline-flex items-center gap-1 mb-1.5">
              <FlaskConicalIcon className="h-3 w-3 text-green-400" aria-hidden /> Gas
            </div>
            <NumberStepper
              value={gas}
              onChange={setGas2}
              min={0}
              max={total}
              step={Math.max(1, Math.floor(total / 20))}
            />
          </div>
        </div>

        <div className="flex flex-wrap gap-1.5">
          <Button variant="ghost" size="xs" onClick={() => { setMinerals(balanced); setGas(total - balanced) }}>
            Balance 50/50
          </Button>
          <Button variant="ghost" size="xs" onClick={() => { setMinerals(total); setGas(0) }}>
            All minerals
          </Button>
          <Button variant="ghost" size="xs" onClick={() => { setMinerals(0); setGas(total) }}>
            All gas
          </Button>
        </div>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={() => assignMutation.mutate()} disabled={assignMutation.isPending}>
            {assignMutation.isPending ? 'Saving…' : 'Save assignment'}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
