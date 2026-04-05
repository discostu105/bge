import axios from 'axios'

const apiClient = axios.create({
  headers: {
    'X-Requested-With': 'XMLHttpRequest',
  },
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
