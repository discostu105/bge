import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import { TrophyIcon, SwordsIcon, StarIcon, ChevronRightIcon, KeyIcon, CopyIcon, CheckIcon, Trash2Icon } from 'lucide-react'
import apiClient from '@/api/client'
import type { ProfileViewModel, PlayerAchievementsViewModel, PlayerHistoryViewModel, PlayerListViewModel, ApiKeyViewModel } from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonCard, SkeletonLine } from '@/components/Skeleton'
import { relativeTime } from '@/lib/utils'

function ApiKeySection() {
  const queryClient = useQueryClient()
  const [newKey, setNewKey] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  const { data: playerList, isLoading } = useQuery({
    queryKey: ['my-players'],
    queryFn: () => apiClient.get<PlayerListViewModel>('/api/player-management').then(r => r.data),
  })

  const generateMutation = useMutation({
    mutationFn: (playerId: string) =>
      apiClient.post<ApiKeyViewModel>(`/api/player-management/${playerId}/apikey`).then(r => r.data),
    onSuccess: (data) => {
      setNewKey(data.apiKey)
      void queryClient.invalidateQueries({ queryKey: ['my-players'] })
    },
  })

  const revokeMutation = useMutation({
    mutationFn: (playerId: string) =>
      apiClient.delete(`/api/player-management/${playerId}/apikey`),
    onSuccess: () => {
      setNewKey(null)
      void queryClient.invalidateQueries({ queryKey: ['my-players'] })
    },
  })

  const handleCopy = async () => {
    if (!newKey) return
    await navigator.clipboard.writeText(newKey)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  if (isLoading) return null

  const player = playerList?.players?.[0]
  if (!player) return null

  return (
    <div className="rounded-lg border bg-card p-5 space-y-3">
      <strong className="text-sm flex items-center gap-1.5">
        <KeyIcon className="h-4 w-4 text-primary" />
        Bot API Key
      </strong>
      <p className="text-xs text-muted-foreground">
        Use an API key to control your player programmatically via the REST API.
      </p>

      {newKey && (
        <div className="rounded border bg-secondary/30 p-3 space-y-2">
          <div className="text-xs font-medium text-warning-foreground">
            Copy this key now — it won't be shown again.
          </div>
          <div className="flex items-center gap-2">
            <code className="flex-1 text-xs bg-background rounded px-2 py-1.5 break-all select-all border">
              {newKey}
            </code>
            <button
              onClick={() => void handleCopy()}
              className="shrink-0 p-1.5 rounded hover:bg-secondary"
              aria-label="Copy API key"
            >
              {copied ? <CheckIcon className="h-4 w-4 text-green-500" /> : <CopyIcon className="h-4 w-4" />}
            </button>
          </div>
        </div>
      )}

      <div className="flex items-center gap-2">
        {player.hasApiKey ? (
          <>
            <span className="text-xs text-muted-foreground">Active key configured</span>
            <button
              onClick={() => generateMutation.mutate(player.playerId)}
              disabled={generateMutation.isPending}
              className="rounded bg-primary px-3 py-1.5 text-xs text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {generateMutation.isPending ? 'Regenerating...' : 'Regenerate'}
            </button>
            <button
              onClick={() => revokeMutation.mutate(player.playerId)}
              disabled={revokeMutation.isPending}
              className="rounded border border-destructive/50 px-3 py-1.5 text-xs text-destructive hover:bg-destructive/10 disabled:opacity-50 flex items-center gap-1"
            >
              <Trash2Icon className="h-3 w-3" />
              {revokeMutation.isPending ? 'Revoking...' : 'Revoke'}
            </button>
          </>
        ) : (
          <button
            onClick={() => generateMutation.mutate(player.playerId)}
            disabled={generateMutation.isPending}
            className="rounded bg-primary px-3 py-1.5 text-xs text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {generateMutation.isPending ? 'Generating...' : 'Generate API Key'}
          </button>
        )}
      </div>

      {(generateMutation.isError || revokeMutation.isError) && (
        <p className="text-xs text-destructive">Something went wrong. Please try again.</p>
      )}
    </div>
  )
}

function outcomeLabel(isWin: boolean, gameStatus: string): { label: string; className: string } {
  if (isWin) return { label: 'Win', className: 'bg-green-700/30 text-green-300' }
  if (gameStatus.toLowerCase() === 'active' || gameStatus.toLowerCase() === 'running') {
    return { label: 'DNF', className: 'bg-secondary/40 text-muted-foreground' }
  }
  return { label: 'Loss', className: 'bg-red-900/30 text-red-300' }
}

export function PlayerProfile() {
  const { data: profile, isLoading, error, refetch } = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.get<ProfileViewModel>('/api/profile').then((r) => r.data),
  })

  const { data: achievementsData } = useQuery<PlayerAchievementsViewModel>({
    queryKey: ['achievements'],
    queryFn: () => apiClient.get('/api/player-management/me/achievements').then(r => r.data),
  })

  const { data: historyData } = useQuery<PlayerHistoryViewModel>({
    queryKey: ['player-history'],
    queryFn: () => apiClient.get('/api/history').then(r => r.data),
  })

  if (isLoading) return (
    <div className="max-w-lg space-y-5" aria-hidden="true">
      <SkeletonLine className="h-6 w-40" />
      <div className="rounded-lg border bg-card p-6 space-y-5">
        <div className="flex items-center gap-4 animate-pulse">
          <div className="h-16 w-16 rounded-full bg-muted" />
          <div className="space-y-2">
            <div className="h-5 w-32 rounded bg-muted" />
            <div className="h-3.5 w-24 rounded bg-muted" />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <SkeletonCard />
          <SkeletonCard />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      </div>
    </div>
  )
  if (error) return <ApiError message="Failed to load profile." onRetry={() => void refetch()} />
  if (!profile) return <p className="text-destructive text-sm">Failed to load profile.</p>

  const displayName = profile.displayName ?? profile.playerName ?? 'Unknown'
  const losses = profile.gamesPlayed - profile.wins
  const winRate = profile.gamesPlayed > 0 ? (profile.wins / profile.gamesPlayed) * 100 : 0
  const historyGames = historyData?.games ?? []
  const avgScore = historyGames.length > 0
    ? historyGames.reduce((sum, g) => sum + g.finalScore, 0) / historyGames.length
    : 0

  return (
    <div className="max-w-2xl space-y-5">
      <h1 className="text-xl font-bold">My Profile</h1>

      <div className="rounded-lg border bg-card p-6 space-y-5">
        {/* Avatar + name + join date */}
        <div className="flex items-center gap-4">
          {profile.avatarUrl ? (
            <img
              src={profile.avatarUrl}
              alt={displayName}
              width="64"
              height="64"
              className="h-16 w-16 rounded-full border-2 border-primary/50"
              fetchPriority="high"
            />
          ) : (
            <div className="h-16 w-16 rounded-full bg-secondary flex items-center justify-center text-2xl border-2 border-primary/50">
              {displayName[0]?.toUpperCase() ?? '?'}
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-bold text-lg">{displayName}</span>
              <span className="inline-flex items-center rounded-full bg-primary/20 px-2.5 py-0.5 text-xs font-bold text-primary border border-primary/30">
                Lv {profile.level}
              </span>
            </div>
            {profile.playerName && profile.displayName && profile.playerName !== profile.displayName && (
              <div className="text-sm text-muted-foreground">Playing as {profile.playerName}</div>
            )}
            {profile.joinedAt && (
              <div className="text-xs text-muted-foreground mt-0.5">
                Member since {relativeTime(profile.joinedAt)}
              </div>
            )}
          </div>
        </div>

        {/* XP progress bar */}
        <div className="space-y-1">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span className="font-medium">Level {profile.level}{profile.level < 50 ? ` → ${profile.level + 1}` : ' (Max)'}</span>
            <span>{profile.totalXp.toLocaleString()} XP total
              {profile.level < 50 && <> · {profile.xpToNextLevel.toLocaleString()} to next level</>}
            </span>
          </div>
          <div className="h-2 rounded-full bg-secondary overflow-hidden" role="progressbar" aria-valuenow={profile.levelProgress} aria-valuemin={0} aria-valuemax={100}>
            <div
              className="h-full rounded-full bg-primary transition-all"
              style={{ width: `${profile.level >= 50 ? 100 : profile.levelProgress}%` }}
            />
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

      {/* Career stats — 6 cells */}
      {profile.gamesPlayed > 0 && (
        <div className="rounded-lg border bg-card p-5 space-y-3">
          <strong className="text-sm flex items-center gap-1.5">
            <TrophyIcon className="h-4 w-4 text-primary" />
            Career Stats
          </strong>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Games</div>
              <div className="text-xl font-bold">{profile.gamesPlayed}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Wins</div>
              <div className="text-xl font-bold text-green-400">{profile.wins}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Losses</div>
              <div className="text-xl font-bold text-red-400">{losses}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Win Rate</div>
              <div className="text-xl font-bold">{winRate.toFixed(0)}%</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Avg Score</div>
              <div className="text-xl font-bold">{avgScore > 0 ? Math.floor(avgScore).toLocaleString() : '—'}</div>
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

      {/* API Key management */}
      <ApiKeySection />

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

      {/* Game history — last 20 games */}
      {historyGames.length > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Recent Games</strong>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm" aria-label="Game history">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Game</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Type</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Date</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Rank</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Outcome</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Replay</th>
                </tr>
              </thead>
              <tbody>
                {historyGames.slice(0, 20).map((g) => {
                  const outcome = outcomeLabel(g.isWin, '')
                  return (
                    <tr key={g.gameId} className="border-b border-border last:border-0">
                      <td className="py-2 px-3 font-medium">{g.gameName}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground hidden sm:table-cell">{g.gameDefType || '—'}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground whitespace-nowrap">
                        {relativeTime(g.finishedAt)}
                      </td>
                      <td className="py-2 px-3 font-mono">#{g.finalRank}</td>
                      <td className="py-2 px-3">
                        <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium ${outcome.className}`}>
                          {g.isWin && <StarIcon className="h-3 w-3" />}
                          {outcome.label}
                        </span>
                      </td>
                      <td className="py-2 px-3 text-muted-foreground hidden sm:table-cell">—</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
