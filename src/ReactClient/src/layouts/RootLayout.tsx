import { Suspense } from 'react'
import { Outlet } from 'react-router'
import { CurrentUserProvider } from '@/contexts/CurrentUserContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { PageLoader } from '@/components/PageLoader'

export function RootLayout() {
  return (
    <ErrorBoundary>
      <CurrentUserProvider>
        <Suspense fallback={<PageLoader />}>
          <Outlet />
        </Suspense>
      </CurrentUserProvider>
    </ErrorBoundary>
  )
}
