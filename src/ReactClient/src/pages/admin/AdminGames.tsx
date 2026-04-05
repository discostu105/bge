import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel, CreateGameRequest, UpdateGameRequest } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface EditState {
  gameId: string
  name: string
  endTime: string
  discordWebhookUrl: string
}

function toDatetimeLocal(iso: string | null | undefined): string {
  if (!iso) return ''
  return new Date(iso).toISOString().slice(0, 16)
}

function toUtcIso(local: string): string {
  return new Date(local).toISOString()
}

export function AdminGames() {
  const queryClient = useQueryClient()

  const [createName, setCreateName] = useState('')
  const [createTickDuration, setCreateTickDuration] = useState('00:01:00')
  const [createStartTime, setCreateStartTime] = useState(toDatetimeLocal(new Date(Date.now() + 3600000).toISOString()))
  const [createEndTime, setCreateEndTime] = useState(toDatetimeLocal(new Date(Date.now() + 7 * 86400000).toISOString()))
  const [createError, setCreateError] = useState<string | null>(null)
  const [createSuccess, setCreateSuccess] = useState<string | null>(null)

  const [editState, setEditState] = useState<EditState | null>(null)
  const [editError, setEditError] = useState<string | null>(null)
  const [editSuccess, setEditSuccess] = useState<string | null>(null)
  const [finalizeError, setFinalizeError] = useState<string | null>(null)

  const { data, isLoading, error, refetch } = useQuery<GameListViewModel>({
    queryKey: ['admin-games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
  })

  const createMutation = useMutation({
    mutationFn: (req: CreateGameRequest) => apiClient.post('/api/games', req),
    onSuccess: (_, req) => {
      setCreateSuccess(`Game '${req.name}' created.`)
      setCreateName('')
      setCreateError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-games'] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) setCreateError('You must be logged in.')
        else setCreateError(`Failed: ${err.response?.data ?? err.message}`)
      } else {
        setCreateError('Unknown error.')
      }
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ gameId, req }: { gameId: string; req: UpdateGameRequest }) =>
      apiClient.patch(`/api/games/${gameId}`, req),
    onSuccess: () => {
      setEditSuccess('Game updated.')
      setEditState(null)
      setEditError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-games'] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401 || err.response?.status === 403)
          setEditError('Not authorized.')
        else setEditError(`Failed: ${err.response?.data ?? err.message}`)
      } else {
        setEditError('Unknown error.')
      }
    },
  })

  const finalizeMutation = useMutation({
    mutationFn: (gameId: string) => apiClient.post(`/api/games/${gameId}/finalize`, {}),
    onSuccess: () => {
      setFinalizeError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-games'] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 403) setFinalizeError('Not authorized to finalize this game.')
        else setFinalizeError(`Failed: ${err.response?.data ?? err.message}`)
      } else {
        setFinalizeError('Unknown error.')
      }
    },
  })

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    setCreateSuccess(null)
    createMutation.mutate({
      name: createName,
      gameDefType: 'sco',
      startTime: toUtcIso(createStartTime),
      endTime: toUtcIso(createEndTime),
      tickDuration: createTickDuration,
      discordWebhookUrl: null,
    })
  }

  const startEdit = (game: GameSummaryViewModel) => {
    setEditState({
      gameId: game.gameId,
      name: game.name,
      endTime: toDatetimeLocal(game.endTime),
      discordWebhookUrl: game.discordWebhookUrl ?? '',
    })
    setEditError(null)
    setEditSuccess(null)
  }

  const handleSaveEdit = () => {
    if (!editState) return
    updateMutation.mutate({
      gameId: editState.gameId,
      req: {
        name: editState.name,
        endTime: toUtcIso(editState.endTime),
        discordWebhookUrl: editState.discordWebhookUrl || null,
      },
    })
  }

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <h1 className="text-2xl font-bold mb-4">Game Admin</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold mb-4">Game Admin</h1>
        <ApiError message="Failed to load games." onRetry={() => void refetch()} />
      </div>
    )
  }

  const games = data?.games ?? []

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Game Admin</h1>

      {/* Create Game */}
      <div className="rounded-lg border border-border bg-card p-4 mb-8">
        <h2 className="text-lg font-semibold mb-4">Create Game</h2>
        <form onSubmit={handleCreate} className="grid gap-3 max-w-md">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input
              className="w-full rounded border border-input bg-background px-3 py-2 text-sm"
              placeholder="Game name"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Tick Duration (HH:MM:SS)</label>
            <input
              className="w-full rounded border border-input bg-background px-3 py-2 text-sm"
              placeholder="00:01:00"
              value={createTickDuration}
              onChange={(e) => setCreateTickDuration(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Start Time</label>
            <input
              type="datetime-local"
              className="w-full rounded border border-input bg-background px-3 py-2 text-sm"
              value={createStartTime}
              onChange={(e) => setCreateStartTime(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">End Time</label>
            <input
              type="datetime-local"
              className="w-full rounded border border-input bg-background px-3 py-2 text-sm"
              value={createEndTime}
              onChange={(e) => setCreateEndTime(e.target.value)}
              required
            />
          </div>
          {createError && <p className="text-destructive text-sm">{createError}</p>}
          {createSuccess && <p className="text-success-foreground text-sm">{createSuccess}</p>}
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="px-4 py-2 rounded bg-primary text-primary-foreground hover:bg-primary/90 text-sm disabled:opacity-50"
          >
            {createMutation.isPending ? 'Creating…' : 'Create Game'}
          </button>
        </form>
      </div>

      {/* Games List */}
      <h2 className="text-lg font-semibold mb-3">Games</h2>
      {finalizeError && (
        <div className="rounded border border-destructive/50 bg-destructive/10 p-3 text-destructive text-sm mb-3">
          {finalizeError}
        </div>
      )}
      {isLoading ? (
        <PageLoader message="Loading games..." />
      ) : games.length === 0 ? (
        <p className="text-muted-foreground">No games found.</p>
      ) : (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th scope="col" className="px-3 py-2 text-left font-medium">Name</th>
                <th scope="col" className="px-3 py-2 text-left font-medium">Status</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Players</th>
                <th scope="col" className="px-3 py-2 text-left font-medium hidden md:table-cell">Start</th>
                <th scope="col" className="px-3 py-2 text-left font-medium hidden md:table-cell">End</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {games.map((game) => (
                <>
                  <tr key={game.gameId} className="border-b border-border/50">
                    <td className="px-3 py-2 font-medium">{game.name}</td>
                    <td className="px-3 py-2">
                      <span
                        className={`text-xs px-2 py-0.5 rounded font-bold ${
                          game.status === 'Active'
                            ? 'bg-success text-success-foreground'
                            : game.status === 'Upcoming'
                            ? 'bg-warning text-warning-foreground'
                            : 'bg-secondary text-secondary-foreground'
                        }`}
                      >
                        {game.status}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-right text-muted-foreground">{game.playerCount}</td>
                    <td className="px-3 py-2 text-muted-foreground hidden md:table-cell">
                      {game.startTime ? new Date(game.startTime).toLocaleString() : '—'}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground hidden md:table-cell">
                      {game.endTime ? new Date(game.endTime).toLocaleString() : '—'}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <a
                          href={`/games/${game.gameId}/ranking`}
                          className="px-2 py-1 rounded border border-border text-xs hover:bg-muted"
                        >
                          View
                        </a>
                        {editState?.gameId !== game.gameId && (
                          <button
                            onClick={() => startEdit(game)}
                            className="px-2 py-1 rounded border border-border text-xs hover:bg-muted"
                          >
                            Edit
                          </button>
                        )}
                        {game.status === 'Active' && (
                          <button
                            onClick={() => finalizeMutation.mutate(game.gameId)}
                            disabled={finalizeMutation.isPending}
                            className="px-2 py-1 rounded bg-destructive text-destructive-foreground text-xs hover:bg-destructive/90 disabled:opacity-50"
                          >
                            Finalize
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                  {editState?.gameId === game.gameId && (
                    <tr key={`${game.gameId}-edit`} className="border-b border-border/50 bg-muted/20">
                      <td colSpan={6} className="px-4 py-4">
                        <div className="grid gap-3 max-w-sm">
                          <div>
                            <label className="block text-xs font-medium mb-1">Name</label>
                            <input
                              className="w-full rounded border border-input bg-background px-3 py-1.5 text-sm"
                              value={editState.name}
                              onChange={(e) => setEditState({ ...editState, name: e.target.value })}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium mb-1">Discord Webhook URL</label>
                            <input
                              className="w-full rounded border border-input bg-background px-3 py-1.5 text-sm"
                              placeholder="(optional)"
                              value={editState.discordWebhookUrl}
                              onChange={(e) => setEditState({ ...editState, discordWebhookUrl: e.target.value })}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium mb-1">End Time</label>
                            <input
                              type="datetime-local"
                              className="w-full rounded border border-input bg-background px-3 py-1.5 text-sm"
                              value={editState.endTime}
                              onChange={(e) => setEditState({ ...editState, endTime: e.target.value })}
                            />
                          </div>
                          {editError && <p className="text-destructive text-sm">{editError}</p>}
                          {editSuccess && <p className="text-success-foreground text-sm">{editSuccess}</p>}
                          <div className="flex gap-2">
                            <button
                              onClick={handleSaveEdit}
                              disabled={updateMutation.isPending}
                              className="px-3 py-1.5 rounded bg-primary text-primary-foreground text-sm hover:bg-primary/90 disabled:opacity-50"
                            >
                              {updateMutation.isPending ? 'Saving…' : 'Save'}
                            </button>
                            <button
                              onClick={() => setEditState(null)}
                              className="px-3 py-1.5 rounded border border-border text-sm hover:bg-muted"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                </>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
