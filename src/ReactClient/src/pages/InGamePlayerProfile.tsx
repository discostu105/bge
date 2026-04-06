import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useParams, Link } from 'react-router'
import { FlagIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type { InGamePlayerProfileViewModel } from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { ApiError } from '@/components/ApiError'
import { SkeletonCard, SkeletonLine } from '@/components/Skeleton'

interface InGamePlayerProfileProps {
  gameId: string
}

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

export function InGamePlayerProfile({ gameId }: InGamePlayerProfileProps) {
  const { playerId } = useParams<{ playerId: string }>()
  const [showReport, setShowReport] = useState(false)

  const { data: player, isLoading, error, refetch } = useQuery({
    queryKey: ['ingame-player', gameId, playerId],
    queryFn: () =>
      apiClient
        .get<InGamePlayerProfileViewModel>(`/api/playerranking/${encodeURIComponent(playerId ?? '')}`)
        .then((r) => r.data),
    enabled: !!playerId,
  })

  if (isLoading) return (
    <div className="max-w-md space-y-4" aria-hidden="true">
      <SkeletonLine className="h-4 w-20" />
      <div className="rounded-lg border bg-card p-6 space-y-4">
        <div className="flex items-center gap-4 animate-pulse">
          <div className="h-14 w-14 rounded-full bg-muted" />
          <div className="space-y-2">
            <div className="h-5 w-32 rounded bg-muted" />
            <div className="h-3.5 w-24 rounded bg-muted" />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      </div>
    </div>
  )
  if (error) return <ApiError message="Failed to load player profile." onRetry={() => void refetch()} />
  if (!player) return <p className="text-destructive text-sm">Player not found.</p>

  return (
    <div className="max-w-md space-y-4">
      <div className="flex items-center gap-2">
        <Link to={`/games/${gameId}/ranking`} className="text-sm text-muted-foreground hover:text-foreground">
          ← Ranking
        </Link>
      </div>

      <div className="rounded-lg border bg-card p-6 space-y-4">
        {/* Header */}
        <div className="flex items-center gap-4">
          <div className="h-14 w-14 rounded-full bg-secondary flex items-center justify-center text-xl border border-primary/40">
            {player.playerName?.[0]?.toUpperCase() ?? '?'}
          </div>
          <div className="flex-1">
            <div className="font-bold text-lg">{player.playerName}</div>
            <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <span
                className={`h-2 w-2 rounded-full ${player.isOnline ? 'bg-green-400' : 'bg-muted-foreground'}`}
              />
              {player.isOnline
                ? 'Online now'
                : player.lastOnline
                  ? `Last seen ${relativeTime(player.lastOnline)}`
                  : 'Offline'}
            </div>
          </div>
          {player.userId && (
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

        {/* Stats grid */}
        <div className="grid grid-cols-2 gap-3">
          <div className="rounded border bg-secondary/20 p-3">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Score</div>
            <div className="text-2xl font-bold">{Math.floor(player.score).toLocaleString()}</div>
          </div>
          <div className="rounded border bg-secondary/20 p-3">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Rank</div>
            <div className="text-2xl font-bold">{player.rank} <span className="text-sm text-muted-foreground font-normal">/ {player.totalPlayers}</span></div>
          </div>
          <div className="rounded border bg-secondary/20 p-3">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Techs Researched</div>
            <div className="text-2xl font-bold">{player.techsResearched}</div>
          </div>
          {player.protectionTicksRemaining > 0 && (
            <div className="rounded border border-yellow-700/50 bg-yellow-900/20 p-3">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Protection</div>
              <div className="text-lg font-semibold text-yellow-400">{player.protectionTicksRemaining} ticks</div>
            </div>
          )}
        </div>

        {/* Alliance */}
        {player.allianceName && (
          <div className="rounded border bg-secondary/20 p-3">
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Alliance</div>
            <div className="font-semibold">{player.allianceName}</div>
            {player.allianceRole && (
              <div className="text-sm text-muted-foreground capitalize">{player.allianceRole}</div>
            )}
          </div>
        )}

        {/* Agent badge */}
        {player.isAgent && (
          <div className="inline-block rounded-full border border-blue-700/50 bg-blue-900/20 px-3 py-1 text-xs text-blue-400 font-medium">
            AI Agent
          </div>
        )}
      </div>

      {showReport && player.userId && (
        <ReportModal targetUserId={player.userId} onClose={() => setShowReport(false)} />
      )}
    </div>
  )
}
