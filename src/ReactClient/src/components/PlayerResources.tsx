import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'

export function PlayerResources({ gameId }: { gameId: string }) {
  const { data: resources } = useQuery({
    queryKey: ['resources', gameId],
    queryFn: () =>
      apiClient.get<PlayerResourcesViewModel>('/api/resources').then((r) => r.data),
    refetchInterval: 30_000,
  })

  if (!resources) return null

  return (
    <div className="flex items-center gap-4 text-sm">
      <span className="flex items-center gap-1">
        <span className="text-blue-400">💎</span>
        <span className="font-mono">{Math.floor(resources.minerals).toLocaleString()}</span>
        <span className="text-muted-foreground text-xs">min</span>
      </span>
      <span className="flex items-center gap-1">
        <span className="text-green-400">⛽</span>
        <span className="font-mono">{Math.floor(resources.gas).toLocaleString()}</span>
        <span className="text-muted-foreground text-xs">gas</span>
      </span>
      <span className="flex items-center gap-1">
        <span className="text-yellow-400">🏘️</span>
        <span className="font-mono">{resources.land}</span>
        <span className="text-muted-foreground text-xs">land</span>
      </span>
    </div>
  )
}
