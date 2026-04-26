import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'
import { formatNumber } from '@/lib/formatters'
import { ResourceIcon } from '@/components/ui/resource-icon'
import { resourceHex, resourceTextClass } from '@/lib/resource-colors'

const GLOWS: Record<string, string> = {
  minerals: '0 0 6px rgba(96,165,250,0.45)',
  gas: '0 0 6px rgba(74,222,128,0.45)',
  land: '0 0 6px rgba(184,128,74,0.45)',
}

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
        const key = name.toLowerCase()
        return (
          <div key={name} className="flex items-baseline gap-1.5">
            <ResourceIcon name={name} className="self-center" />
            <span className={`label text-[9px] ${resourceTextClass(name)}`}>{name}</span>
            <span
              className={`mono text-[13px] font-semibold ${resourceTextClass(name)}`}
              style={{ color: resourceHex(name), textShadow: GLOWS[key] }}
            >
              {formatNumber(value)}
            </span>
          </div>
        )
      })}
    </div>
  )
}
