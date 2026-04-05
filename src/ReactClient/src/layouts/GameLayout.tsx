import { useState } from 'react'
import { Outlet, NavLink, useParams, Navigate, useLocation } from 'react-router'
import { GamepadIcon, LogOutIcon, MenuIcon, XIcon } from 'lucide-react'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { cn } from '@/lib/utils'

function GameEndedBanner({ status }: { status: string }) {
  if (status !== 'Finished') return null
  return (
    <div className="bg-amber-900/60 border-b border-amber-700 px-4 py-2 text-amber-200 text-sm flex items-center gap-2">
      <span>⚠️</span>
      <span>
        <strong>Game Over!</strong> The game has ended.{' '}
        <NavLink to="results" className="underline hover:text-amber-100">View final results</NavLink>
      </span>
    </div>
  )
}

function TimeRemainingBanner({ endTime }: { endTime: string }) {
  if (!endTime) return null
  const remaining = new Date(endTime).getTime() - Date.now()
  if (remaining <= 0) return null

  const hours = remaining / 3_600_000
  if (hours > 24) return null

  const isDanger = hours < 1
  const display = hours < 1
    ? `${Math.ceil(remaining / 60_000)}m`
    : `${Math.floor(hours)}h ${Math.floor((remaining % 3_600_000) / 60_000)}m`

  return (
    <div className={cn(
      'border-b px-4 py-2 text-sm flex items-center gap-2',
      isDanger
        ? 'bg-red-900/60 border-red-700 text-red-200'
        : 'bg-yellow-900/40 border-yellow-700 text-yellow-200'
    )}>
      <span>⏰</span>
      <span>Game ends in <strong>{display}</strong></span>
    </div>
  )
}

function GameLayoutInner() {
  const { gameId, currentGame } = useCurrentGame()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const location = useLocation()

  // Close sidebar on navigation
  const locationKey = location.pathname
  const [lastPath, setLastPath] = useState(locationKey)
  if (locationKey !== lastPath) {
    setLastPath(locationKey)
    if (sidebarOpen) setSidebarOpen(false)
  }

  if (!gameId) return <Navigate to="/games" />

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* ── Mobile sidebar overlay ──────────────────────────── */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/60 md:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* ── Sidebar ─────────────────────────────────────────── */}
      <aside className={cn(
        'fixed inset-y-0 left-0 z-50 w-52 flex flex-col border-r bg-card overflow-y-auto transition-transform duration-200 ease-in-out md:static md:translate-x-0 md:shrink-0',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full'
      )}>
        <div className="px-4 py-3 border-b">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 min-w-0">
              <GamepadIcon className="h-5 w-5 text-primary shrink-0" />
              <span className="font-semibold text-sm truncate">
                {currentGame?.name ?? 'Game'}
              </span>
            </div>
            <button
              onClick={() => setSidebarOpen(false)}
              className="ml-2 p-1 rounded hover:bg-secondary md:hidden"
              aria-label="Close menu"
            >
              <XIcon className="h-5 w-5" />
            </button>
          </div>
          <NavLink to="/games" className="text-xs text-muted-foreground hover:text-foreground mt-1 block">
            ← All Games
          </NavLink>
        </div>

        <div className="flex-1 py-2">
          <NavMenu />
        </div>

        <div className="border-t p-3">
          <a
            href="/signout"
            className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground min-h-[44px] items-center"
          >
            <LogOutIcon className="h-3.5 w-3.5" />
            Sign out
          </a>
        </div>
      </aside>

      {/* ── Main area ────────────────────────────────────────── */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Top bar */}
        <header className="h-12 shrink-0 border-b flex items-center justify-between px-3 sm:px-4 bg-card">
          <button
            onClick={() => setSidebarOpen(true)}
            className="p-2 -ml-1 rounded hover:bg-secondary md:hidden"
            aria-label="Open menu"
          >
            <MenuIcon className="h-5 w-5" />
          </button>
          <div className="hidden md:block" />
          <div className="flex items-center gap-2 sm:gap-3 overflow-x-auto">
            <PlayerResources gameId={gameId} />
            <NotificationBell />
          </div>
        </header>

        {/* Banners */}
        {currentGame && (
          <>
            <GameEndedBanner status={currentGame.status} />
            <TimeRemainingBanner endTime={currentGame.endTime} />
          </>
        )}

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-3 sm:p-4">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export function GameLayout() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return <Navigate to="/games" />

  return (
    <CurrentGameProvider>
      <GameLayoutInner />
    </CurrentGameProvider>
  )
}
