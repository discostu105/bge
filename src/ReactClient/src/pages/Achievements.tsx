import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { PlayerAchievementsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function Achievements() {
  const { data, isLoading, error, refetch } = useQuery<PlayerAchievementsViewModel>({
    queryKey: ['achievements'],
    queryFn: () => apiClient.get('/api/player-management/me/achievements').then((r) => r.data),
  })

  const formatDate = (d: string) =>
    new Date(d).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })

  const rankBadge = (rank: number): string => {
    if (rank === 1) return 'bg-yellow-500 text-black'
    if (rank === 2) return 'bg-gray-400 text-black'
    if (rank === 3) return 'bg-amber-700 text-white'
    return 'bg-secondary text-secondary-foreground'
  }

  if (isLoading) return <PageLoader message="Loading achievements..." />
  if (error) return <ApiError message="Failed to load achievements." onRetry={() => void refetch()} />

  const achievements = data?.achievements ?? []

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-1">Achievements</h1>
      <p className="text-muted-foreground mb-6">Your earned badges across all games.</p>

      {achievements.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="text-6xl mb-4 opacity-30">🎯</div>
          <h3 className="text-lg font-semibold mb-1">No achievements yet</h3>
          <p className="text-muted-foreground mb-4">Complete your first game to earn your first badge!</p>
          <Link
            to="/games"
            className="px-4 py-2 rounded bg-primary text-primary-foreground hover:bg-primary/90 text-sm"
          >
            Browse Games
          </Link>
        </div>
      ) : (
        <>
          <p className="mb-4 text-sm text-muted-foreground">
            <strong className="text-foreground">{achievements.length}</strong>{' '}
            achievement{achievements.length !== 1 ? 's' : ''} earned
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {achievements.map((a, idx) => (
              <div
                key={idx}
                className="rounded-lg border border-border bg-card p-4 flex flex-col gap-2 hover:border-border/80 transition-colors"
              >
                <div className="text-3xl leading-none">{a.achievementIcon}</div>
                <div className="font-semibold text-sm">{a.achievementLabel}</div>
                <div className="text-sm text-muted-foreground">{a.gameName}</div>
                <div className="flex justify-between items-center mt-auto text-xs text-muted-foreground">
                  <span className={`px-2 py-0.5 rounded font-mono font-bold ${rankBadge(a.finalRank)}`}>
                    #{a.finalRank}
                  </span>
                  <span>{Number(a.score).toLocaleString()} pts</span>
                  <span>{formatDate(a.earnedAt)}</span>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
