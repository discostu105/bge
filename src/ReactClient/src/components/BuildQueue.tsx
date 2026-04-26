import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { XIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { BuildQueueViewModel } from '@/api/types'
import { useConfirm } from '@/contexts/ConfirmContext'

interface BuildQueueProps {
  gameId: string
}

export function BuildQueue({ gameId }: BuildQueueProps) {
  const queryClient = useQueryClient()
  const confirm = useConfirm()

  const { data } = useQuery<BuildQueueViewModel>({
    queryKey: ['buildqueue', gameId],
    queryFn: () => apiClient.get('/api/buildqueue').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const removeMutation = useMutation({
    mutationFn: (entryId: string) =>
      apiClient.delete(`/api/buildqueue/remove?entryId=${entryId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
    },
  })

  if (!data || data.entries.length === 0) {
    return null
  }

  return (
    <div>
      <div className="label mb-1.5">
        Build Queue <span className="text-muted-foreground">({data.entries.length})</span>
      </div>
      <div className="flex flex-wrap gap-1.5">
        {data.entries
          .slice()
          .sort((a, b) => a.priority - b.priority)
          .map((entry) => (
            <div
              key={entry.id}
              className="inline-flex items-center gap-1.5 rounded-full border bg-secondary/50 px-2.5 py-1 text-xs"
            >
              <span className="font-mono text-muted-foreground">#{entry.priority}</span>
              <span className="font-medium">{entry.name}</span>
              {entry.count > 1 && (
                <span className="text-primary">×{entry.count}</span>
              )}
              <button
                onClick={async () => {
                  const ok = await confirm({
                    title: 'Remove from build queue?',
                    description: `“${entry.name}” will be removed from the queue. Any resources already reserved will be refunded.`,
                    destructive: true,
                    confirmLabel: 'Remove',
                    cancelLabel: 'Keep',
                  })
                  if (ok) removeMutation.mutate(entry.id)
                }}
                className="ml-0.5 rounded-full p-0.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                aria-label={`Remove ${entry.name}`}
              >
                <XIcon className="h-3 w-3" />
              </button>
            </div>
          ))}
      </div>
    </div>
  )
}
