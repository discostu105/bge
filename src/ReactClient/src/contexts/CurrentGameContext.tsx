import { createContext, useContext, type ReactNode } from 'react'
import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameDetailViewModel } from '@/api/types'

interface CurrentGameContextValue {
  currentGame: GameDetailViewModel | null
  gameId: string | null
  refetch: () => void
}

const CurrentGameContext = createContext<CurrentGameContextValue>({
  currentGame: null,
  gameId: null,
  refetch: () => {},
})

export function CurrentGameProvider({ children }: { children: ReactNode }) {
  const { gameId } = useParams<{ gameId: string }>()

  const { data: currentGame = null, refetch } = useQuery({
    queryKey: ['game', gameId],
    queryFn: () =>
      apiClient.get<GameDetailViewModel>(`/api/games/${gameId}`).then((r) => r.data),
    enabled: !!gameId,
    refetchInterval: 10_000,
  })

  return (
    <CurrentGameContext.Provider value={{ currentGame, gameId: gameId ?? null, refetch }}>
      {children}
    </CurrentGameContext.Provider>
  )
}

export function useCurrentGame() {
  return useContext(CurrentGameContext)
}
