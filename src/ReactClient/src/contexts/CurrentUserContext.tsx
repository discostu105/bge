import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import apiClient from '@/api/client'
import type { ProfileViewModel } from '@/api/types'

interface CurrentUserContextValue {
  user: ProfileViewModel | null
  isLoading: boolean
}

const CurrentUserContext = createContext<CurrentUserContextValue>({
  user: null,
  isLoading: true,
})

export function CurrentUserProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<ProfileViewModel | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    apiClient
      .get<ProfileViewModel>('/api/profile')
      .then((res) => setUser(res.data))
      .catch(() => setUser(null))
      .finally(() => setIsLoading(false))
  }, [])

  return (
    <CurrentUserContext.Provider value={{ user, isLoading }}>
      {children}
    </CurrentUserContext.Provider>
  )
}

export function useCurrentUser() {
  return useContext(CurrentUserContext)
}
