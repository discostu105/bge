import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AxiosError } from 'axios'
import apiClient from '@/api/client'
import type { ShopViewModel, PurchaseItemRequest } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

export function Shop() {
  const { user } = useCurrentUser()
  const queryClient = useQueryClient()
  const [purchaseError, setPurchaseError] = useState<string | null>(null)
  const [purchaseSuccess, setPurchaseSuccess] = useState<string | null>(null)

  const { data, error, isLoading, refetch } = useQuery<ShopViewModel>({
    queryKey: ['shop'],
    queryFn: () => apiClient.get('/api/shop').then((r) => r.data),
  })

  const purchaseMutation = useMutation({
    mutationFn: (req: PurchaseItemRequest) => apiClient.post('/api/shop/purchase', req),
    onSuccess: (_, req) => {
      setPurchaseError(null)
      setPurchaseSuccess(`Item purchased!`)
      void queryClient.invalidateQueries({ queryKey: ['shop'] })
      void queryClient.invalidateQueries({ queryKey: ['economy-balance'] })
      void queryClient.invalidateQueries({ queryKey: ['economy-items'] })
      setTimeout(() => setPurchaseSuccess(null), 3000)
    },
    onError: (err: AxiosError) => {
      if (err.response?.status === 402) setPurchaseError('Insufficient funds.')
      else if (err.response?.status === 409) setPurchaseError('You already own this item.')
      else setPurchaseError('Purchase failed. Please try again.')
    },
  })

  if (isLoading) return <PageLoader message="Loading shop..." />
  if (error) return <ApiError message="Failed to load shop." onRetry={() => void refetch()} />

  const items = data?.items ?? []

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Item Shop</h1>
        <p className="text-muted-foreground text-sm mt-1">Spend your coins on cosmetic items</p>
      </div>

      {purchaseError && (
        <div className="p-3 bg-destructive/10 text-destructive rounded text-sm">{purchaseError}</div>
      )}
      {purchaseSuccess && (
        <div className="p-3 bg-green-500/10 text-green-700 rounded text-sm">{purchaseSuccess}</div>
      )}

      {items.length === 0 ? (
        <p className="text-muted-foreground">No items available.</p>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div key={item.itemId} className="border rounded-lg p-4 space-y-3">
              <div>
                <div className="font-semibold">{item.name}</div>
                <div className="text-sm text-muted-foreground">{item.description}</div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">💰 {item.price}</span>
                {item.isOwned ? (
                  <span className="text-xs px-2 py-1 bg-muted rounded text-muted-foreground">Owned</span>
                ) : user ? (
                  <button
                    className="text-sm px-3 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90 disabled:opacity-50"
                    disabled={purchaseMutation.isPending}
                    onClick={() => {
                      setPurchaseError(null)
                      purchaseMutation.mutate({ itemId: item.itemId, idempotencyKey: crypto.randomUUID() })
                    }}
                  >
                    Buy
                  </button>
                ) : (
                  <span className="text-xs text-muted-foreground">Sign in to buy</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
