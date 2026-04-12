import { Suspense } from 'react'
import { Outlet } from 'react-router'
import { CurrentUserProvider } from '@/contexts/CurrentUserContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ConfirmProvider } from '@/contexts/ConfirmContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { PageLoader } from '@/components/PageLoader'

export function RootLayout() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
        <CurrentUserProvider>
          <ConfirmProvider>
            <Suspense fallback={<PageLoader />}>
              <Outlet />
            </Suspense>
          </ConfirmProvider>
        </CurrentUserProvider>
      </ThemeProvider>
    </ErrorBoundary>
  )
}
