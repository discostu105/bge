import { useState } from 'react'
import { useParams, useNavigate } from 'react-router'
import { useQuery, useMutation } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameDetailViewModel, JoinGameRequest, RaceListViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

function statusBadge(status: string): string {
  switch (status) {
    case 'Active': return 'bg-success text-success-foreground'
    case 'Upcoming': return 'bg-warning text-warning-foreground'
    case 'Finished': return 'bg-muted text-muted-foreground'
    default: return 'bg-muted text-muted-foreground'
  }
}

export function JoinGame() {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const [playerName, setPlayerName] = useState('')
  const [playerType, setPlayerType] = useState('terran')
  const [joinError, setJoinError] = useState<string | null>(null)
  const [alreadyJoined, setAlreadyJoined] = useState(false)

  const { data: game, error: loadError, isLoading, refetch } = useQuery<GameDetailViewModel>({
    queryKey: ['game', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}`).then((r) => r.data),
    enabled: !!gameId,
  })

  const { data: racesData } = useQuery<RaceListViewModel>({
    queryKey: ['races'],
    queryFn: () => apiClient.get('/api/games/races').then((r) => r.data),
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
    joinMutation.mutate({ playerName: playerName.trim(), playerType })
  }

  if (isLoading) return <PageLoader message="Loading game..." />
  if (loadError) return <ApiError message="Failed to load game." onRetry={() => void refetch()} />
  if (!game) return <ApiError message="Game not found." />

  const races = racesData?.races ?? [
    { id: 'terran', name: 'Terraner' },
    { id: 'protoss', name: 'Protoss' },
    { id: 'zerg', name: 'Zerg' },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Join Game</h1>

      <div className="flex items-center gap-2">
        <strong>{game.name}</strong>
        <span className={`text-xs font-bold px-2 py-0.5 rounded ${statusBadge(game.status)}`}>{game.status}</span>
      </div>

      {alreadyJoined ? (
        <div className="space-y-3">
          <div className="rounded border border-info/50 bg-info/10 p-3 text-sm">You have already joined this game.</div>
          <a href={`/games/${gameId}/base`} className="inline-block text-sm px-4 py-2 rounded bg-primary text-primary-foreground hover:opacity-90">
            Play
          </a>
        </div>
      ) : (
        <div className="rounded-lg border border-border bg-card p-4 max-w-sm space-y-4">
          <h2 className="font-semibold">Choose your commander</h2>

          {joinError && (
            <div role="alert" className="rounded border border-danger/50 bg-danger/10 p-2 text-sm text-danger-foreground">{joinError}</div>
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

          <div className="space-y-1">
            <label className="text-sm font-medium">Race</label>
            <div className="grid grid-cols-3 gap-2">
              {races.map((race) => (
                <button
                  key={race.id}
                  className={`rounded border px-3 py-2 text-sm font-medium transition-colors ${
                    playerType === race.id
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-border hover:bg-muted/30'
                  }`}
                  onClick={() => setPlayerType(race.id)}
                  type="button"
                >
                  {race.name}
                </button>
              ))}
            </div>
          </div>

          <button
            className="w-full py-2 rounded bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50"
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
