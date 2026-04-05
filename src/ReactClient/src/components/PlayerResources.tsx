import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'

function formatNumber(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
  if (value >= 10_000) return `${(value / 1_000).toFixed(1)}K`
  return Math.floor(value).toLocaleString()
}

function ResourceEntry({ name, value }: { name: string; value: number }) {
  return (
    <span className="flex items-center gap-1">
      <strong className="capitalize text-xs">{name}</strong>
      <span className="font-mono">{formatNumber(value)}</span>
    </span>
  )
}

export function PlayerResources({ gameId }: { gameId: string }) {
  const { data: resources } = useQuery({
    queryKey: ['resources', gameId],
    queryFn: () =>
      apiClient.get<PlayerResourcesViewModel>('/api/resources').then((r) => r.data),
    refetchInterval: 30_000,
  })

  if (!resources) return null

  return (
    <div className="flex items-center gap-2 sm:gap-4 text-xs sm:text-sm whitespace-nowrap">
      {Object.entries(resources.primaryResource.cost).map(([name, value]) => (
        <ResourceEntry key={name} name={name} value={value} />
      ))}
      {Object.entries(resources.secondaryResources.cost).map(([name, value]) => (
        <ResourceEntry key={name} name={name} value={value} />
      ))}
    </div>
  )
}
