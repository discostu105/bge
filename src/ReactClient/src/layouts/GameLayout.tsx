import { Suspense } from 'react'
import { Outlet, NavLink, useParams, Navigate } from 'react-router'
import { GamepadIcon, LogOutIcon } from 'lucide-react'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { PageLoader } from '@/components/PageLoader'
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

  if (!gameId) return <Navigate to="/games" />

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* ── Sidebar ─────────────────────────────────────────── */}
      <aside className="w-52 shrink-0 flex flex-col border-r bg-card overflow-y-auto">
        <div className="px-4 py-3 border-b">
          <div className="flex items-center gap-2">
            <GamepadIcon className="h-5 w-5 text-primary" />
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
            <LogOutIcon className="h-3.5 w-3.5" />
            Sign out
          </a>
        </div>
      </aside>

      {/* ── Main area ────────────────────────────────────────── */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Top bar */}
        <header className="h-12 shrink-0 border-b flex items-center justify-between px-4 bg-card">
          <div />
          <div className="flex items-center gap-3">
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
        <main className="flex-1 overflow-y-auto p-4">
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
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
