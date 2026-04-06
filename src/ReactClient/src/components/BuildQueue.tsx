import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { HammerIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { BuildQueueViewModel } from '@/api/types'
import { EmptyState } from '@/components/EmptyState'

interface BuildQueueProps {
  gameId: string
}

export function BuildQueue({ gameId }: BuildQueueProps) {
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery<BuildQueueViewModel>({
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

  if (isLoading) {
    return <div className="text-muted-foreground text-sm">Loading build queue...</div>
  }

  if (!data || data.entries.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <h3 className="font-semibold mb-2">Build Queue</h3>
        <EmptyState
          icon={<HammerIcon />}
          title="Queue is empty"
          description="Queue buildings or units to start constructing."
        />
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card p-4">
      <h3 className="font-semibold mb-3">Build Queue</h3>
      <div className="space-y-2">
        {data.entries
          .slice()
          .sort((a, b) => a.priority - b.priority)
          .map((entry) => (
            <div
              key={entry.id}
              className="flex items-center justify-between rounded border bg-secondary px-3 py-2 text-sm"
            >
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground font-mono text-xs">#{entry.priority}</span>
                <span className="font-medium">{entry.name}</span>
                {entry.count > 1 && (
                  <span className="rounded-full bg-primary/20 px-1.5 py-0.5 text-xs text-primary">
                    ×{entry.count}
                  </span>
                )}
                <span className="text-xs text-muted-foreground capitalize">{entry.type}</span>
              </div>
              <button
                onClick={() => removeMutation.mutate(entry.id)}
                className="rounded min-h-[36px] px-3 py-1.5 text-xs text-destructive hover:bg-destructive/10"
              >
                Remove
              </button>
            </div>
          ))}
      </div>
    </div>
  )
}
