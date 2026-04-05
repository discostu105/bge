import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  AssetsViewModel,
  AssetViewModel,
  PlayerResourcesViewModel,
  PlayerNotificationViewModel,
  GameDetailViewModel,
  TradeResourceRequest,
} from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { BuildUnitsForm } from '@/components/BuildUnitsForm'
import { WorkerAssignment } from '@/components/WorkerAssignment'
import { CostBadge } from '@/components/CostBadge'
import { relativeTime } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface BaseProps {
  gameId: string
}

function notificationIcon(kind: string): string {
  switch (kind) {
    case 'Warning': return '⚠️'
    case 'GameEvent': return '⚡'
    default: return 'ℹ️'
  }
}

function AssetCard({
  asset,
  gameId,
  onBuild,
  onQueue,
}: {
  asset: AssetViewModel
  gameId: string
  onBuild: (defId: string) => void
  onQueue: (defId: string) => void
}) {
  return (
    <div
      className={`rounded-lg border p-4 ${
        asset.built
          ? 'border-green-700 bg-green-900/20'
          : 'border-border bg-card'
      }`}
    >
      <h3 className="font-semibold text-sm mb-2">{asset.definition.name}</h3>

      {asset.built ? (
        <>
          <div className="text-xs text-green-400 mb-2">Built ✓</div>
          <BuildUnitsForm
            availableUnits={asset.availableUnits}
            gameId={gameId}
          />
        </>
      ) : asset.alreadyQueued ? (
        <div className="text-xs text-muted-foreground">
          Queued — ready in {asset.ticksLeftForBuild} rounds
        </div>
      ) : (
        <>
          <div className="text-xs mb-1">
            Cost: <CostBadge cost={asset.cost} />
          </div>
          {asset.prerequisites && (
            <div className="text-xs text-muted-foreground mb-2">
              Requires: {asset.prerequisites}
            </div>
          )}
          {asset.prerequisitesMet && (
            <div className="flex gap-2 mt-2">
              {asset.canAfford && (
                <button
                  onClick={() => onBuild(asset.definition.id)}
                  className="rounded bg-primary min-h-[36px] px-3 py-1.5 text-xs text-primary-foreground hover:opacity-90"
                >
                  Build
                </button>
              )}
              <button
                onClick={() => onQueue(asset.definition.id)}
                className="rounded bg-secondary min-h-[36px] px-3 py-1.5 text-xs text-secondary-foreground hover:opacity-90"
              >
                Queue
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export function Base({ gameId }: BaseProps) {
  const queryClient = useQueryClient()
  const [lastError, setLastError] = useState<string | null>(null)
  const [tradeFromResource, setTradeFromResource] = useState('minerals')
  const [tradeAmount, setTradeAmount] = useState(1)
  const [tradeError, setTradeError] = useState<string | null>(null)
  const [colonizeAmount, setColonizeAmount] = useState(1)
  const [colonizeError, setColonizeError] = useState<string | null>(null)

  const { data: assetsData, isLoading: assetsLoading, error: assetsError, refetch: refetchAssets } = useQuery<AssetsViewModel>({
    queryKey: ['assets', gameId],
    queryFn: () => apiClient.get('/api/assets').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const { data: resources } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const { data: gameDetail } = useQuery<GameDetailViewModel>({
    queryKey: ['gamedetail', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}`).then((r) => r.data),
    refetchInterval: 30_000,
    enabled: !!gameId,
  })

  const { data: notifications, refetch: refetchNotifications } = useQuery<
    PlayerNotificationViewModel[]
  >({
    queryKey: ['notifications', gameId],
    queryFn: () => apiClient.get('/api/notifications/recent').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const buildMutation = useMutation({
    mutationFn: (assetDefId: string) =>
      apiClient.post(`/api/assets/build?assetDefId=${assetDefId}`, ''),
    onSuccess: () => {
      setLastError(null)
      void queryClient.invalidateQueries({ queryKey: ['assets', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Build failed')
    },
  })

  const queueAssetMutation = useMutation({
    mutationFn: (defId: string) =>
      apiClient.post('/api/buildqueue/add', { type: 'asset', defId, count: 1 }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Queue failed')
    },
  })

  const tradeMutation = useMutation({
    mutationFn: () => {
      const request: TradeResourceRequest = { fromResource: tradeFromResource, amount: tradeAmount }
      return apiClient.post('/api/resources/trade', request)
    },
    onSuccess: () => {
      setTradeError(null)
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setTradeError((err.response?.data as string) ?? 'Trade failed')
    },
  })

  const colonizeMutation = useMutation({
    mutationFn: () =>
      apiClient.post(`/api/colonize/colonize?amount=${colonizeAmount}`, ''),
    onSuccess: () => {
      setColonizeError(null)
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err))
        setColonizeError((err.response?.data as string) ?? 'Colonize failed')
    },
  })

  const clearNotificationsMutation = useMutation({
    mutationFn: () => apiClient.delete('/api/notifications/recent'),
    onSuccess: () => void refetchNotifications(),
  })

  const isFinished = gameDetail?.status === 'Finished'

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Base</h1>

      {isFinished && (
        <div className="rounded-lg border border-yellow-600 bg-yellow-900/20 p-4 text-yellow-200">
          <strong>Game Over!</strong> The game has ended.{' '}
          <a href={`/games/${gameId}/results`} className="underline">
            View final results
          </a>
          .
        </div>
      )}

      <WorkerAssignment gameId={gameId} />

      {/* Trade */}
      <div className="rounded-lg border bg-card p-4">
        <h3 className="font-semibold mb-3">Trade</h3>
        {tradeError && <div className="text-destructive text-sm mb-2">{tradeError}</div>}
        <div className="flex flex-wrap items-center gap-3">
          <select
            value={tradeFromResource}
            onChange={(e) => setTradeFromResource(e.target.value)}
            className="rounded border bg-input px-2 py-1 text-sm"
          >
            <option value="minerals">Minerals → Gas</option>
            <option value="gas">Gas → Minerals</option>
          </select>
          <input
            type="number"
            min={1}
            value={tradeAmount}
            onChange={(e) => setTradeAmount(Number(e.target.value))}
            className="w-24 rounded border bg-input px-2 py-1 text-sm"
          />
          <span className="text-sm text-muted-foreground">
            Cost: {tradeAmount * 2} → receive: {tradeAmount}
          </span>
          <button
            onClick={() => tradeMutation.mutate()}
            disabled={tradeMutation.isPending}
            className="rounded bg-yellow-600 min-h-[44px] px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
          >
            Trade
          </button>
        </div>
      </div>

      {/* Colonize */}
      {resources && (
        <div className="rounded-lg border bg-card p-4">
          <h3 className="font-semibold mb-3">Colonize</h3>
          {colonizeError && <div className="text-destructive text-sm mb-2">{colonizeError}</div>}
          <div className="text-sm text-muted-foreground mb-2">
            Cost: {resources.colonizationCostPerLand} minerals per land
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <input
              type="number"
              min={1}
              max={24}
              value={colonizeAmount}
              onChange={(e) => setColonizeAmount(Number(e.target.value))}
              className="w-24 rounded border bg-input px-2 py-1 text-sm"
            />
            <span className="text-sm text-muted-foreground">
              = {colonizeAmount * resources.colonizationCostPerLand} minerals
            </span>
            <button
              onClick={() => colonizeMutation.mutate()}
              disabled={colonizeMutation.isPending}
              className="rounded bg-green-600 min-h-[44px] px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
            >
              Colonize
            </button>
          </div>
        </div>
      )}

      {/* Assets */}
      {lastError && <div className="text-destructive text-sm">{lastError}</div>}
      {assetsLoading ? (
        <PageLoader message="Loading assets..." />
      ) : assetsError && !assetsData ? (
        <ApiError message="Failed to load assets." onRetry={() => void refetchAssets()} />
      ) : assetsData ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {assetsData.assets.map((asset) => (
            <AssetCard
              key={asset.definition.id}
              asset={asset}
              gameId={gameId}
              onBuild={(defId) => buildMutation.mutate(defId)}
              onQueue={(defId) => queueAssetMutation.mutate(defId)}
            />
          ))}
        </div>
      ) : null}

      <BuildQueue gameId={gameId} />

      {/* Recent Activity */}
      <div className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <strong className="text-sm">Recent Activity</strong>
          {(notifications?.length ?? 0) > 0 && (
            <button
              onClick={() => clearNotificationsMutation.mutate()}
              className="text-xs text-muted-foreground hover:text-foreground"
            >
              Clear
            </button>
          )}
        </div>
        <div className="p-2">
          {!notifications ? (
            <div className="p-3 text-muted-foreground text-sm">Loading...</div>
          ) : notifications.length === 0 ? (
            <div className="p-3 text-muted-foreground text-sm">No recent activity.</div>
          ) : (
            <ul className="divide-y">
              {notifications.slice(0, 10).map((n) => (
                <li key={n.id} className="flex items-start gap-2 py-2 px-2">
                  <span>{notificationIcon(n.kind)}</span>
                  <div className="flex-1 min-w-0">
                    <span className="text-sm">{n.message}</span>
                    <div className="text-xs text-muted-foreground">{relativeTime(n.createdAt)}</div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  )
}
