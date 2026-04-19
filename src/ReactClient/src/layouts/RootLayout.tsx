import { Suspense } from 'react'
import { Outlet } from 'react-router'
import { CurrentUserProvider } from '@/contexts/CurrentUserContext'
import { ConfirmProvider } from '@/contexts/ConfirmContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { PageLoader } from '@/components/PageLoader'
import { Toaster } from '@/components/ui/sonner'

export function RootLayout() {
  return (
    <ErrorBoundary>
      <CurrentUserProvider>
        <ConfirmProvider>
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
        </ConfirmProvider>
        <Toaster richColors position="top-right" />
      </CurrentUserProvider>
    </ErrorBoundary>
  )
}
