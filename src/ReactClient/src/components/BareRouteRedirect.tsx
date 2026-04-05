import { Navigate } from 'react-router'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

/**
 * Redirects bare routes (e.g. /base) to /games/:currentGameId/base.
 * Falls back to /games if no current game is set.
 */
export function BareRouteRedirect({ path }: { path: string }) {
  const { user, isLoading } = useCurrentUser()

  if (isLoading) return null

  const gameId = user?.currentGameId
  if (gameId) {
    return <Navigate to={`/games/${gameId}/${path}`} replace />
  }

  return <Navigate to="/games" replace />
}
