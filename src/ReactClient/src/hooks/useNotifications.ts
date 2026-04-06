import { useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerNotificationViewModel } from '@/api/types'
import type { UseSignalRReturn } from './useSignalR'

interface UseNotificationsOptions {
  signalR?: UseSignalRReturn
  onRealTimeNotification?: (notification: { type: string; title: string; body?: string; createdAt: string }) => void
}

export function useNotifications(options?: UseNotificationsOptions) {
  const queryClient = useQueryClient()

  const { data: notifications = [] } = useQuery({
    queryKey: ['notifications'],
    queryFn: () =>
      apiClient
        .get<PlayerNotificationViewModel[]>('/api/notifications/recent')
        .then((r) => r.data),
    // Poll less frequently if SignalR is connected
    refetchInterval: options?.signalR?.status === 'connected' ? 120_000 : 30_000,
  })

  // Listen for real-time notifications via SignalR
  const handler = useCallback(
    (...args: unknown[]) => {
      const payload = args[0] as { type?: string; title?: string; body?: string; createdAt?: string }
      if (!payload?.title) return

      // Refresh the query to pick up the new notification
      queryClient.invalidateQueries({ queryKey: ['notifications'] })

      // Fire toast callback
      options?.onRealTimeNotification?.({
        type: payload.type ?? 'Info',
        title: payload.title,
        body: payload.body,
        createdAt: payload.createdAt ?? new Date().toISOString(),
      })
    },
    [queryClient, options?.onRealTimeNotification]
  )

  const milestoneHandler = useCallback(
    (...args: unknown[]) => {
      const payload = args[0] as { milestoneId?: string; name?: string; icon?: string }
      if (!payload?.name) return

      options?.onRealTimeNotification?.({
        type: 'MilestoneUnlocked',
        title: `${payload.icon ?? '🏆'} Achievement Unlocked: ${payload.name}`,
        createdAt: new Date().toISOString(),
      })
    },
    [options?.onRealTimeNotification]
  )

  useEffect(() => {
    if (!options?.signalR) return
    options.signalR.on('ReceiveNotification', handler)
    options.signalR.on('ReceiveAlert', handler)
    options.signalR.on('MilestoneUnlocked', milestoneHandler)
    return () => {
      options.signalR?.off('ReceiveNotification', handler)
      options.signalR?.off('ReceiveAlert', handler)
      options.signalR?.off('MilestoneUnlocked', milestoneHandler)
    }
  }, [options?.signalR, handler, milestoneHandler])

  const dismissAll = useMutation({
    mutationFn: () => apiClient.delete('/api/notifications/recent'),
    onSuccess: () => queryClient.setQueryData(['notifications'], []),
  })

  return { notifications, dismissAll: dismissAll.mutate }
}
