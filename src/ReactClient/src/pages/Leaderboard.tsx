import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { TrophyIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { GlobalLeaderboardViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

function medalClass(rank: number): string {
  if (rank === 1) return 'text-yellow-400'
  if (rank === 2) return 'text-slate-300'
  if (rank === 3) return 'text-amber-600'
  return 'text-muted-foreground'
}

function medalLabel(rank: number): string {
  if (rank === 1) return '🥇'
  if (rank === 2) return '🥈'
  if (rank === 3) return '🥉'
  return `#${rank}`
}

function SeasonCountdown({ seasonEnd }: { seasonEnd: string }) {
  const end = new Date(seasonEnd)
  const now = new Date()
  const diffMs = end.getTime() - now.getTime()
  const diffDays = Math.max(0, Math.floor(diffMs / (1000 * 60 * 60 * 24)))
  const diffHours = Math.max(0, Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)))
  return (
    <span className="text-sm text-muted-foreground">
      Season resets in <span className="font-semibold text-foreground">{diffDays}d {diffHours}h</span>
    </span>
  )
}

export function Leaderboard() {
  const { data, isLoading, error, refetch } = useQuery<GlobalLeaderboardViewModel>({
    queryKey: ['global-leaderboard'],
    queryFn: () => apiClient.get<GlobalLeaderboardViewModel>('/api/leaderboard').then((r) => r.data),
    refetchInterval: 60_000,
  })

  if (isLoading) return <PageLoader message="Loading leaderboard..." />
  if (error) return <ApiError message="Failed to load leaderboard." onRetry={() => void refetch()} />
  if (!data) return null

  const entries = data.entries
  const top10 = entries.slice(0, 10)
  const rest = entries.slice(10)
  const currentPlayerEntry = entries.find((e) => e.isCurrentPlayer)

  return (
    <div className="max-w-3xl space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <TrophyIcon className="h-6 w-6 text-yellow-400" />
            Global Leaderboard
          </h1>
          <p className="text-sm text-muted-foreground mt-1">Rolling 30-day season</p>
        </div>
        <SeasonCountdown seasonEnd={data.seasonEnd} />
      </div>

      {entries.length === 0 ? (
        <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground text-sm">
          No ranked players yet this season. Play a game to get on the board!
        </div>
      ) : (
        <>
          {/* Top 10 podium */}
          <div className="rounded-lg border overflow-hidden">
            <div className="px-4 py-2 bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide font-semibold">
              Top Players
            </div>
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-secondary/10 text-xs text-muted-foreground uppercase tracking-wide">
                  <th scope="col" className="px-4 py-2 text-left w-12">Rank</th>
                  <th scope="col" className="px-4 py-2 text-left">Player</th>
                  <th scope="col" className="px-4 py-2 text-right">Score</th>
                  <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">T. Wins</th>
                  <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">Wins</th>
                  <th scope="col" className="px-4 py-2 text-right hidden md:table-cell">Milestones</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {top10.map((entry) => (
                  <tr
                    key={entry.userId}
                    className={`transition-colors ${entry.isCurrentPlayer ? 'bg-primary/10 border-l-2 border-l-primary' : 'hover:bg-secondary/20'}`}
                  >
                    <td className={`px-4 py-3 font-bold text-base ${medalClass(entry.rank)}`}>
                      {medalLabel(entry.rank)}
                    </td>
                    <td className="px-4 py-3 font-medium">
                      <Link
                        to={`/profile/${encodeURIComponent(entry.userId)}`}
                        className="hover:underline text-primary"
                      >
                        {entry.displayName}
                      </Link>
                      {entry.isCurrentPlayer && (
                        <span className="ml-2 text-xs text-muted-foreground">(you)</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-right font-mono font-semibold">
                      {entry.score.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden sm:table-cell">
                      {entry.tournamentWins}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden sm:table-cell">
                      {entry.gameWins}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden md:table-cell">
                      {entry.achievementsUnlocked}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Current player sticky row if outside top 10 */}
          {currentPlayerEntry && currentPlayerEntry.rank > 10 && (
            <div className="rounded-lg border border-primary/40 bg-primary/5 overflow-hidden">
              <div className="px-4 py-2 text-xs text-muted-foreground font-semibold uppercase tracking-wide">
                Your Ranking
              </div>
              <table className="w-full text-sm">
                <tbody>
                  <tr>
                    <td className="px-4 py-3 font-bold text-muted-foreground w-12">#{currentPlayerEntry.rank}</td>
                    <td className="px-4 py-3 font-medium">
                      {currentPlayerEntry.displayName}
                      <span className="ml-2 text-xs text-muted-foreground">(you)</span>
                    </td>
                    <td className="px-4 py-3 text-right font-mono font-semibold">
                      {currentPlayerEntry.score.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden sm:table-cell">
                      {currentPlayerEntry.tournamentWins}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden sm:table-cell">
                      {currentPlayerEntry.gameWins}
                    </td>
                    <td className="px-4 py-3 text-right font-mono hidden md:table-cell">
                      {currentPlayerEntry.achievementsUnlocked}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          )}

          {/* Remaining players (11+) */}
          {rest.length > 0 && (
            <div className="rounded-lg border overflow-hidden">
              <div className="px-4 py-2 bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide font-semibold">
                Remaining Players
              </div>
              <table className="w-full text-sm">
                <tbody className="divide-y">
                  {rest.map((entry) => (
                    <tr
                      key={entry.userId}
                      className={`transition-colors ${entry.isCurrentPlayer ? 'bg-primary/10' : 'hover:bg-secondary/20'}`}
                    >
                      <td className="px-4 py-2 font-mono text-muted-foreground w-12">#{entry.rank}</td>
                      <td className="px-4 py-2 font-medium">
                        <Link
                          to={`/profile/${encodeURIComponent(entry.userId)}`}
                          className="hover:underline text-primary"
                        >
                          {entry.displayName}
                        </Link>
                      </td>
                      <td className="px-4 py-2 text-right font-mono">{entry.score.toFixed(1)}</td>
                      <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">{entry.tournamentWins}</td>
                      <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">{entry.gameWins}</td>
                      <td className="px-4 py-2 text-right font-mono hidden md:table-cell">{entry.achievementsUnlocked}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {/* Score formula legend */}
      <div className="rounded-lg border bg-secondary/10 p-4 text-xs text-muted-foreground space-y-1">
        <p className="font-semibold text-foreground text-sm">Score Formula</p>
        <p>Tournament win = 3 pts &nbsp;·&nbsp; Game win = 1 pt &nbsp;·&nbsp; Milestone unlocked = 0.5 pts</p>
        <p>Season window: rolling 30 days</p>
      </div>
    </div>
  )
}
