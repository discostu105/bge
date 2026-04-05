import { useState } from 'react'
import { useParams, useNavigate } from 'react-router'
import { useQuery, useMutation } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameDetailViewModel, JoinGameRequest } from '@/api/types'

function statusBadge(status: string): string {
  switch (status) {
    case 'Active': return 'bg-green-700 text-white'
    case 'Upcoming': return 'bg-yellow-500 text-black'
    case 'Finished': return 'bg-gray-500 text-white'
    default: return 'bg-gray-300 text-black'
  }
}

export function JoinGame() {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const [playerName, setPlayerName] = useState('')
  const [joinError, setJoinError] = useState<string | null>(null)
  const [alreadyJoined, setAlreadyJoined] = useState(false)

  const { data: game, error: loadError, isLoading } = useQuery<GameDetailViewModel>({
    queryKey: ['game', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}`).then((r) => r.data),
    enabled: !!gameId,
  })

  const joinMutation = useMutation({
    mutationFn: (req: JoinGameRequest) =>
      apiClient.post(`/api/games/${gameId}/join`, req),
    onSuccess: () => {
      void navigate(`/games/${gameId}/base`)
    },
    onError: async (err: unknown) => {
      const status = (err as { response?: { status?: number; data?: string } })?.response?.status
      if (status === 409) {
        setAlreadyJoined(true)
      } else {
        const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to join.'
        setJoinError(String(msg))
      }
    },
  })

  function handleJoin() {
    if (!playerName.trim()) {
      setJoinError('Please enter a commander name.')
      return
    }
    setJoinError(null)
    joinMutation.mutate({ playerName: playerName.trim() })
  }

  if (isLoading) return <p className="text-muted-foreground text-sm">Loading...</p>

  if (loadError || !game) {
    return (
      <div className="space-y-3">
        <div className="rounded border border-red-700 bg-red-900/20 p-3 text-sm text-red-400">Game not found.</div>
        <a href="/games" className="text-sm text-blue-400 hover:underline">← Back to Games</a>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Join Game</h1>

      <div className="flex items-center gap-2">
        <strong>{game.name}</strong>
        <span className={`text-xs font-bold px-2 py-0.5 rounded ${statusBadge(game.status)}`}>{game.status}</span>
      </div>

      {alreadyJoined ? (
        <div className="space-y-3">
          <div className="rounded border border-blue-700 bg-blue-900/20 p-3 text-sm">You have already joined this game.</div>
          <a href={`/games/${gameId}/base`} className="inline-block text-sm px-4 py-2 rounded bg-blue-700 text-white hover:bg-blue-600">
            Play
          </a>
        </div>
      ) : (
        <div className="rounded-lg border border-border bg-card p-4 max-w-sm space-y-4">
          <h2 className="font-semibold">Choose your commander</h2>

          {joinError && (
            <div className="rounded border border-red-700 bg-red-900/20 p-2 text-sm text-red-400">{joinError}</div>
          )}

          <div className="space-y-1">
            <label className="text-sm font-medium">Commander Name</label>
            <input
              type="text"
              className="w-full rounded border border-border bg-background px-3 py-2 text-sm"
              value={playerName}
              onChange={(e) => setPlayerName(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleJoin()}
              placeholder="Enter your name"
              maxLength={32}
            />
          </div>

          <button
            className="w-full py-2 rounded bg-blue-700 text-white text-sm font-medium hover:bg-blue-600 disabled:opacity-50"
            onClick={handleJoin}
            disabled={joinMutation.isPending}
          >
            {joinMutation.isPending ? 'Joining...' : 'Join Game'}
          </button>
        </div>
      )}
    </div>
  )
}
