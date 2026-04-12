import { useParams, Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameResultsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { formatDuration } from '@/lib/formatters'

export function GameResults() {
  const { gameId } = useParams<{ gameId: string }>()

  const { data: model, error, isLoading, refetch } = useQuery<GameResultsViewModel>({
    queryKey: ['game-results', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/results`).then((r) => r.data),
    enabled: !!gameId,
  })

  if (isLoading) return <PageLoader message="Loading results..." />
  if (error) return <ApiError message="Failed to load game results." onRetry={() => void refetch()} />
  if (!model) return null

  const end = new Date(model.actualEndTime ?? model.endTime)
  const start = new Date(model.startTime)
  const duration = formatDuration(end.getTime() - start.getTime())

  const [first, second, third] = [model.standings[0], model.standings[1], model.standings[2]]

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{model.name} — Results</h1>

      {model.tournamentId && (
        <div>
          <Link
            to={`/tournaments/${model.tournamentId}`}
            className="text-sm text-primary underline"
          >
            View Tournament Results: {model.tournamentId}
          </Link>
        </div>
      )}

      <p className="text-muted-foreground text-sm">
        {start.toLocaleDateString()} {start.toLocaleTimeString()} — {end.toLocaleDateString()} {end.toLocaleTimeString()}
        &nbsp;·&nbsp; Duration: {duration}
      </p>

      {model.standings.length === 0 && (
        <p className="text-muted-foreground text-sm">No standings recorded for this game.</p>
      )}

      {model.standings.length >= 1 && (
        <>
          {/* Podium */}
          <div className="flex items-end justify-center gap-4 flex-wrap">
            {second && (
              <div className="rounded-lg border border-border text-center p-4 w-36">
                <div className="text-2xl">🥈</div>
                <div className="font-bold text-sm">{second.playerName}</div>
                <div className="text-muted-foreground text-xs">{second.land.toLocaleString()}</div>
              </div>
            )}
            {first && (
              <div className="rounded-lg border border-warning/50 text-center p-4 w-40">
                <div className="text-3xl">🏆</div>
                <div className="font-bold">{first.playerName}</div>
                <div className="text-muted-foreground text-sm">{first.land.toLocaleString()}</div>
                <span className="text-xs px-2 py-0.5 rounded bg-warning text-warning-foreground mt-1 inline-block">Winner</span>
              </div>
            )}
            {third && (
              <div className="rounded-lg border border-border text-center p-4 w-36">
                <div className="text-2xl">🥉</div>
                <div className="font-bold text-sm">{third.playerName}</div>
                <div className="text-muted-foreground text-xs">{third.land.toLocaleString()}</div>
              </div>
            )}
          </div>

          {/* Full standings table */}
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th scope="col" className="py-2 pr-4">Rank</th>
                <th scope="col" className="py-2 pr-4">Player</th>
                <th scope="col" className="py-2">Land</th>
              </tr>
            </thead>
            <tbody>
              {model.standings.map((entry) => {
                const isMe = entry.playerId === model.currentPlayerId
                return (
                  <tr
                    key={entry.playerId}
                    className={`border-b border-border ${
                      isMe ? 'bg-info/10 font-semibold' : entry.isWinner ? 'bg-warning/10' : ''
                    }`}
                  >
                    <td className="py-2 pr-4">
                      {entry.isWinner ? `🏆 #${entry.rank}` : `#${entry.rank}`}
                    </td>
                    <td className="py-2 pr-4">
                      {entry.playerName}
                      {isMe && (
                        <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-primary text-primary-foreground">You</span>
                      )}
                      {!isMe && entry.isWinner && (
                        <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-warning text-warning-foreground">Winner</span>
                      )}
                    </td>
                    <td className="py-2">{entry.land.toLocaleString()}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </>
      )}
    </div>
  )
}
