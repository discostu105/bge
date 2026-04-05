import { useEffect } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { ProfileViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function Index() {
  const navigate = useNavigate()

  const { data: profile, isLoading, error, refetch } = useQuery<ProfileViewModel>({
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

  if (isLoading) return <PageLoader />
  if (error) return <ApiError message="Failed to load profile." onRetry={() => void refetch()} />

  return null
}
