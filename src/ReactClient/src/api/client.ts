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
      const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
      window.location.href = `/signin?returnUrl=${returnUrl}`
    }
    return Promise.reject(error)
  }
)

export default apiClient
