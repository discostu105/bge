import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { AllTimePlayerListViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function PlayersList() {
  const { data, isLoading, error, refetch } = useQuery<AllTimePlayerListViewModel>({
    queryKey: ['players-list'],
    queryFn: () => apiClient.get<AllTimePlayerListViewModel>('/api/players/all').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const players = data?.players ?? []

  return (
    <div className="max-w-3xl space-y-4">
      <h1 className="text-xl font-bold">All-Time Players</h1>

      {isLoading ? (
        <PageLoader message="Loading players..." />
      ) : error ? (
        <ApiError message="Failed to load players." onRetry={() => void refetch()} />
      ) : players.length === 0 ? (
        <p className="text-muted-foreground text-sm">No players yet.</p>
      ) : (
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide">
                <th scope="col" className="px-4 py-2 text-left w-10">#</th>
                <th scope="col" className="px-4 py-2 text-left">Player</th>
                <th scope="col" className="px-4 py-2 text-right">Score</th>
                <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">Games</th>
                <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">Wins</th>
                <th scope="col" className="px-4 py-2 text-right hidden md:table-cell">Best Rank</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {players.map((p, i) => (
                <tr key={p.userId} className="hover:bg-secondary/20 transition-colors">
                  <td className="px-4 py-2 font-mono text-muted-foreground">#{i + 1}</td>
                  <td className="px-4 py-2">
                    <Link
                      to={`/profile/${p.userId}`}
                      className="hover:text-primary transition-colors font-medium"
                    >
                      {p.displayName}
                    </Link>
                  </td>
                  <td className="px-4 py-2 text-right font-mono">
                    {Math.floor(p.totalScore).toLocaleString()}
                  </td>
                  <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">
                    {p.totalGames}
                  </td>
                  <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">
                    {p.totalWins}
                  </td>
                  <td className="px-4 py-2 text-right font-mono hidden md:table-cell">
                    {p.bestRank}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
