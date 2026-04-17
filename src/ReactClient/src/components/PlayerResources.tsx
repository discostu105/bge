import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'
import { formatNumber } from '@/lib/formatters'
import { Stat } from '@/components/ui/stat'
import { ResourceIcon } from '@/components/ui/resource-icon'
import { ScrollArea, ScrollBar } from '@/components/ui/scroll-area'

export function PlayerResources({ gameId }: { gameId: string }) {
  const { data } = useQuery({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get<PlayerResourcesViewModel>('/api/resources').then((r) => r.data),
    refetchInterval: 10_000,
  })
  if (!data) return null

  const all = [
    ...Object.entries(data.primaryResource.cost),
    ...Object.entries(data.secondaryResources.cost),
  ].filter(([, v]) => v !== undefined)

  return (
    <ScrollArea className="max-w-full">
      <div className="flex items-center gap-4 px-1 py-0.5 whitespace-nowrap">
        {all.map(([name, value]) => (
          <Stat
            key={name}
            icon={<ResourceIcon name={name} className="h-4 w-4 text-muted-foreground" />}
            label={<span className="capitalize">{name}</span>}
            value={formatNumber(value)}
          />
        ))}
      </div>
      <ScrollBar orientation="horizontal" />
    </ScrollArea>
  )
}
