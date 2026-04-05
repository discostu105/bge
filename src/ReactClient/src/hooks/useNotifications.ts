import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerNotificationViewModel } from '@/api/types'

export function useNotifications() {
  const queryClient = useQueryClient()

  const { data: notifications = [] } = useQuery({
    queryKey: ['notifications'],
    queryFn: () =>
      apiClient
        .get<PlayerNotificationViewModel[]>('/api/notifications/recent')
        .then((r) => r.data),
    refetchInterval: 30_000,
  })

  const dismissAll = useMutation({
    mutationFn: () => apiClient.delete('/api/notifications/recent'),
    onSuccess: () => queryClient.setQueryData(['notifications'], []),
  })

  return { notifications, dismissAll: dismissAll.mutate }
}
