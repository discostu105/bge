import { Outlet } from 'react-router'
import { CurrentUserProvider } from '@/contexts/CurrentUserContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'

export function RootLayout() {
  return (
    <ErrorBoundary>
      <CurrentUserProvider>
        <Outlet />
      </CurrentUserProvider>
    </ErrorBoundary>
  )
}
