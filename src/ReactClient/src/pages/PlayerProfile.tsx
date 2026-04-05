import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { TrophyIcon, SwordsIcon, StarIcon, ChevronRightIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { ProfileViewModel, PlayerAchievementsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function PlayerProfile() {
  const { data: profile, isLoading, error, refetch } = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.get<ProfileViewModel>('/api/profile').then((r) => r.data),
  })

  const { data: achievementsData } = useQuery<PlayerAchievementsViewModel>({
    queryKey: ['achievements'],
    queryFn: () => apiClient.get('/api/player-management/me/achievements').then(r => r.data),
  })

  if (isLoading) return <PageLoader message="Loading profile..." />
  if (error) return <ApiError message="Failed to load profile." onRetry={() => void refetch()} />
  if (!profile) return <p className="text-destructive text-sm">Failed to load profile.</p>

  const displayName = profile.displayName ?? profile.playerName ?? 'Unknown'

  return (
    <div className="max-w-lg space-y-5">
      <h1 className="text-xl font-bold">My Profile</h1>

      <div className="rounded-lg border bg-card p-6 space-y-5">
        {/* Avatar + name */}
        <div className="flex items-center gap-4">
          {profile.avatarUrl ? (
            <img
              src={profile.avatarUrl}
              alt={displayName}
              className="h-16 w-16 rounded-full border-2 border-primary/50"
            />
          ) : (
            <div className="h-16 w-16 rounded-full bg-secondary flex items-center justify-center text-2xl border-2 border-primary/50">
              {displayName[0]?.toUpperCase() ?? '?'}
            </div>
          )}
          <div>
            <div className="font-bold text-lg">{displayName}</div>
            {profile.playerName && profile.displayName && profile.playerName !== profile.displayName && (
              <div className="text-sm text-muted-foreground">Playing as {profile.playerName}</div>
            )}
          </div>
        </div>

        {/* Current game stats */}
        {profile.currentGameId ? (
          <>
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Score</div>
                <div className="text-2xl font-bold">{Math.floor(profile.score).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Rank</div>
                <div className="text-2xl font-bold">
                  {profile.rank}
                  <span className="text-sm text-muted-foreground font-normal"> / {profile.totalPlayers}</span>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Land</div>
                <div className="text-lg font-semibold">{Math.floor(profile.land).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Minerals</div>
                <div className="text-lg font-semibold">{Math.floor(profile.minerals).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Gas</div>
                <div className="text-lg font-semibold">{Math.floor(profile.gas).toLocaleString()}</div>
              </div>
            </div>

            <div className="rounded border bg-secondary/20 p-3 flex items-center gap-2">
              <SwordsIcon className="h-4 w-4 text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Army Size:</span>
              <span className="font-semibold">{profile.armySize.toLocaleString()}</span>
            </div>

            <Link
              to={`/games/${profile.currentGameId}/base`}
              className="block rounded bg-primary px-4 py-2 text-center text-sm text-primary-foreground hover:opacity-90"
            >
              Go to Game →
            </Link>
          </>
        ) : (
          <div className="rounded border border-dashed px-4 py-3 text-sm text-muted-foreground">
            Not currently in a game.{' '}
            <Link to="/games" className="text-primary hover:underline">Browse games</Link>
          </div>
        )}
      </div>

      {/* Career stats */}
      {profile.gamesPlayed > 0 && (
        <div className="rounded-lg border bg-card p-5 space-y-3">
          <strong className="text-sm flex items-center gap-1.5">
            <TrophyIcon className="h-4 w-4 text-primary" />
            Career Stats
          </strong>
          <div className="grid grid-cols-3 gap-3">
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Games</div>
              <div className="text-xl font-bold">{profile.gamesPlayed}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Wins</div>
              <div className="text-xl font-bold text-green-400">{profile.wins}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Best Rank</div>
              <div className="text-xl font-bold">
                {profile.bestRank > 0 ? (
                  <span className="flex items-center justify-center gap-1">
                    {profile.bestRank === 1 && <StarIcon className="h-4 w-4 text-amber-400" />}
                    #{profile.bestRank}
                  </span>
                ) : '—'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Achievement badges */}
      {(() => {
        const badges = achievementsData?.achievements ?? []
        if (badges.length === 0) return null
        const shown = badges.slice(0, 4)
        return (
          <div className="rounded-lg border bg-card p-5 space-y-3">
            <div className="flex items-center justify-between">
              <strong className="text-sm flex items-center gap-1.5">
                <TrophyIcon className="h-4 w-4 text-yellow-500" />
                Recent Achievements
              </strong>
              <Link
                to="/achievements"
                className="text-xs text-primary hover:underline flex items-center gap-0.5"
              >
                View all <ChevronRightIcon className="h-3 w-3" />
              </Link>
            </div>
            <div className="flex flex-wrap gap-2">
              {shown.map((a, i) => (
                <div
                  key={i}
                  className="flex items-center gap-2 rounded-full border bg-secondary/20 px-3 py-1.5"
                  title={`${a.achievementLabel} — ${a.gameName}`}
                >
                  <span className="text-lg leading-none">{a.achievementIcon}</span>
                  <span className="text-xs font-medium">{a.achievementLabel}</span>
                </div>
              ))}
              {badges.length > 4 && (
                <Link
                  to="/achievements"
                  className="flex items-center rounded-full border border-dashed px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
                >
                  +{badges.length - 4} more
                </Link>
              )}
            </div>
          </div>
        )
      })()}
    </div>
  )
}
