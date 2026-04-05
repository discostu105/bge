import { useQuery } from '@tanstack/react-query'
import { useParams, Link } from 'react-router'
import apiClient from '@/api/client'
import type { InGamePlayerProfileViewModel } from '@/api/types'
import { relativeTime } from '@/lib/utils'

interface InGamePlayerProfileProps {
  gameId: string
}

export function InGamePlayerProfile({ gameId }: InGamePlayerProfileProps) {
  const { playerId } = useParams<{ playerId: string }>()

  const { data: player, isLoading } = useQuery({
    queryKey: ['ingame-player', gameId, playerId],
    queryFn: () =>
      apiClient
        .get<InGamePlayerProfileViewModel>(`/api/playerranking/${encodeURIComponent(playerId ?? '')}`)
        .then((r) => r.data),
    enabled: !!playerId,
  })

  if (isLoading) return <p className="text-muted-foreground text-sm">Loading…</p>
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
          <div>
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
    </div>
  )
}
