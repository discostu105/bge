import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type {
  TransactionHistoryViewModel,
  OwnedItemViewModel,
  CurrencyTradeOfferViewModel,
} from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

type Tab = 'history' | 'items' | 'trades'

export function Economy() {
  const { user } = useCurrentUser()
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<Tab>('history')
  const [page, setPage] = useState(1)
  const pageSize = 20

  const historyQuery = useQuery<TransactionHistoryViewModel>({
    queryKey: ['economy-history', page],
    queryFn: () => apiClient.get(`/api/economy/transactions?page=${page}&pageSize=${pageSize}`).then((r) => r.data),
    enabled: !!user && tab === 'history',
  })

  const itemsQuery = useQuery<OwnedItemViewModel[]>({
    queryKey: ['economy-items'],
    queryFn: () => apiClient.get('/api/economy/items').then((r) => r.data),
    enabled: !!user && tab === 'items',
  })

  const tradesQuery = useQuery<CurrencyTradeOfferViewModel[]>({
    queryKey: ['economy-trades'],
    queryFn: () => apiClient.get('/api/economy/trade-offers').then((r) => r.data),
    enabled: !!user && tab === 'trades',
  })

  const acceptMutation = useMutation({
    mutationFn: (offerId: string) => apiClient.post(`/api/economy/trade-offers/${offerId}/accept`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['economy-trades'] })
      void queryClient.invalidateQueries({ queryKey: ['economy-balance'] })
    },
  })

  const declineMutation = useMutation({
    mutationFn: (offerId: string) => apiClient.post(`/api/economy/trade-offers/${offerId}/decline`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['economy-trades'] }),
  })

  if (!user) {
    return (
      <div className="text-center py-16 text-muted-foreground">
        Sign in to view your economy.
      </div>
    )
  }

  const tabs: { id: Tab; label: string }[] = [
    { id: 'history', label: 'Transaction History' },
    { id: 'items', label: 'Owned Items' },
    { id: 'trades', label: 'Trade Offers' },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Economy</h1>
        <p className="text-muted-foreground text-sm mt-1">Your currency, items, and trades</p>
      </div>

      <div className="flex gap-2 border-b">
        {tabs.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              tab === t.id
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'history' && (
        <div className="space-y-4">
          {historyQuery.isLoading && <PageLoader message="Loading transactions..." />}
          {historyQuery.error && (
            <ApiError message="Failed to load transactions." onRetry={() => void historyQuery.refetch()} />
          )}
          {historyQuery.data && (
            <>
              <div className="text-sm text-muted-foreground">
                Balance: <span className="font-semibold text-foreground">💰 {historyQuery.data.balance}</span>
                {' · '}{historyQuery.data.totalCount} transactions
              </div>
              {historyQuery.data.transactions.length === 0 ? (
                <p className="text-muted-foreground">No transactions yet.</p>
              ) : (
                <div className="divide-y rounded border">
                  {historyQuery.data.transactions.map((tx) => (
                    <div key={tx.transactionId} className="flex items-center justify-between px-4 py-3 text-sm">
                      <div>
                        <div className="font-medium">{tx.description}</div>
                        <div className="text-xs text-muted-foreground">
                          {tx.type} · {new Date(tx.createdAt).toLocaleString()}
                        </div>
                      </div>
                      <div className={`font-semibold ${tx.amount >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                        {tx.amount >= 0 ? '+' : ''}{tx.amount}
                      </div>
                    </div>
                  ))}
                </div>
              )}
              <div className="flex gap-2 justify-center text-sm">
                <button
                  className="px-3 py-1 border rounded disabled:opacity-40"
                  disabled={page === 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </button>
                <span className="px-3 py-1 text-muted-foreground">Page {page}</span>
                <button
                  className="px-3 py-1 border rounded disabled:opacity-40"
                  disabled={historyQuery.data.transactions.length < pageSize}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {tab === 'items' && (
        <div className="space-y-4">
          {itemsQuery.isLoading && <PageLoader message="Loading items..." />}
          {itemsQuery.error && (
            <ApiError message="Failed to load items." onRetry={() => void itemsQuery.refetch()} />
          )}
          {itemsQuery.data && (
            itemsQuery.data.length === 0 ? (
              <p className="text-muted-foreground">No items owned. Visit the <a href="/shop" className="underline">shop</a> to buy some!</p>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {itemsQuery.data.map((item) => (
                  <div key={item.ownershipId} className="border rounded-lg p-4 space-y-1">
                    <div className="font-semibold">{item.name}</div>
                    <div className="text-sm text-muted-foreground">{item.description}</div>
                    <div className="text-xs text-muted-foreground">
                      Purchased {new Date(item.purchasedAt).toLocaleDateString()}
                    </div>
                  </div>
                ))}
              </div>
            )
          )}
        </div>
      )}

      {tab === 'trades' && (
        <div className="space-y-4">
          {tradesQuery.isLoading && <PageLoader message="Loading trade offers..." />}
          {tradesQuery.error && (
            <ApiError message="Failed to load trade offers." onRetry={() => void tradesQuery.refetch()} />
          )}
          {tradesQuery.data && (
            tradesQuery.data.length === 0 ? (
              <p className="text-muted-foreground">No incoming trade offers.</p>
            ) : (
              <div className="divide-y rounded border">
                {tradesQuery.data.map((offer) => (
                  <div key={offer.offerId} className="flex items-center justify-between px-4 py-3 text-sm">
                    <div>
                      <div className="font-medium">
                        From {offer.fromDisplayName ?? offer.fromUserId}
                      </div>
                      <div className="text-muted-foreground">
                        Offers 💰 {offer.offeredAmount}
                        {offer.wantedItemId && ` for your "${offer.wantedItemName ?? offer.wantedItemId}"`}
                        {offer.wantedCurrencyAmount && ` for 💰 ${offer.wantedCurrencyAmount}`}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Expires {new Date(new Date(offer.createdAt).getTime() + 24 * 3600 * 1000).toLocaleString()}
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <button
                        className="px-3 py-1 bg-primary text-primary-foreground rounded text-xs hover:bg-primary/90 disabled:opacity-50"
                        disabled={acceptMutation.isPending}
                        onClick={() => acceptMutation.mutate(offer.offerId)}
                      >
                        Accept
                      </button>
                      <button
                        className="px-3 py-1 border rounded text-xs hover:bg-muted disabled:opacity-50"
                        disabled={declineMutation.isPending}
                        onClick={() => declineMutation.mutate(offer.offerId)}
                      >
                        Decline
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )
          )}
        </div>
      )}
    </div>
  )
}
