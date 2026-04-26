import axios from 'axios'
import { gameIdFromPath } from '@/lib/gameIdFromPath'

const apiClient = axios.create({
  headers: {
    'X-Requested-With': 'XMLHttpRequest',
  },
})

apiClient.interceptors.request.use((config) => {
  const gameId = gameIdFromPath(window.location.pathname)
  if (gameId) {
    config.headers.set('X-Game-Id', gameId)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // A 401 on a request scoped to a specific game (X-Game-Id header) usually
      // means "you're authenticated but you have no player in this game" — not
      // "your session is gone." Bouncing the user to /signin in that case
      // produces an infinite signup loop on a freshly-created game's lobby.
      const headers = error.config?.headers
      const hadGameId = headers != null && (
        typeof headers.get === 'function'
          ? headers.get('X-Game-Id') != null
          : Object.keys(headers).some((k) => k.toLowerCase() === 'x-game-id')
      )
      if (!hadGameId) {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.href = `/signin?returnUrl=${returnUrl}`
      }
    }
    return Promise.reject(error)
  }
)

export default apiClient
