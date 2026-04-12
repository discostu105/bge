import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type { MarketViewModel, MarketOrderViewModel, CreateMarketOrderRequest } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useConfirm } from '@/contexts/ConfirmContext'

interface MarketProps {
  gameId: string
}

function exchangeRate(order: MarketOrderViewModel): string {
  if (order.wantedAmount === 0) return '—'
  const ratio = order.offeredAmount / order.wantedAmount
  return `${ratio.toFixed(4)} ${order.offeredResourceName}/${order.wantedResourceName}`
}

export function Market({ gameId }: MarketProps) {
  const queryClient = useQueryClient()
  const confirm = useConfirm()
  const [postError, setPostError] = useState<string | null>(null)
  const [orderErrors, setOrderErrors] = useState<Record<string, string>>({})
  const [offerResourceId, setOfferResourceId] = useState('')
  const [offerAmount, setOfferAmount] = useState(100)
  const [wantResourceId, setWantResourceId] = useState('')
  const [wantAmount, setWantAmount] = useState(100)

  const { data, isLoading, error: queryError, refetch } = useQuery<MarketViewModel>({
    queryKey: ['market', gameId],
    queryFn: () => apiClient.get('/api/market').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const postOfferMutation = useMutation({
    mutationFn: () => {
      const request: CreateMarketOrderRequest = {
        offeredResourceId: offerResourceId,
        offeredAmount: offerAmount,
        wantedResourceId: wantResourceId,
        wantedAmount: wantAmount,
      }
      return apiClient.post('/api/market/post', request)
    },
    onSuccess: () => {
      setPostError(null)
      setOfferResourceId('')
      setOfferAmount(100)
      setWantResourceId('')
      setWantAmount(100)
      void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setPostError((err.response?.data as string) ?? 'Post failed')
    },
  })

  const acceptMutation = useMutation({
    mutationFn: (orderId: string) =>
      apiClient.post(`/api/market/accept?orderId=${orderId}`, ''),
    onSuccess: (_, orderId) => {
      setOrderErrors((prev) => {
        const next = { ...prev }
        delete next[orderId]
        return next
      })
      void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
    },
    onError: (err, orderId) => {
      if (axios.isAxiosError(err)) {
        setOrderErrors((prev) => ({
          ...prev,
          [orderId]: (err.response?.data as string) ?? 'Accept failed',
        }))
      }
    },
  })

  const cancelMutation = useMutation({
    mutationFn: (orderId: string) =>
      apiClient.delete(`/api/market/cancel?orderId=${orderId}`),
    onSuccess: (_, orderId) => {
      setOrderErrors((prev) => {
        const next = { ...prev }
        delete next[orderId]
        return next
      })
      void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
    },
    onError: (err, orderId) => {
      if (axios.isAxiosError(err)) {
        setOrderErrors((prev) => ({
          ...prev,
          [orderId]: (err.response?.data as string) ?? 'Cancel failed',
        }))
      }
    },
  })

  if (isLoading) return <PageLoader message="Loading market..." />
  if (queryError) return <ApiError message="Failed to load market." onRetry={() => void refetch()} />
  if (!data) return null

  const myOrders = data.openOrders.filter((o) => o.sellerPlayerId === data.currentPlayerId)
  const otherOrders = data.openOrders.filter((o) => o.sellerPlayerId !== data.currentPlayerId)

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Market</h1>

      {/* Post Offer */}
      <div className="rounded-lg border bg-card p-4 sm:p-5 max-w-lg">
        <h2 className="font-semibold mb-4">Post Trade Offer</h2>
        {postError && (
          <div className="text-destructive text-sm mb-3">{postError}</div>
        )}
        <div className="space-y-3">
          <div>
            <label className="block text-sm text-muted-foreground mb-1">Offer resource</label>
            <select
              value={offerResourceId}
              onChange={(e) => setOfferResourceId(e.target.value)}
              className="w-full rounded border bg-input px-2 py-1.5 text-sm"
            >
              <option value="">-- select resource --</option>
              {data.resourceOptions.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm text-muted-foreground mb-1">Amount to offer</label>
            <input
              type="number"
              min={1}
              value={offerAmount}
              onChange={(e) => setOfferAmount(Number(e.target.value))}
              className="w-full rounded border bg-input px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="block text-sm text-muted-foreground mb-1">Want resource</label>
            <select
              value={wantResourceId}
              onChange={(e) => setWantResourceId(e.target.value)}
              className="w-full rounded border bg-input px-2 py-1.5 text-sm"
            >
              <option value="">-- select resource --</option>
              {data.resourceOptions.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm text-muted-foreground mb-1">Amount wanted</label>
            <input
              type="number"
              min={1}
              value={wantAmount}
              onChange={(e) => setWantAmount(Number(e.target.value))}
              className="w-full rounded border bg-input px-2 py-1.5 text-sm"
            />
          </div>
          <button
            onClick={() => postOfferMutation.mutate()}
            disabled={postOfferMutation.isPending}
            className="rounded bg-primary min-h-[44px] px-4 py-2 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            Post Offer
          </button>
        </div>
      </div>

      {/* My Orders */}
      {myOrders.length > 0 && (
        <div>
          <h2 className="font-semibold mb-3">My Orders</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Offering</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Wants</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Rate</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Posted</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
                </tr>
              </thead>
              <tbody>
                {myOrders.map((order) => (
                  <tr key={order.orderId} className="border-b border-border bg-info/10">
                    <td className="py-2 px-3">
                      {order.offeredAmount} {order.offeredResourceName}
                    </td>
                    <td className="py-2 px-3">
                      {order.wantedAmount} {order.wantedResourceName}
                    </td>
                    <td className="py-2 px-3 text-muted-foreground text-xs">
                      {exchangeRate(order)}
                    </td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {new Date(order.createdAt).toLocaleString()}
                    </td>
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={async () => {
                            const ok = await confirm({
                              title: 'Cancel market order?',
                              description: `Your ${order.offeredAmount} ${order.offeredResourceName} will be returned to your base.`,
                              destructive: true,
                              confirmLabel: 'Cancel order',
                              cancelLabel: 'Keep order',
                            })
                            if (ok) cancelMutation.mutate(order.orderId)
                          }}
                          disabled={cancelMutation.isPending}
                          className="rounded bg-destructive min-h-[36px] px-3 py-1.5 text-xs text-destructive-foreground hover:opacity-90 disabled:opacity-50"
                        >
                          Cancel
                        </button>
                        {orderErrors[order.orderId] && (
                          <span className="text-destructive text-xs">{orderErrors[order.orderId]}</span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Open Orders from others */}
      <div>
        <h2 className="font-semibold mb-3">Open Orders</h2>
        {otherOrders.length === 0 ? (
          <p className="text-muted-foreground text-sm">No open orders from other players.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Seller</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Offering</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Wants</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Rate</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Posted</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
                </tr>
              </thead>
              <tbody>
                {otherOrders.map((order) => (
                  <tr key={order.orderId} className="border-b border-border">
                    <td className="py-2 px-3">{order.sellerPlayerName}</td>
                    <td className="py-2 px-3">
                      {order.offeredAmount} {order.offeredResourceName}
                    </td>
                    <td className="py-2 px-3">
                      {order.wantedAmount} {order.wantedResourceName}
                    </td>
                    <td className="py-2 px-3 text-muted-foreground text-xs">
                      {exchangeRate(order)}
                    </td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {new Date(order.createdAt).toLocaleString()}
                    </td>
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => acceptMutation.mutate(order.orderId)}
                          disabled={acceptMutation.isPending}
                          className="rounded bg-success min-h-[36px] px-3 py-1.5 text-xs text-white hover:opacity-90 disabled:opacity-50"
                        >
                          Accept
                        </button>
                        {orderErrors[order.orderId] && (
                          <span className="text-destructive text-xs">{orderErrors[order.orderId]}</span>
                        )}
                      </div>
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
