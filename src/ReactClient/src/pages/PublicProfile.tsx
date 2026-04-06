import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useParams } from 'react-router'
import { Link } from 'react-router'
import { UserIcon, TrophyIcon, StarIcon, ChevronLeftIcon, ChevronRightIcon, FlagIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type { PlayerCrossGameStatsViewModel, PublicPlayerAchievementsViewModel, PlayerLeaderboardContextViewModel } from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

const REPORT_REASONS = ['Cheating', 'Harassment', 'Spam', 'Inappropriate name', 'Other']

function ReportModal({ targetUserId, onClose }: { targetUserId: string; onClose: () => void }) {
  const [reason, setReason] = useState(REPORT_REASONS[0])
  const [details, setDetails] = useState('')
  const [submitted, setSubmitted] = useState(false)

  const mutation = useMutation({
    mutationFn: () => apiClient.post('/api/reports', { targetUserId, reason, details: details.trim() || null }),
    onSuccess: () => setSubmitted(true),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60" onClick={onClose}>
      <div
        className="bg-card border border-border rounded-lg p-6 w-full max-w-sm space-y-4 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-lg font-bold">Report Player</h2>
        {submitted ? (
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">Your report has been submitted. Thank you.</p>
            <button onClick={onClose} className="w-full rounded bg-primary text-primary-foreground px-3 py-2 text-sm">
              Close
            </button>
          </div>
        ) : (
          <>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Reason</label>
              <select
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                className="w-full border border-border rounded px-2 py-1.5 text-sm bg-background"
              >
                {REPORT_REASONS.map((r) => <option key={r}>{r}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Details (optional)</label>
              <textarea
                value={details}
                onChange={(e) => setDetails(e.target.value)}
                rows={3}
                maxLength={500}
                className="w-full border border-border rounded px-2 py-1.5 text-sm bg-background resize-none"
                placeholder="Provide any additional context..."
              />
            </div>
            {mutation.isError && (
              <p className="text-xs text-destructive">
                {axios.isAxiosError(mutation.error) ? (mutation.error.response?.data as string ?? mutation.error.message) : 'Failed to submit report.'}
              </p>
            )}
            <div className="flex gap-2 justify-end">
              <button onClick={onClose} className="px-3 py-1.5 rounded border border-border text-sm hover:bg-muted">
                Cancel
              </button>
              <button
                onClick={() => mutation.mutate()}
                disabled={mutation.isPending}
                className="px-3 py-1.5 rounded bg-destructive text-destructive-foreground text-sm hover:bg-destructive/90 disabled:opacity-50"
              >
                {mutation.isPending ? 'Submitting…' : 'Submit Report'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

const PAGE_SIZE = 10

function avatarInitials(name: string | null | undefined): string {
  if (!name) return '?'
  const words = name.trim().split(/\s+/)
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase()
  return (words[0][0] + words[words.length - 1][0]).toUpperCase()
}

function outcomeLabel(isWinner: boolean, gameStatus: string): { label: string; className: string } {
  if (isWinner) return { label: 'Win', className: 'bg-green-700/30 text-green-300' }
  if (gameStatus.toLowerCase() === 'active' || gameStatus.toLowerCase() === 'running') {
    return { label: 'DNF', className: 'bg-secondary/40 text-muted-foreground' }
  }
  return { label: 'Loss', className: 'bg-red-900/30 text-red-300' }
}

function PublicAchievementBadges({ userId }: { userId: string }) {
  const { data } = useQuery<PublicPlayerAchievementsViewModel>({
    queryKey: ['public-achievements', userId],
    queryFn: () =>
      apiClient.get(`/api/players/${encodeURIComponent(userId)}/achievements`).then(r => r.data),
    retry: false,
  })

  const badges = data?.achievements ?? []
  if (badges.length === 0) return null

  const shown = badges.slice(0, 8)
  const extra = badges.length - 8

  return (
    <div className="rounded-lg border bg-card p-5 space-y-3">
      <strong className="text-sm flex items-center gap-1.5">
        <TrophyIcon className="h-4 w-4 text-yellow-500" />
        Achievements
      </strong>
      <div className="grid grid-cols-4 gap-2">
        {shown.map((a, i) => (
          <div
            key={i}
            className="flex flex-col items-center gap-1 rounded border bg-secondary/20 p-2 text-center"
            title={`${a.achievementLabel} — ${a.gameName}`}
          >
            <span className="text-xl leading-none">{a.achievementIcon}</span>
            <span className="text-xs font-medium leading-tight line-clamp-2">{a.achievementLabel}</span>
          </div>
        ))}
      </div>
      {extra > 0 && (
        <p className="text-xs text-muted-foreground">+{extra} more achievement{extra !== 1 ? 's' : ''}</p>
      )}
    </div>
  )
}

export function PublicProfile() {
  const { userId } = useParams<{ userId: string }>()
  const [page, setPage] = useState(0)
  const [showReport, setShowReport] = useState(false)

  const { data: stats, isLoading, error, refetch } = useQuery<PlayerCrossGameStatsViewModel>({
    queryKey: ['public-profile', userId],
    queryFn: () =>
      apiClient.get(`/api/players/${encodeURIComponent(userId ?? '')}/profile`).then((r) => r.data),
    enabled: !!userId,
  })

  const { data: leaderboardCtx } = useQuery<PlayerLeaderboardContextViewModel>({
    queryKey: ['leaderboard-context', userId],
    queryFn: () =>
      apiClient.get(`/api/leaderboard/player/${encodeURIComponent(userId ?? '')}`).then((r) => r.data),
    enabled: !!userId,
  })

  if (isLoading) return <PageLoader message="Loading profile..." />
  if (error) return <ApiError message="Failed to load player profile." onRetry={() => void refetch()} />
  if (!stats) {
    return (
      <div className="max-w-2xl p-4">
        <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
          <UserIcon className="h-10 w-10 mx-auto mb-2 opacity-40" />
          <p className="text-sm">Player not found or no game history yet.</p>
        </div>
      </div>
    )
  }

  const losses = stats.totalGames - stats.totalWins
  const winRate = stats.totalGames > 0 ? (stats.totalWins / stats.totalGames) * 100 : 0
  const avgRank =
    stats.games.length > 0
      ? stats.games.reduce((sum, g) => sum + g.finalRank, 0) / stats.games.length
      : 0

  const totalPages = Math.ceil(stats.games.length / PAGE_SIZE)
  const pagedGames = stats.games.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE)

  return (
    <div className="max-w-2xl space-y-5">
      {/* Profile header */}
      <div className="rounded-lg border bg-card p-5 flex items-center gap-4">
        <div
          className="flex-shrink-0 h-14 w-14 rounded-full bg-primary/20 border border-primary/30 flex items-center justify-center text-lg font-bold text-primary select-none"
          aria-hidden="true"
        >
          {avatarInitials(stats.playerName)}
        </div>
        <div className="flex-1">
          <h1 className="text-xl font-bold">{stats.playerName ?? 'Unknown Player'}</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {stats.totalGames} {stats.totalGames === 1 ? 'game' : 'games'} played
          </p>
          {leaderboardCtx && (
            <p className="text-xs mt-0.5">
              <Link to={`/leaderboard?highlight=${encodeURIComponent(userId ?? '')}`} className="text-yellow-500 hover:underline font-medium">
                🏆 Season rank #{leaderboardCtx.rank}
              </Link>
            </p>
          )}
          {stats.joinedAt && (
            <p className="text-xs text-muted-foreground mt-0.5">
              Member since {relativeTime(stats.joinedAt)}
            </p>
          )}
        </div>
        {userId && (
          <button
            onClick={() => setShowReport(true)}
            className="flex-shrink-0 flex items-center gap-1 px-2 py-1 rounded border border-border text-xs text-muted-foreground hover:bg-muted hover:text-foreground"
            title="Report this player"
          >
            <FlagIcon className="h-3 w-3" />
            Report
          </button>
        )}
      </div>
      {showReport && userId && <ReportModal targetUserId={userId} onClose={() => setShowReport(false)} />}

      {/* Statistics panel */}
      <div className="rounded-lg border bg-card p-5 space-y-3">
        <strong className="text-sm flex items-center gap-1.5">
          <TrophyIcon className="h-4 w-4 text-primary" />
          Career Stats
        </strong>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Wins</div>
            <div className="text-xl font-bold text-green-400">{stats.totalWins}</div>
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
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Avg Rank</div>
            <div className="text-xl font-bold">{avgRank > 0 ? `#${avgRank.toFixed(1)}` : '—'}</div>
          </div>
          <div className="rounded border bg-secondary/20 p-3 text-center">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Resources Gathered</div>
            <div className="text-xl font-bold">
              {stats.totalResourcesGathered != null
                ? Math.floor(stats.totalResourcesGathered).toLocaleString()
                : '—'}
            </div>
          </div>
        </div>
      </div>

      {/* Game history table */}
      {stats.games.length === 0 ? (
        <div className="rounded-lg border border-dashed p-6 text-center text-muted-foreground">
          <p className="text-sm">No games played yet.</p>
        </div>
      ) : (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3 flex items-center justify-between">
            <strong className="text-sm">Game History</strong>
            {totalPages > 1 && (
              <span className="text-xs text-muted-foreground">
                {page * PAGE_SIZE + 1}–{Math.min((page + 1) * PAGE_SIZE, stats.games.length)} of {stats.games.length}
              </span>
            )}
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
                {pagedGames.map((g) => {
                  const outcome = outcomeLabel(g.isWinner, g.gameStatus)
                  return (
                    <tr key={g.gameId} className="border-b border-border last:border-0">
                      <td className="py-2 px-3 font-medium">{g.gameName}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground hidden sm:table-cell">{g.gameDefType || '—'}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground whitespace-nowrap">
                        {relativeTime(g.gameEndTime)}
                      </td>
                      <td className="py-2 px-3 font-mono">#{g.finalRank}</td>
                      <td className="py-2 px-3">
                        <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium ${outcome.className}`}>
                          {g.isWinner && <StarIcon className="h-3 w-3" />}
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
          {totalPages > 1 && (
            <div className="border-t px-4 py-2 flex items-center justify-end gap-2">
              <button
                onClick={() => setPage((p) => Math.max(0, p - 1))}
                disabled={page === 0}
                className="rounded p-1 hover:bg-secondary/40 disabled:opacity-40 disabled:cursor-not-allowed"
                aria-label="Previous page"
              >
                <ChevronLeftIcon className="h-4 w-4" />
              </button>
              <span className="text-xs text-muted-foreground">
                Page {page + 1} of {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
                disabled={page >= totalPages - 1}
                className="rounded p-1 hover:bg-secondary/40 disabled:opacity-40 disabled:cursor-not-allowed"
                aria-label="Next page"
              >
                <ChevronRightIcon className="h-4 w-4" />
              </button>
            </div>
          )}
        </div>
      )}

      {/* Achievement badges — compact 2×4 grid, silent fail if unavailable */}
      {userId && <PublicAchievementBadges userId={userId} />}
    </div>
  )
}
