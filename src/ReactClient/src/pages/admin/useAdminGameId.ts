import { useEffect } from 'react'
import { useParams } from 'react-router'

const STORAGE_KEY = 'bge-admin-game-id'

/**
 * Returns the currently selected admin game id.
 *
 * Prefers the route param when one is present (so the URL is the source of
 * truth for the active page), and falls back to localStorage so the
 * selection persists when the user navigates between admin pages without a
 * gameId in the URL (e.g. from per-game Players to global Audit Log and
 * back).
 *
 * Whenever a route gameId is observed, it is mirrored to localStorage so the
 * "remembered" selection follows the user.
 */
export function useAdminGameId(): string | null {
  const { gameId } = useParams<{ gameId: string }>()
  const urlGameId = gameId ?? null

  useEffect(() => {
    if (urlGameId) {
      try {
        localStorage.setItem(STORAGE_KEY, urlGameId)
      } catch {
        // ignore storage errors (private mode etc.)
      }
    }
  }, [urlGameId])

  if (urlGameId) return urlGameId
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

export function rememberAdminGameId(gameId: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, gameId)
  } catch {
    // ignore
  }
}

export function readRememberedAdminGameId(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}
