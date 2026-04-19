import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel } from '@/api/types'
import { normalizeRace, type Race } from '@/lib/race'

export function usePlayerRace(): Race | null {
  const { data } = useQuery<PlayerProfileViewModel>({
    queryKey: ['playerprofile'],
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
