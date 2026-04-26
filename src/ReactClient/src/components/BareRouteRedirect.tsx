import { Navigate, useParams } from 'react-router'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

/**
 * Redirects bare routes (e.g. /base) to /games/:currentGameId/base.
 * Substitutes :paramName tokens in path with values from the current route.
 * Falls back to /games if no current game is set.
 */
export function BareRouteRedirect({ path }: { path: string }) {
  const { user, isLoading } = useCurrentUser()
  const params = useParams()

  if (isLoading) return null

  const gameId = user?.currentGameId
  if (gameId) {
    const resolved = path.replace(/:([A-Za-z0-9_]+)/g, (_, key) => params[key] ?? '')
    return <Navigate to={`/games/${gameId}/${resolved}`} replace />
  }

  return <Navigate to="/games" replace />
}
