import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { AllianceRankingViewModel } from '@/api/types'

interface AllianceRankingProps {
  gameId: string
}

export function AllianceRanking({ gameId }: AllianceRankingProps) {
  const { data: ranking } = useQuery<AllianceRankingViewModel[]>({
    queryKey: ['allianceranking', gameId],
    queryFn: () => apiClient.get('/api/allianceranking').then((r) => r.data),
    refetchInterval: 10_000,
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Alliance Ranking</h1>

      {!ranking ? (
        <div className="text-muted-foreground text-sm">Loading...</div>
      ) : ranking.length === 0 ? (
        <div className="text-muted-foreground text-sm">
          No alliances with 2 or more members yet.
        </div>
      ) : (
        <div className="rounded-lg border bg-card overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b">
              <tr>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">#</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Members</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Total Land</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Avg Land</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Score</th>
              </tr>
            </thead>
            <tbody>
              {ranking.map((item, i) => (
                <tr key={item.allianceId} className="border-b border-border">
                  <td className="py-2 px-3 text-muted-foreground">{i + 1}</td>
                  <td className="py-2 px-3 font-medium">
                    <a
                      href={`/alliances/${item.allianceId}`}
                      className="text-primary hover:underline"
                    >
                      {item.name}
                    </a>
                  </td>
                  <td className="py-2 px-3">{item.memberCount}</td>
                  <td className="py-2 px-3">{item.totalLand.toLocaleString()}</td>
                  <td className="py-2 px-3">{item.avgLand.toLocaleString()}</td>
                  <td className="py-2 px-3">{item.score.toFixed(1)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
