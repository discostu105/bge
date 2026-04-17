import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerNotificationViewModel } from '@/api/types'

export interface NavBadges {
  messages: number
  alliances: number
  diplomacy: number
}

/** Pure bucketing — exported for tests. */
export function bucketBadges(
  items: Pick<PlayerNotificationViewModel, 'kind' | 'isRead'>[]
): NavBadges {
  const out: NavBadges = { messages: 0, alliances: 0, diplomacy: 0 }
  for (const n of items) {
    if (n.isRead) continue
    if (n.kind === 'MessageReceived') out.messages += 1
    else if (n.kind === 'AllianceRequest') out.alliances += 1
    else if (n.kind === 'AttackReceived') out.diplomacy += 1
  }
  return out
}

export function useNavBadges(): NavBadges {
  // Reuse the same key as useNotifications so they share a cache entry.
  const { data } = useQuery({
    queryKey: ['notifications'],
    queryFn: () =>
      apiClient
        .get<PlayerNotificationViewModel[]>('/api/notifications/recent')
        .then((r) => r.data),
    refetchInterval: 30_000,
  })
  return bucketBadges(data ?? [])
}
