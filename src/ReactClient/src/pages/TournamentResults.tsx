import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { TournamentResultsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function TournamentResults() {
  const { tournamentId } = useParams<{ tournamentId: string }>()

  const { data: model, error, isLoading, refetch } = useQuery<TournamentResultsViewModel>({
    queryKey: ['tournament-results', tournamentId],
    queryFn: () => apiClient.get(`/api/tournaments/${tournamentId}/results`).then((r) => r.data),
    enabled: !!tournamentId,
  })

  if (isLoading) return <PageLoader message="Loading tournament results..." />
  if (error) return <ApiError message="Failed to load tournament results." onRetry={() => void refetch()} />
  if (!model) return null

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Tournament: {model.tournamentId}</h1>
        <p className="text-muted-foreground text-sm mt-1">{model.totalGames} game{model.totalGames !== 1 ? 's' : ''}</p>
      </div>

      {model.rankings.length === 0 ? (
        <p className="text-muted-foreground">No results yet — games may still be in progress.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th className="pb-2 pr-4">Rank</th>
                <th className="pb-2 pr-4">Player</th>
                <th className="pb-2 pr-4">Games</th>
                <th className="pb-2 pr-4">Wins</th>
                <th className="pb-2">Land</th>
              </tr>
            </thead>
            <tbody>
              {model.rankings.map((entry) => (
                <tr
                  key={entry.userId ?? entry.playerName}
                  className={`border-b border-border ${entry.rank === 1 ? 'bg-warning/10 font-semibold' : ''}`}
                >
                  <td className="py-2 pr-4">
                    {entry.rank === 1 ? `🏆 #1` : `#${entry.rank}`}
                  </td>
                  <td className="py-2 pr-4">{entry.playerName}</td>
                  <td className="py-2 pr-4">{entry.gamesPlayed}</td>
                  <td className="py-2 pr-4">{entry.wins}</td>
                  <td className="py-2">{entry.totalLand.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
