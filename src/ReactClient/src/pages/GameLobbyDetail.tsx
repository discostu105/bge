import { useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameLobbyViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useSignalR } from '@/hooks/useSignalR'

function statusBadge(status: string): { css: string; label: string } {
  switch (status.toLowerCase()) {
    case 'upcoming': return { css: 'bg-warning text-warning-foreground', label: 'Upcoming' }
    case 'active': return { css: 'bg-success text-success-foreground', label: 'Active' }
    case 'finished': return { css: 'bg-muted text-muted-foreground', label: 'Finished' }
    default: return { css: 'bg-muted text-muted-foreground', label: status }
  }
}

function raceLabel(playerType: string): string {
  switch (playerType) {
    case 'terran': return 'Terran'
    case 'protoss': return 'Protoss'
    case 'zerg': return 'Zerg'
    default: return playerType
  }
}

export function GameLobbyDetail() {
  const { gameId } = useParams<{ gameId: string }>()
  const queryClient = useQueryClient()
  const { on, off } = useSignalR('/gamehub')

  const { data: lobby, isLoading, error, refetch } = useQuery<GameLobbyViewModel>({
    queryKey: ['game-lobby', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/lobby`).then((r) => r.data),
    enabled: !!gameId,
    refetchInterval: 15_000,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['game-lobby', gameId] })
  }, [queryClient, gameId])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  if (isLoading) return <PageLoader message="Loading lobby..." />
  if (error) return <ApiError message="Failed to load lobby." onRetry={() => void refetch()} />
  if (!lobby) return <ApiError message="Game not found." />

  const badge = statusBadge(lobby.status)
  const isActive = lobby.status.toLowerCase() === 'active'
  const isFinished = lobby.status.toLowerCase() === 'finished'

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Link to="/lobby" className="text-muted-foreground hover:text-foreground text-sm">&larr; Back</Link>
        <h1 className="text-2xl font-bold">{lobby.gameName}</h1>
        <span className={`text-xs font-bold px-2 py-0.5 rounded ${badge.css}`}>{badge.label}</span>
      </div>

      <div className="flex gap-6 text-sm text-muted-foreground">
        <div>
          <span className="font-medium text-foreground">Players:</span>{' '}
          {lobby.players.length}{lobby.maxPlayers > 0 ? ` / ${lobby.maxPlayers}` : ''}
        </div>
        {lobby.startTime && (
          <div>
            <span className="font-medium text-foreground">Starts:</span>{' '}
            {new Date(lobby.startTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
          </div>
        )}
        {lobby.endTime && (
          <div>
            <span className="font-medium text-foreground">Ends:</span>{' '}
            {new Date(lobby.endTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
          </div>
        )}
      </div>

      <div className="flex gap-2">
        {lobby.canJoin && (
          <Link
            to={`/games/${gameId}/join`}
            className="px-4 py-2 rounded bg-primary text-primary-foreground text-sm font-medium hover:opacity-90"
          >
            Join Game
          </Link>
        )}
        {(isActive || isFinished) && (
          <Link
            to={`/games/${gameId}/ranking`}
            className="px-4 py-2 rounded border border-border text-sm hover:bg-muted/30"
          >
            {isFinished ? 'View Results' : 'Spectate'}
          </Link>
        )}
      </div>

      <div className="space-y-2">
        <h2 className="text-sm font-semibold text-muted-foreground">Players</h2>
        {lobby.players.length === 0 ? (
          <p className="text-muted-foreground text-sm">No players have joined yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm border-collapse">
              <thead>
                <tr className="border-b border-border text-left text-muted-foreground">
                  <th scope="col" className="py-2 pr-4">Commander</th>
                  <th scope="col" className="py-2 pr-4">Race</th>
                  <th scope="col" className="py-2 pr-4">Joined</th>
                </tr>
              </thead>
              <tbody>
                {lobby.players.map((p) => (
                  <tr key={p.playerId} className="border-b border-border">
                    <td className="py-2 pr-4 font-medium">{p.playerName}</td>
                    <td className="py-2 pr-4">{raceLabel(p.playerType)}</td>
                    <td className="py-2 pr-4 text-muted-foreground">
                      {new Date(p.joined).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
