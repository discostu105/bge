import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router'
import { UserIcon, TrophyIcon, StarIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { PlayerCrossGameStatsViewModel } from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function PublicProfile() {
  const { userId } = useParams<{ userId: string }>()

  const { data: stats, isLoading, error, refetch } = useQuery<PlayerCrossGameStatsViewModel>({
    queryKey: ['public-profile', userId],
    queryFn: () =>
      apiClient.get(`/api/players/${encodeURIComponent(userId ?? '')}/profile`).then((r) => r.data),
    enabled: !!userId,
  })

  if (isLoading) return <PageLoader message="Loading profile..." />
  if (error) return <ApiError message="Failed to load player profile." onRetry={() => void refetch()} />
  if (!stats) {
    return (
      <div className="max-w-lg p-4">
        <div className="rounded-lg border border-dashed p-6 text-center text-muted-foreground">
          <UserIcon className="h-10 w-10 mx-auto mb-2 opacity-40" />
          <p className="text-sm">Player not found or no game history yet.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-lg space-y-5">
      <h1 className="text-xl font-bold flex items-center gap-2">
        <UserIcon className="h-5 w-5 text-primary" />
        {stats.playerName ?? 'Unknown Player'}
      </h1>

      {/* Summary stats */}
      <div className="rounded-lg border bg-card p-5 space-y-3">
        <strong className="text-sm flex items-center gap-1.5">
          <TrophyIcon className="h-4 w-4 text-primary" />
          Career Stats
        </strong>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Games</div>
            <div className="text-xl font-bold">{stats.totalGames}</div>
          </div>
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Wins</div>
            <div className="text-xl font-bold text-green-400">{stats.totalWins}</div>
          </div>
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Best Rank</div>
            <div className="text-xl font-bold">
              {stats.bestRank > 0 ? (
                <span className="flex items-center justify-center gap-1">
                  {stats.bestRank === 1 && <StarIcon className="h-4 w-4 text-amber-400" />}
                  #{stats.bestRank}
                </span>
              ) : '—'}
            </div>
          </div>
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Total Score</div>
            <div className="text-xl font-bold">{Math.floor(stats.totalScore).toLocaleString()}</div>
          </div>
        </div>
      </div>

      {/* Game History */}
      {stats.games.length > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Game History</strong>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Game</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Result</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Rank</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Score</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Ended</th>
                </tr>
              </thead>
              <tbody>
                {stats.games.map((g) => (
                  <tr key={g.gameId} className="border-b border-border">
                    <td className="py-2 px-3 font-medium">{g.gameName}</td>
                    <td className="py-2 px-3">
                      {g.isWinner ? (
                        <span className="inline-flex items-center gap-1 rounded bg-green-700/30 px-2 py-0.5 text-xs text-green-300">
                          <StarIcon className="h-3 w-3" />
                          Winner
                        </span>
                      ) : (
                        <span className="text-xs text-muted-foreground">{g.gameStatus}</span>
                      )}
                    </td>
                    <td className="py-2 px-3">#{g.finalRank}</td>
                    <td className="py-2 px-3">{Math.floor(g.finalScore).toLocaleString()}</td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {relativeTime(g.gameEndTime)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
