import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { CoinsIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { CurrencyBalanceViewModel } from '@/api/types'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

export function CurrencyBadge() {
  const { user } = useCurrentUser()

  const { data } = useQuery<CurrencyBalanceViewModel>({
    queryKey: ['economy-balance'],
    queryFn: () => apiClient.get('/api/economy/balance').then((r) => r.data),
    enabled: !!user,
    refetchInterval: 30000,
  })

  if (!user || data === undefined) return null

  return (
    <Link to="/economy" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors">
      <CoinsIcon className="h-4 w-4 text-warning" aria-hidden="true" />
      <span>{data.balance}</span>
    </Link>
  )
}
