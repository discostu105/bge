import { Outlet } from 'react-router'
import { CurrentUserProvider } from '@/contexts/CurrentUserContext'

export function RootLayout() {
  return (
    <CurrentUserProvider>
      <Outlet />
    </CurrentUserProvider>
  )
}
