import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { GemIcon, FlaskConicalIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { WorkerAssignmentViewModel } from '@/api/types'

interface WorkerAssignmentProps {
  gameId: string
}

export function WorkerAssignment({ gameId }: WorkerAssignmentProps) {
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery<WorkerAssignmentViewModel>({
    queryKey: ['workers', gameId],
    queryFn: () => apiClient.get(`/api/workers`).then((r) => r.data),
    refetchInterval: 30_000,
  })

  const assignMutation = useMutation({
    mutationFn: (params: { mineralWorkers: number; gasWorkers: number }) =>
      apiClient.post('/api/workers/assign', null, { params }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workers', gameId] })
    },
  })

  if (isLoading || !data) {
    return <div className="text-muted-foreground text-sm">Loading workers...</div>
  }

  const handleChange = (type: 'mineral' | 'gas', value: number) => {
    const clamped = Math.max(0, value)
    if (type === 'mineral') {
      const gas = Math.min(data.gasWorkers, data.totalWorkers - clamped)
      assignMutation.mutate({ mineralWorkers: clamped, gasWorkers: gas })
    } else {
      const mineral = Math.min(data.mineralWorkers, data.totalWorkers - clamped)
      assignMutation.mutate({ mineralWorkers: mineral, gasWorkers: clamped })
    }
  }

  return (
    <div className="rounded-lg border bg-card p-4">
      <h3 className="font-semibold mb-3">Worker Assignment</h3>
      <div className="grid grid-cols-3 gap-2 sm:gap-4 text-sm mb-3">
        <div className="text-center">
          <div className="text-2xl font-bold text-primary">{data.totalWorkers}</div>
          <div className="text-muted-foreground">Total</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-blue-400">{data.mineralWorkers}</div>
          <div className="text-muted-foreground inline-flex items-center justify-center gap-1">
            <GemIcon className="h-3.5 w-3.5 text-blue-400" aria-hidden="true" /> Mining
          </div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-green-400">{data.gasWorkers}</div>
          <div className="text-muted-foreground inline-flex items-center justify-center gap-1">
            <FlaskConicalIcon className="h-3.5 w-3.5 text-green-400" aria-hidden="true" /> Gas
          </div>
        </div>
      </div>
      <div className="text-xs text-muted-foreground mb-3">
        Idle: {data.idleWorkers} workers
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="mineralWorkers" className="text-xs text-muted-foreground inline-flex items-center gap-1">
            <GemIcon className="h-3 w-3 text-blue-400" aria-hidden="true" /> Mineral Workers
          </label>
          <input
            id="mineralWorkers"
            type="number"
            min={0}
            max={data.totalWorkers}
            value={data.mineralWorkers}
            onChange={(e) => handleChange('mineral', Number(e.target.value))}
            className="mt-1 w-full rounded border bg-input px-2 py-1 text-sm"
          />
        </div>
        <div>
          <label htmlFor="gasWorkers" className="text-xs text-muted-foreground inline-flex items-center gap-1">
            <FlaskConicalIcon className="h-3 w-3 text-green-400" aria-hidden="true" /> Gas Workers
          </label>
          <input
            id="gasWorkers"
            type="number"
            min={0}
            max={data.totalWorkers}
            value={data.gasWorkers}
            onChange={(e) => handleChange('gas', Number(e.target.value))}
            className="mt-1 w-full rounded border bg-input px-2 py-1 text-sm"
          />
        </div>
      </div>
    </div>
  )
}
