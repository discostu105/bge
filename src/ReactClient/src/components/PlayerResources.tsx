import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'
import { formatNumber } from '@/lib/formatters'

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
  ].filter(([, v]) => v !== undefined) as [string, number][]

  return (
    <div className="flex items-center gap-4 whitespace-nowrap">
      {all.map(([name, value]) => {
        const isLand = name.toLowerCase() === 'land'
        return (
          <div key={name} className="flex items-baseline gap-1.5">
            <span className="label text-[9px]">{name}</span>
            <span
              className={`mono text-[13px] font-semibold ${isLand ? 'text-interactive' : 'text-resource'}`}
              style={isLand ? undefined : { textShadow: '0 0 6px rgba(34,197,94,0.35)' }}
            >
              {formatNumber(value)}
            </span>
          </div>
        )
      })}
    </div>
  )
}
