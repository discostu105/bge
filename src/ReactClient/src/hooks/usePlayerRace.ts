import { useEffect } from 'react'
import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel } from '@/api/types'
import { normalizeRace, type Race } from '@/lib/race'

export function usePlayerRace(): Race | null {
  // Scope the cache by gameId — /api/playerprofile is server-scoped per game
  // via the X-Game-Id header, so a user playing different races in different
  // games must not get a stale cached profile when navigating between them.
  const { gameId } = useParams<{ gameId: string }>()
  const { data } = useQuery<PlayerProfileViewModel>({
    queryKey: ['playerprofile', gameId ?? null],
    queryFn: () => apiClient.get('/api/playerprofile').then((r) => r.data),
    staleTime: 60_000,
  })

  const race = normalizeRace(data?.playerType ?? null)

  useEffect(() => {
    const root = document.documentElement
    if (race) {
      root.setAttribute('data-race', race)
    } else {
      root.removeAttribute('data-race')
    }
    return () => {
      root.removeAttribute('data-race')
    }
  }, [race])

  return race
}
