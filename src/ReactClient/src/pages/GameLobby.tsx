import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel } from '@/api/types'

function statusBadge(status: string): { css: string; label: string } {
  switch (status.toLowerCase()) {
    case 'upcoming': return { css: 'bg-yellow-500 text-black', label: 'Upcoming' }
    case 'active': return { css: 'bg-green-700 text-white', label: 'Active' }
    case 'finished': return { css: 'bg-gray-500 text-white', label: 'Finished' }
    default: return { css: 'bg-gray-300 text-black', label: status }
  }
}

export function GameLobby() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const { data, isLoading } = useQuery<GameListViewModel>({
    queryKey: ['games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const joinMutation = useMutation({
    mutationFn: (game: GameSummaryViewModel) =>
      apiClient.post(`/api/games/${game.gameId}/join`, {}),
    onSuccess: (_, game) => {
      setError(null)
      setSuccess(`You have joined "${game.name}"!`)
      void queryClient.invalidateQueries({ queryKey: ['games'] })
    },
    onError: async (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to join.'
      setError(String(msg))
    },
  })

  const games = data?.games ?? []

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Game Lobby</h1>

      {success && <div className="rounded border border-green-700 bg-green-900/20 p-3 text-sm">{success}</div>}
      {error && <div className="rounded border border-red-700 bg-red-900/20 p-3 text-sm text-red-400">{error}</div>}

      {isLoading && <p className="text-muted-foreground text-sm">Loading...</p>}

      {!isLoading && games.length === 0 && (
        <p className="text-muted-foreground text-sm">No games available.</p>
      )}

      {games.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th className="py-2 pr-4">Game</th>
                <th className="py-2 pr-4">Status</th>
                <th className="py-2 pr-4">Players</th>
                <th className="py-2 pr-4">Start Time</th>
                <th className="py-2" />
              </tr>
            </thead>
            <tbody>
              {games.map((game) => {
                const badge = statusBadge(game.status)
                return (
                  <tr key={game.gameId} className="border-b border-border hover:bg-muted/30">
                    <td className="py-2 pr-4 font-medium">{game.name}</td>
                    <td className="py-2 pr-4">
                      <span className={`text-xs font-bold px-2 py-0.5 rounded ${badge.css}`}>{badge.label}</span>
                    </td>
                    <td className="py-2 pr-4">
                      {game.playerCount} / {game.maxPlayers}
                    </td>
                    <td className="py-2 pr-4 text-muted-foreground">
                      {game.startTime
                        ? new Date(game.startTime).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
                        : 'TBD'}
                    </td>
                    <td className="py-2">
                      {game.canJoin && (
                        <button
                          className="text-xs px-2 py-1 rounded bg-blue-700 text-white hover:bg-blue-600"
                          onClick={() => joinMutation.mutate(game)}
                          disabled={joinMutation.isPending}
                        >
                          Join
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
