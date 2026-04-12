import { useParams, Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameResultsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { formatDuration } from '@/lib/formatters'

export function GameSummary() {
  const { gameId } = useParams<{ gameId: string }>()

  const { data: model, error, isLoading, refetch } = useQuery<GameResultsViewModel>({
    queryKey: ['game-results', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/results`).then((r) => r.data),
    enabled: !!gameId,
  })

  if (isLoading) return <PageLoader message="Loading game summary..." />
  if (error) return <ApiError message="Failed to load game summary." onRetry={() => void refetch()} />
  if (!model) return <ApiError message="No summary available for this game." />

  const end = new Date(model.actualEndTime ?? model.endTime)
  const start = new Date(model.startTime)
  const duration = formatDuration(end.getTime() - start.getTime())
  const winner = model.standings.find((s) => s.isWinner)

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">🏆 Game Over: {model.name}</h1>
      <p className="text-muted-foreground text-sm">
        Ended {end.toLocaleDateString()} {end.toLocaleTimeString()} · Duration: {duration}
      </p>

      {winner && (
        <div className="flex items-center gap-4 rounded-lg border border-warning/50 bg-warning/10 p-4">
          <span className="text-4xl">🏆</span>
          <div>
            <div className="font-bold text-lg">{winner.playerName} wins!</div>
            <div className="text-muted-foreground text-sm">Final Land: {winner.land.toLocaleString()}</div>
          </div>
        </div>
      )}

      <div>
        <h2 className="text-lg font-semibold mb-3">Final Standings</h2>
        {model.standings.length === 0 ? (
          <p className="text-muted-foreground text-sm">No standings recorded for this game.</p>
        ) : (
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th scope="col" className="py-2 pr-4">Rank</th>
                <th scope="col" className="py-2 pr-4">Player</th>
                <th scope="col" className="py-2">Land</th>
              </tr>
            </thead>
            <tbody>
              {model.standings.map((entry) => (
                <tr
                  key={entry.playerId}
                  className={`border-b border-border ${entry.isWinner ? 'bg-yellow-900/20 font-semibold' : ''}`}
                >
                  <td className="py-2 pr-4">
                    {entry.isWinner ? '🏆 1' : `#${entry.rank}`}
                  </td>
                  <td className="py-2 pr-4">{entry.playerName}</td>
                  <td className="py-2">{entry.land.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="flex gap-3 flex-wrap">
        <Link to="/games" className="text-sm px-4 py-2 rounded bg-success text-success-foreground hover:opacity-90">
          Join Next Game
        </Link>
        <Link to="/games" className="text-sm px-4 py-2 rounded border border-border hover:bg-muted">
          Back to Games List
        </Link>
        <Link to={`/games/${gameId}/results`} className="text-sm px-4 py-2 rounded border border-border hover:bg-muted">
          Detailed Results
        </Link>
      </div>
    </div>
  )
}
