import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel, CreateGameRequest } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useSignalR } from '@/hooks/useSignalR'
import { useEffect, useCallback } from 'react'

function statusBadge(status: string): { css: string; label: string } {
  switch (status.toLowerCase()) {
    case 'upcoming': return { css: 'bg-warning text-warning-foreground', label: 'Upcoming' }
    case 'active': return { css: 'bg-success text-success-foreground', label: 'Active' }
    case 'finished': return { css: 'bg-muted text-muted-foreground', label: 'Finished' }
    default: return { css: 'bg-muted text-muted-foreground', label: status }
  }
}

function CreateGameForm({ onCreated }: { onCreated: () => void }) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const [maxPlayers, setMaxPlayers] = useState(0)
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (req: CreateGameRequest) =>
      apiClient.post('/api/games', req),
    onSuccess: () => {
      setOpen(false)
      setName('')
      setMaxPlayers(0)
      setStartTime('')
      setEndTime('')
      setError(null)
      onCreated()
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to create game.'
      setError(String(msg))
    },
  })

  function handleCreate() {
    if (!name.trim()) { setError('Game name is required.'); return }
    if (!startTime || !endTime) { setError('Start and end times are required.'); return }
    setError(null)
    createMutation.mutate({
      name: name.trim(),
      gameDefType: 'sco',
      startTime: new Date(startTime).toISOString(),
      endTime: new Date(endTime).toISOString(),
      tickDuration: '00:00:30',
      discordWebhookUrl: null,
      maxPlayers,
    })
  }

  if (!open) {
    return (
      <button
        className="px-4 py-2 rounded bg-primary text-primary-foreground text-sm font-medium hover:opacity-90"
        onClick={() => setOpen(true)}
      >
        Create Game
      </button>
    )
  }

  return (
    <div className="rounded-lg border border-border bg-card p-4 max-w-md space-y-3">
      <h2 className="font-semibold">Create New Game</h2>

      {error && (
        <div role="alert" className="rounded border border-danger/50 bg-danger/10 p-2 text-sm text-danger-foreground">{error}</div>
      )}

      <div className="space-y-1">
        <label className="text-sm font-medium">Game Name</label>
        <input type="text" className="w-full rounded border border-border bg-background px-3 py-2 text-sm"
          value={name} onChange={(e) => setName(e.target.value)} placeholder="My Game" maxLength={60} />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="text-sm font-medium">Start Time</label>
          <input type="datetime-local" className="w-full rounded border border-border bg-background px-3 py-2 text-sm"
            value={startTime} onChange={(e) => setStartTime(e.target.value)} />
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">End Time</label>
          <input type="datetime-local" className="w-full rounded border border-border bg-background px-3 py-2 text-sm"
            value={endTime} onChange={(e) => setEndTime(e.target.value)} />
        </div>
      </div>

      <div className="space-y-1">
        <label className="text-sm font-medium">Max Players (0 = unlimited)</label>
        <input type="number" className="w-full rounded border border-border bg-background px-3 py-2 text-sm"
          value={maxPlayers} onChange={(e) => setMaxPlayers(Number(e.target.value))} min={0} max={100} />
      </div>

      <div className="flex gap-2">
        <button
          className="px-4 py-2 rounded bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50"
          onClick={handleCreate} disabled={createMutation.isPending}
        >
          {createMutation.isPending ? 'Creating...' : 'Create'}
        </button>
        <button
          className="px-4 py-2 rounded border border-border text-sm hover:bg-muted/30"
          onClick={() => { setOpen(false); setError(null) }}
        >
          Cancel
        </button>
      </div>
    </div>
  )
}

export function GameLobby() {
  const queryClient = useQueryClient()
  const { on, off } = useSignalR('/gamehub')

  const { data, isLoading, error: queryError, refetch } = useQuery<GameListViewModel>({
    queryKey: ['games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['games'] })
  }, [queryClient])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  const games = data?.games ?? []
  const activeGames = games.filter(g => g.status.toLowerCase() !== 'finished')
  const finishedGames = games.filter(g => g.status.toLowerCase() === 'finished')

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Game Lobby</h1>
        <CreateGameForm onCreated={() => void queryClient.invalidateQueries({ queryKey: ['games'] })} />
      </div>

      {isLoading && <PageLoader message="Loading games..." />}
      {queryError && !data && <ApiError message="Failed to load games." onRetry={() => void refetch()} />}

      {!isLoading && activeGames.length === 0 && (
        <p className="text-muted-foreground text-sm">No active or upcoming games. Create one to get started!</p>
      )}

      {activeGames.length > 0 && (
        <GameTable games={activeGames} title="Active & Upcoming Games" />
      )}

      {finishedGames.length > 0 && (
        <details className="group">
          <summary className="cursor-pointer text-sm font-medium text-muted-foreground hover:text-foreground">
            Finished Games ({finishedGames.length})
          </summary>
          <div className="mt-2">
            <GameTable games={finishedGames} />
          </div>
        </details>
      )}
    </div>
  )
}

function GameTable({ games, title }: { games: GameSummaryViewModel[]; title?: string }) {
  return (
    <div className="space-y-2">
      {title && <h2 className="text-sm font-semibold text-muted-foreground">{title}</h2>}
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b border-border text-left text-muted-foreground">
              <th scope="col" className="py-2 pr-4">Game</th>
              <th scope="col" className="py-2 pr-4">Status</th>
              <th scope="col" className="py-2 pr-4">Players</th>
              <th scope="col" className="py-2 pr-4">Start</th>
              <th scope="col" className="py-2" />
            </tr>
          </thead>
          <tbody>
            {games.map((game) => {
              const badge = statusBadge(game.status)
              const isFinished = game.status.toLowerCase() === 'finished'
              return (
                <tr key={game.gameId} className="border-b border-border hover:bg-muted/30">
                  <td className="py-2 pr-4 font-medium">
                    <Link to={`/lobby/${game.gameId}`} className="hover:underline">{game.name}</Link>
                  </td>
                  <td className="py-2 pr-4">
                    <span className={`text-xs font-bold px-2 py-0.5 rounded ${badge.css}`}>{badge.label}</span>
                  </td>
                  <td className="py-2 pr-4">
                    {game.playerCount}{game.maxPlayers > 0 ? ` / ${game.maxPlayers}` : ''}
                  </td>
                  <td className="py-2 pr-4 text-muted-foreground">
                    {game.startTime
                      ? new Date(game.startTime).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
                      : 'TBD'}
                  </td>
                  <td className="py-2 flex gap-2">
                    {game.canJoin && !game.isPlayerEnrolled && (
                      <Link
                        to={`/games/${game.gameId}/join`}
                        className="text-xs px-2 py-1 rounded bg-primary text-primary-foreground hover:opacity-90"
                      >
                        Join
                      </Link>
                    )}
                    {game.isPlayerEnrolled && !isFinished && (
                      <Link
                        to={`/games/${game.gameId}/base`}
                        className="text-xs px-2 py-1 rounded bg-success text-success-foreground hover:opacity-90"
                      >
                        Play
                      </Link>
                    )}
                    {(game.status.toLowerCase() === 'active' || isFinished) && (
                      <Link
                        to={`/games/${game.gameId}/ranking`}
                        className="text-xs px-2 py-1 rounded border border-border hover:bg-muted/30"
                      >
                        Spectate
                      </Link>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
