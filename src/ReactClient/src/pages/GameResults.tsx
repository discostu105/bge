import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameResultsViewModel } from '@/api/types'

function formatDuration(ms: number): string {
  const totalSec = Math.floor(ms / 1000)
  const days = Math.floor(totalSec / 86400)
  const hours = Math.floor((totalSec % 86400) / 3600)
  const mins = Math.floor((totalSec % 3600) / 60)
  if (days >= 1) return `${days}d ${hours}h`
  if (hours >= 1) return `${hours}h ${mins}m`
  return `${mins}m`
}

function vcEmoji(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return '💰'
    case 'TimeExpired': return '⏰'
    case 'AdminFinalized': return '🛑'
    default: return '🏁'
  }
}

function vcBadgeCss(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return 'bg-yellow-500 text-black'
    case 'TimeExpired': return 'bg-gray-500 text-white'
    case 'AdminFinalized': return 'bg-red-700 text-white'
    default: return 'bg-gray-500 text-white'
  }
}

function vcText(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return 'CONQUERED'
    case 'TimeExpired': return 'TIME EXPIRED'
    case 'AdminFinalized': return 'ADMIN ENDED'
    default: return 'FINISHED'
  }
}

export function GameResults() {
  const { gameId } = useParams<{ gameId: string }>()

  const { data: model, error, isLoading } = useQuery<GameResultsViewModel>({
    queryKey: ['game-results', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/results`).then((r) => r.data),
    enabled: !!gameId,
  })

  if (isLoading) return <p className="text-muted-foreground text-sm">Loading...</p>
  if (error) return <div className="rounded border border-red-700 bg-red-900/20 p-3 text-sm text-red-400">{String(error)}</div>
  if (!model) return null

  const end = new Date(model.actualEndTime ?? model.endTime)
  const start = new Date(model.startTime)
  const duration = formatDuration(end.getTime() - start.getTime())

  const [first, second, third] = [model.standings[0], model.standings[1], model.standings[2]]

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{model.name} — Results</h1>

      {model.victoryConditionType && (
        <div className="flex items-center gap-2">
          <span className="text-2xl">{vcEmoji(model.victoryConditionType)}</span>
          <span className={`text-sm font-bold px-3 py-1 rounded ${vcBadgeCss(model.victoryConditionType)}`}>
            {vcText(model.victoryConditionType)}
          </span>
          {model.victoryConditionLabel && (
            <span className="text-muted-foreground text-sm">— {model.victoryConditionLabel}</span>
          )}
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
              <div className="rounded-lg border border-gray-600 text-center p-4 w-36">
                <div className="text-2xl">🥈</div>
                <div className="font-bold text-sm">{second.playerName}</div>
                <div className="text-muted-foreground text-xs">{second.score.toLocaleString()}</div>
              </div>
            )}
            {first && (
              <div className="rounded-lg border border-yellow-600 text-center p-4 w-40">
                <div className="text-3xl">🏆</div>
                <div className="font-bold">{first.playerName}</div>
                <div className="text-muted-foreground text-sm">{first.score.toLocaleString()}</div>
                <span className="text-xs px-2 py-0.5 rounded bg-yellow-500 text-black mt-1 inline-block">Winner</span>
              </div>
            )}
            {third && (
              <div className="rounded-lg border border-gray-600 text-center p-4 w-36">
                <div className="text-2xl">🥉</div>
                <div className="font-bold text-sm">{third.playerName}</div>
                <div className="text-muted-foreground text-xs">{third.score.toLocaleString()}</div>
              </div>
            )}
          </div>

          {/* Full standings table */}
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th className="py-2 pr-4">Rank</th>
                <th className="py-2 pr-4">Player</th>
                <th className="py-2">Score</th>
              </tr>
            </thead>
            <tbody>
              {model.standings.map((entry) => {
                const isMe = entry.playerId === model.currentPlayerId
                return (
                  <tr
                    key={entry.playerId}
                    className={`border-b border-border ${
                      isMe ? 'bg-blue-900/30 font-semibold' : entry.isWinner ? 'bg-yellow-900/20' : ''
                    }`}
                  >
                    <td className="py-2 pr-4">
                      {entry.isWinner ? `🏆 #${entry.rank}` : `#${entry.rank}`}
                    </td>
                    <td className="py-2 pr-4">
                      {entry.playerName}
                      {isMe && (
                        <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-blue-700 text-white">You</span>
                      )}
                      {!isMe && entry.isWinner && (
                        <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-yellow-500 text-black">Winner</span>
                      )}
                    </td>
                    <td className="py-2">{entry.score.toLocaleString()}</td>
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
