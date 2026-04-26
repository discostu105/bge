import { useState } from 'react'
import { useParams } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { AdminNav } from './AdminNav'
import { AdminGamePickerPage } from './AdminGamePicker'

interface AdminPlayerEntry {
  playerId: string
  name: string
  playerType: string
  created: string
  lastOnline: string | null
  isBanned: boolean
  allianceId: string | null
  land: number
  unitCount: number
  assetCount: number
}

interface AdminPlayerDetail extends AdminPlayerEntry {
  resources: Record<string, number>
  currentTick: number
  protectionTicksRemaining: number
  gasPercent: number
}

export function AdminPlayers() {
  const { gameId } = useParams<{ gameId: string }>()
  const queryClient = useQueryClient()
  const [selectedPlayer, setSelectedPlayer] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')

  const { data: players, isLoading, error, refetch } = useQuery<AdminPlayerEntry[]>({
    queryKey: ['admin-players', gameId ?? null],
    queryFn: () => apiClient.get('/api/admin/players-detail').then((r) => r.data),
    enabled: !!gameId,
  })

  const { data: detail } = useQuery<AdminPlayerDetail>({
    queryKey: ['admin-player-detail', gameId ?? null, selectedPlayer],
    queryFn: () => apiClient.get(`/api/admin/players/${selectedPlayer}`).then((r) => r.data),
    enabled: !!gameId && !!selectedPlayer,
  })

  const banMutation = useMutation({
    mutationFn: (playerId: string) => apiClient.post('/api/admin/ban-player', { playerId }),
    onSuccess: () => {
      setActionError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-players'] })
      queryClient.invalidateQueries({ queryKey: ['admin-player-detail'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Ban failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  const unbanMutation = useMutation({
    mutationFn: (playerId: string) => apiClient.post('/api/admin/unban-player', { playerId }),
    onSuccess: () => {
      setActionError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-players'] })
      queryClient.invalidateQueries({ queryKey: ['admin-player-detail'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Unban failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  const resetMutation = useMutation({
    mutationFn: (playerId: string) => apiClient.post('/api/admin/reset-player', { playerId }),
    onSuccess: () => {
      setActionError(null)
      queryClient.invalidateQueries({ queryKey: ['admin-players'] })
      queryClient.invalidateQueries({ queryKey: ['admin-player-detail'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Reset failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <AdminNav currentGameId={gameId ?? null} />
          <h1 className="text-2xl font-bold mb-4">Player Moderation</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <AdminNav currentGameId={gameId ?? null} />
        <h1 className="text-2xl font-bold mb-4">Player Moderation</h1>
        <ApiError message="Failed to load players." onRetry={() => void refetch()} />
      </div>
    )
  }

  if (!gameId) {
    return (
      <div className="p-6 max-w-6xl mx-auto">
        <AdminNav />
        <h1 className="text-2xl font-bold mb-6">Player Moderation</h1>
        <AdminGamePickerPage section="players" title="Players" />
      </div>
    )
  }

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <AdminNav currentGameId={gameId} />
      <h1 className="text-2xl font-bold mb-6">Player Moderation</h1>

      <div className="mb-4">
        <input
          type="text"
          placeholder="Search players by name..."
          className="w-full max-w-sm border border-border rounded px-3 py-2 text-sm bg-background"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      {actionError && (
        <div className="rounded border border-destructive/50 bg-destructive/10 p-3 text-destructive text-sm mb-4">
          {actionError}
        </div>
      )}

      {isLoading ? (
        <PageLoader message="Loading players..." />
      ) : !players || players.length === 0 ? (
        <p className="text-muted-foreground">No players found.</p>
      ) : (() => {
        const filtered = players.filter(p =>
          !searchQuery || p.name.toLowerCase().includes(searchQuery.toLowerCase())
        )
        return filtered.length === 0 ? (
          <p className="text-muted-foreground">No players match your search.</p>
        ) : (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th scope="col" className="px-3 py-2 text-left font-medium">Name</th>
                <th scope="col" className="px-3 py-2 text-left font-medium">Type</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Land</th>
                <th scope="col" className="px-3 py-2 text-right font-medium hidden md:table-cell">Units</th>
                <th scope="col" className="px-3 py-2 text-left font-medium hidden md:table-cell">Last Online</th>
                <th scope="col" className="px-3 py-2 text-left font-medium">Status</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((player) => (
                <>
                  <tr key={player.playerId} className="border-b border-border/50">
                    <td className="px-3 py-2 font-medium">{player.name}</td>
                    <td className="px-3 py-2 text-muted-foreground">{player.playerType}</td>
                    <td className="px-3 py-2 text-right">{player.land}</td>
                    <td className="px-3 py-2 text-right text-muted-foreground hidden md:table-cell">{player.unitCount}</td>
                    <td className="px-3 py-2 text-muted-foreground hidden md:table-cell">
                      {player.lastOnline ? new Date(player.lastOnline).toLocaleString() : 'Never'}
                    </td>
                    <td className="px-3 py-2">
                      {player.isBanned ? (
                        <span className="text-xs px-2 py-0.5 rounded font-bold bg-destructive text-destructive-foreground">
                          Banned
                        </span>
                      ) : (
                        <span className="text-xs px-2 py-0.5 rounded font-bold bg-success text-success-foreground">
                          Active
                        </span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => setSelectedPlayer(selectedPlayer === player.playerId ? null : player.playerId)}
                          className="px-2 py-1 rounded border border-border text-xs hover:bg-muted"
                        >
                          {selectedPlayer === player.playerId ? 'Hide' : 'Details'}
                        </button>
                        {player.isBanned ? (
                          <button
                            onClick={() => unbanMutation.mutate(player.playerId)}
                            disabled={unbanMutation.isPending}
                            className="px-2 py-1 rounded bg-success text-success-foreground text-xs hover:bg-success/90 disabled:opacity-50"
                          >
                            Unban
                          </button>
                        ) : (
                          <button
                            onClick={() => banMutation.mutate(player.playerId)}
                            disabled={banMutation.isPending}
                            className="px-2 py-1 rounded bg-destructive text-destructive-foreground text-xs hover:bg-destructive/90 disabled:opacity-50"
                          >
                            Ban
                          </button>
                        )}
                        <button
                          onClick={() => {
                            if (window.confirm(`Reset player "${player.name}" to initial state?`)) {
                              resetMutation.mutate(player.playerId)
                            }
                          }}
                          disabled={resetMutation.isPending}
                          className="px-2 py-1 rounded border border-destructive text-destructive text-xs hover:bg-destructive/10 disabled:opacity-50"
                        >
                          Reset
                        </button>
                      </div>
                    </td>
                  </tr>
                  {selectedPlayer === player.playerId && detail && (
                    <tr key={`${player.playerId}-detail`} className="border-b border-border/50 bg-muted/20">
                      <td colSpan={7} className="px-4 py-4">
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                          <div>
                            <div className="text-xs text-muted-foreground">Player ID</div>
                            <div className="font-mono text-xs">{detail.playerId}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Created</div>
                            <div>{new Date(detail.created).toLocaleString()}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Alliance</div>
                            <div>{detail.allianceId ?? 'None'}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Current Tick</div>
                            <div>{detail.currentTick}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Protection Ticks</div>
                            <div>{detail.protectionTicksRemaining}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Gas %</div>
                            <div>{detail.gasPercent}%</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Assets</div>
                            <div>{detail.assetCount}</div>
                          </div>
                          <div>
                            <div className="text-xs text-muted-foreground">Units</div>
                            <div>{detail.unitCount}</div>
                          </div>
                        </div>
                        {detail.resources && Object.keys(detail.resources).length > 0 && (
                          <div className="mt-3">
                            <div className="text-xs text-muted-foreground mb-1">Resources</div>
                            <div className="flex gap-4 flex-wrap">
                              {Object.entries(detail.resources).map(([key, val]) => (
                                <div key={key} className="text-sm">
                                  <span className="font-medium">{key}:</span>{' '}
                                  <span className="text-muted-foreground">{Number(val).toLocaleString()}</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </td>
                    </tr>
                  )}
                </>
              ))}
            </tbody>
          </table>
        </div>
        )
      })()}
    </div>
  )
}
