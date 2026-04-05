import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerHistoryViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

function formatDuration(startIso: string, endIso: string): string {
  const ms = new Date(endIso).getTime() - new Date(startIso).getTime()
  if (ms <= 0) return '—'
  const totalMinutes = Math.floor(ms / 60000)
  const days = Math.floor(totalMinutes / 1440)
  const hours = Math.floor((totalMinutes % 1440) / 60)
  const minutes = totalMinutes % 60
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}m`
  return `${minutes}m`
}

function rankBadgeClass(rank: number, players: number): string {
  if (rank === 1) return 'bg-warning text-black'
  if (rank === 2) return 'bg-muted-foreground text-black'
  if (rank === 3) return 'bg-warning/60 text-white'
  if (rank > players / 2) return 'bg-secondary text-secondary-foreground opacity-60'
  return 'bg-secondary text-secondary-foreground'
}

export function PlayerHistory() {
  const { data, isLoading, error, refetch } = useQuery<PlayerHistoryViewModel>({
    queryKey: ['player-history'],
    queryFn: () => apiClient.get('/api/history').then((r) => r.data),
  })

  if (isLoading) return <PageLoader message="Loading history..." />
  if (error) return <ApiError message="Failed to load history." onRetry={() => void refetch()} />

  if (!data || data.totalGames === 0) {
    return (
      <div className="p-6 max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold mb-6">My History</h1>
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="text-5xl mb-4 opacity-30">🕐</div>
          <p className="text-muted-foreground">You haven't finished any games yet.</p>
          <p className="text-muted-foreground text-sm mt-1">Complete a game round to see your history here.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">My History</h1>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
        {[
          { label: 'Games Played', value: data.totalGames, color: '' },
          { label: 'Wins', value: data.totalWins, color: 'text-warning-foreground' },
          { label: 'Best Rank', value: `#${data.bestRank}`, color: '' },
          { label: 'Total Score', value: Number(data.totalScore).toLocaleString(), color: '' },
        ].map(({ label, value, color }) => (
          <div key={label} className="rounded-lg border border-border bg-card p-3 text-center">
            <div className={`text-2xl font-bold ${color}`}>{value}</div>
            <div className="text-xs text-muted-foreground mt-1">{label}</div>
          </div>
        ))}
      </div>

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/40">
              <th className="px-3 py-2 text-left font-medium"></th>
              <th className="px-3 py-2 text-left font-medium">Game</th>
              <th className="px-3 py-2 text-left font-medium">Ended</th>
              <th className="px-3 py-2 text-left font-medium hidden sm:table-cell">Duration</th>
              <th className="px-3 py-2 text-left font-medium">Rank</th>
              <th className="px-3 py-2 text-right font-medium">Score</th>
              <th className="px-3 py-2 text-right font-medium hidden sm:table-cell">Players</th>
            </tr>
          </thead>
          <tbody>
            {data.games.map((entry, idx) => (
              <tr
                key={`${entry.gameId}-${idx}`}
                className="border-b border-border/50 hover:bg-muted/30 transition-colors"
              >
                <td className="px-3 py-2 w-6">
                  {entry.isWin && <span title="Win">🏆</span>}
                </td>
                <td className="px-3 py-2">
                  <span className="font-medium">{entry.gameName}</span>
                  <span className="text-muted-foreground text-xs ml-1 hidden sm:inline">{entry.gameDefType}</span>
                </td>
                <td className="px-3 py-2 text-muted-foreground">
                  {new Date(entry.endTime).toLocaleDateString(undefined, {
                    day: 'numeric',
                    month: 'short',
                    year: 'numeric',
                  })}
                </td>
                <td className="px-3 py-2 text-muted-foreground hidden sm:table-cell">
                  {formatDuration(entry.startTime, entry.endTime)}
                </td>
                <td className="px-3 py-2">
                  <span
                    className={`text-xs px-2 py-0.5 rounded font-bold ${rankBadgeClass(entry.finalRank, entry.playersInGame)}`}
                  >
                    #{entry.finalRank}
                  </span>
                </td>
                <td className="px-3 py-2 text-right font-mono">
                  {Number(entry.finalScore).toLocaleString()}
                </td>
                <td className="px-3 py-2 text-right text-muted-foreground hidden sm:table-cell">
                  {entry.playersInGame}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
