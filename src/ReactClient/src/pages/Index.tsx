import { useEffect } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { ProfileViewModel } from '@/api/types'

export function Index() {
  const navigate = useNavigate()

  const { data: profile, isLoading } = useQuery<ProfileViewModel>({
    queryKey: ['profile'],
    queryFn: () => apiClient.get('/api/profile').then((r) => r.data),
  })

  useEffect(() => {
    if (!profile) return
    if (profile.currentGameId) {
      void navigate(`/games/${profile.currentGameId}/base`, { replace: true })
    } else {
      void navigate('/games', { replace: true })
    }
  }, [profile, navigate])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="text-muted-foreground text-sm">Loading...</div>
      </div>
    )
  }

  return null
}
