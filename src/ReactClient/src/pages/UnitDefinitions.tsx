import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { UnitDefinitionViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function UnitDefinitions() {
  const { data: units, isLoading, error, refetch } = useQuery<UnitDefinitionViewModel[]>({
    queryKey: ['unit-definitions'],
    queryFn: () => apiClient.get('/api/unitdefinitions').then((r) => r.data),
  })

  if (isLoading) return <PageLoader message="Loading unit definitions..." />
  if (error) return <ApiError message="Failed to load unit definitions." onRetry={() => void refetch()} />

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Unit Definitions</h1>
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b border-border text-left text-muted-foreground">
              <th className="py-2 pr-4">ID</th>
              <th className="py-2 pr-4">Name</th>
              <th className="py-2 pr-4">Attack</th>
              <th className="py-2 pr-4">Defense</th>
              <th className="py-2 pr-4">HP</th>
              <th className="py-2 pr-4">Speed</th>
              <th className="py-2">Cost</th>
            </tr>
          </thead>
          <tbody>
            {(units ?? []).map((unit) => (
              <tr key={unit.id} className="border-b border-border hover:bg-muted/30">
                <td className="py-2 pr-4 text-muted-foreground font-mono text-xs">{unit.id}</td>
                <td className="py-2 pr-4 font-medium">{unit.name}</td>
                <td className="py-2 pr-4">{unit.attack}</td>
                <td className="py-2 pr-4">{unit.defense}</td>
                <td className="py-2 pr-4">{unit.hitpoints}</td>
                <td className="py-2 pr-4">{unit.speed}</td>
                <td className="py-2 text-muted-foreground text-xs">
                  {Object.entries(unit.cost.cost)
                    .map(([k, v]) => `${v} ${k}`)
                    .join(', ')}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
