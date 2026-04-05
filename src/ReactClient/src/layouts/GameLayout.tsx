import { Suspense, useState, useCallback, useEffect } from 'react'
import { Outlet, NavLink, useParams, Navigate, useLocation } from 'react-router'
import { GamepadIcon, LogOutIcon, MenuIcon, XIcon, WifiIcon, WifiOffIcon } from 'lucide-react'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { NotificationToasts, useNotificationToasts } from '@/components/NotificationToast'
import { TutorialOverlay } from '@/components/TutorialOverlay'
import { PageLoader } from '@/components/PageLoader'
import { useSignalR } from '@/hooks/useSignalR'
import { cn } from '@/lib/utils'
import { vcText, vcBadgeCss } from '@/lib/victory'
import type { GameDetailViewModel } from '@/api/types'

function GameEndedBanner({ game }: { game: GameDetailViewModel }) {
  if (game.status !== 'Finished') return null
  return (
    <div role="alert" className="bg-warning/20 border-b border-warning/50 px-4 py-2 text-warning-foreground text-sm flex items-center gap-2">
      <span aria-hidden="true">🏆</span>
      <span>
        {game.winnerName ? (
          <><strong>{game.winnerName} wins!</strong></>
        ) : (
          <strong>Game Over!</strong>
        )}
        {game.victoryConditionType && (
          <span className={`ml-2 text-xs font-bold px-2 py-0.5 rounded ${vcBadgeCss(game.victoryConditionType)}`}>
            {vcText(game.victoryConditionType)}
          </span>
        )}
        {' — '}
        <NavLink to="results" className="underline hover:opacity-80">View final results</NavLink>
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
        ? 'bg-danger/20 border-danger/50 text-danger-foreground'
        : 'bg-warning/15 border-warning/40 text-warning-foreground'
    )}>
      <span aria-hidden="true">⏰</span>
      <span>Game ends in <strong>{display}</strong></span>
    </div>
  )
}

function SidebarContent({ currentGame }: { currentGame: { name: string } | null }) {
  return (
    <>
      <div className="px-4 py-3 border-b">
        <div className="flex items-center gap-2">
          <GamepadIcon className="h-5 w-5 text-primary" aria-hidden="true" />
          <span className="font-semibold text-sm truncate">
            {currentGame?.name ?? 'Game'}
          </span>
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
          className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground"
        >
          <LogOutIcon className="h-3.5 w-3.5" aria-hidden="true" />
          Sign out
        </a>
      </div>
    </>
  )
}

function GameLayoutInner() {
  const { gameId, currentGame } = useCurrentGame()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const location = useLocation()
  const signalR = useSignalR('/gamehub')
  const { toasts, addToast, dismiss } = useNotificationToasts()

  const onRealTimeNotification = useCallback(
    (n: { type: string; title: string; body?: string; createdAt: string }) => {
      addToast(n)
    },
    [addToast]
  )

  // Close mobile sidebar on navigation
  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

  if (!gameId) return <Navigate to="/games" />

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* ── Desktop Sidebar ─────────────────────────────────── */}
      <aside className="hidden md:flex w-52 shrink-0 flex-col border-r bg-card overflow-y-auto" aria-label="Game sidebar">
        <SidebarContent currentGame={currentGame} />
      </aside>

      {/* ── Mobile Sidebar Overlay ──────────────────────────── */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 md:hidden"
          onClick={() => setSidebarOpen(false)}
          aria-hidden="true"
        >
          <div className="absolute inset-0 bg-black/60" />
        </div>
      )}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 w-52 flex flex-col border-r bg-card overflow-y-auto transition-transform duration-200 md:hidden',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
        aria-label="Game sidebar"
      >
        <div className="flex items-center justify-end p-2 border-b">
          <button
            onClick={() => setSidebarOpen(false)}
            className="p-1 rounded hover:bg-secondary"
            aria-label="Close menu"
          >
            <XIcon className="h-5 w-5" />
          </button>
        </div>
        <SidebarContent currentGame={currentGame} />
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
            <span
              className="hidden sm:inline-flex"
              title={`Real-time: ${signalR.status}`}
            >
              {signalR.status === 'connected' ? (
                <WifiIcon className="h-3.5 w-3.5 text-green-500" />
              ) : (
                <WifiOffIcon className="h-3.5 w-3.5 text-muted-foreground" />
              )}
            </span>
            <NotificationBell signalR={signalR} onRealTimeNotification={onRealTimeNotification} />
          </div>
        </header>

        {/* Banners */}
        {currentGame && (
          <>
            <GameEndedBanner game={currentGame} />
            <TimeRemainingBanner endTime={currentGame.endTime} />
          </>
        )}

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-3 sm:p-4">
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
        </main>
      </div>

      {/* Real-time notification toasts */}
      <NotificationToasts toasts={toasts} dismiss={dismiss} gameId={gameId} />

      {/* First-time player tutorial */}
      <TutorialOverlay />
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
