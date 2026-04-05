import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { ProfileViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

export function PlayerProfile() {
  const queryClient = useQueryClient()

  const { data: profile, isLoading } = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.get<ProfileViewModel>('/api/profile').then((r) => r.data),
  })

  if (isLoading) return <p className="text-muted-foreground text-sm">Loading…</p>
  if (!profile) return <p className="text-destructive text-sm">Failed to load profile.</p>

  return (
    <div className="max-w-lg space-y-4">
      <h1 className="text-xl font-bold">Profile</h1>

      <div className="rounded-lg border bg-card p-6 space-y-4">
        {/* Avatar + name */}
        <div className="flex items-center gap-4">
          <div className="h-16 w-16 rounded-full bg-secondary flex items-center justify-center text-2xl border-2 border-primary/50">
            {profile.userName?.[0]?.toUpperCase() ?? '?'}
          </div>
          <div>
            <div className="font-bold text-lg">{profile.userName}</div>
            {profile.isAdmin && (
              <span className="text-xs rounded bg-yellow-900/60 text-yellow-300 px-1.5 py-0.5">Admin</span>
            )}
          </div>
        </div>

        {/* Current game info */}
        {profile.currentGameId ? (
          <div className="rounded border bg-secondary/20 px-4 py-3 text-sm space-y-1">
            <div className="text-xs text-muted-foreground uppercase tracking-wide">Active game</div>
            <div className="font-medium">
              Playing as <strong>{profile.currentPlayerName}</strong>
            </div>
            <Link
              to={`/games/${profile.currentGameId}/base`}
              className="text-primary text-xs hover:underline"
            >
              Go to game →
            </Link>
          </div>
        ) : (
          <div className="rounded border border-dashed px-4 py-3 text-sm text-muted-foreground">
            Not currently in a game.{' '}
            <Link to="/lobby" className="text-primary hover:underline">Browse games</Link>
          </div>
        )}
      </div>
    </div>
  )
}
