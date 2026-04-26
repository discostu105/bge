import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { AdminNav } from './AdminNav'
import { AdminGamePickerPage } from './AdminGamePicker'

interface LiveStats {
  totalPlayers: number
  activePlayers: number
  bannedPlayers: number
  totalResources: Record<string, number>
  openMarketOrders: number
  currentTick: number
  isPaused: boolean
  effectiveTickDurationSeconds: number
  dailyActiveUsers: number
  runningGames: number
  totalGames: number
}

export function AdminMetrics() {
  const { gameId } = useParams<{ gameId: string }>()
  const { data: stats, isLoading, error, refetch } = useQuery<LiveStats>({
    queryKey: ['admin-live-stats', gameId ?? null],
    queryFn: () => apiClient.get('/api/admin/live-stats').then((r) => r.data),
    refetchInterval: 10000,
    enabled: !!gameId,
  })

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <AdminNav currentGameId={gameId ?? null} />
          <h1 className="text-2xl font-bold mb-4">Metrics</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <AdminNav currentGameId={gameId ?? null} />
        <h1 className="text-2xl font-bold mb-4">Metrics</h1>
        <ApiError message="Failed to load metrics." onRetry={() => void refetch()} />
      </div>
    )
  }

  if (!gameId) {
    return (
      <div className="p-6 max-w-4xl mx-auto">
        <AdminNav />
        <h1 className="text-2xl font-bold mb-6">Metrics</h1>
        <AdminGamePickerPage section="metrics" title="Metrics" />
      </div>
    )
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <AdminNav currentGameId={gameId} />
      <h1 className="text-2xl font-bold mb-6">Metrics</h1>

      {isLoading || !stats ? (
        <PageLoader message="Loading metrics..." />
      ) : (
        <div className="space-y-6">
          {/* Across all games (truly global) */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Across all games</h2>
            <div className="grid grid-cols-3 gap-4">
              <div className="text-center">
                <div className="text-3xl font-bold">{stats.totalGames}</div>
                <div className="text-xs text-muted-foreground">Total games</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold text-success-foreground">{stats.runningGames}</div>
                <div className="text-xs text-muted-foreground">Running</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold text-primary">{stats.dailyActiveUsers}</div>
                <div className="text-xs text-muted-foreground">DAU (24h)</div>
              </div>
            </div>
          </div>

          {/* Player Stats */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Players</h2>
            <div className="grid grid-cols-3 gap-4">
              <div className="text-center">
                <div className="text-3xl font-bold">{stats.totalPlayers}</div>
                <div className="text-xs text-muted-foreground">Total</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold text-success-foreground">{stats.activePlayers}</div>
                <div className="text-xs text-muted-foreground">Active (15m)</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold text-destructive">{stats.bannedPlayers}</div>
                <div className="text-xs text-muted-foreground">Banned</div>
              </div>
            </div>
          </div>

          {/* Tick Info */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Tick Engine</h2>
            <div className="grid grid-cols-3 gap-4">
              <div className="text-center">
                <div className="text-3xl font-bold">{stats.currentTick}</div>
                <div className="text-xs text-muted-foreground">Current Tick</div>
              </div>
              <div className="text-center">
                <div className={`text-3xl font-bold ${stats.isPaused ? 'text-warning' : 'text-success-foreground'}`}>
                  {stats.isPaused ? 'Paused' : 'Running'}
                </div>
                <div className="text-xs text-muted-foreground">Status</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold">{stats.effectiveTickDurationSeconds}s</div>
                <div className="text-xs text-muted-foreground">Tick Duration</div>
              </div>
            </div>
          </div>

          {/* Economy */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Economy</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {Object.entries(stats.totalResources).map(([key, val]) => (
                <div key={key} className="text-center">
                  <div className="text-2xl font-bold">{Number(val).toLocaleString()}</div>
                  <div className="text-xs text-muted-foreground capitalize">{key}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Market */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Market</h2>
            <div className="text-center">
              <div className="text-3xl font-bold">{stats.openMarketOrders}</div>
              <div className="text-xs text-muted-foreground">Open Market Orders</div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
