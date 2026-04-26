import { Suspense, useState, useCallback, useEffect, useRef } from 'react'
import { Outlet, useParams, Navigate, useLocation, useNavigate, NavLink } from 'react-router'
import {
  LogOutIcon, MenuIcon, XIcon, UserIcon, HistoryIcon, LineChartIcon, Trophy, ArrowLeftIcon,
} from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { TickInfoViewModel } from '@/api/types'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { useCurrentUser } from '@/contexts/CurrentUserContext'
import { usePlayerRace } from '@/hooks/usePlayerRace'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { NotificationToasts, useNotificationToasts } from '@/components/NotificationToast'
import { TutorialOverlay } from '@/components/TutorialOverlay'
import { PageLoader } from '@/components/PageLoader'
import { GameStatusBanner } from '@/components/GameStatusBanner'
import { TimerRing } from '@/components/ui/timer-ring'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'
import { useSignalR } from '@/hooks/useSignalR'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog'
import {
  DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'
import type { Race } from '@/lib/race'

interface GameFinalizedPayload {
  gameId: string
  winnerId: string | null
  winnerName: string | null
}

function RaceEmblem({ race, size = 24 }: { race: Race | null; size?: number }) {
  const style = { color: 'var(--color-race-primary)' }
  if (race === 'zerg') return <ZergEmblem width={size} height={size} style={style} />
  if (race === 'protoss') return <ProtossEmblem width={size} height={size} style={style} />
  return <TerranEmblem width={size} height={size} style={style} />
}

function RaceStripe() {
  return (
    <div
      className="h-[6px]"
      style={{
        background: 'linear-gradient(90deg, transparent 0%, var(--color-race-primary) 30%, var(--color-race-secondary) 50%, var(--color-race-primary) 70%, transparent 100%)',
      }}
    />
  )
}

function GameOverDialog({ payload, gameId, onClose }: { payload: GameFinalizedPayload; gameId: string; onClose: () => void }) {
  const navigate = useNavigate()
  const [countdown, setCountdown] = useState(8)
  useEffect(() => {
    if (countdown <= 0) { navigate(`/games/${gameId}/results`); return }
    const t = setTimeout(() => setCountdown((c) => c - 1), 1000)
    return () => clearTimeout(t)
  }, [countdown, navigate, gameId])

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <div className="flex justify-center mb-2"><Trophy className="h-10 w-10 text-prestige" aria-hidden /></div>
          <DialogTitle className="text-center text-2xl">{payload.winnerName ? `${payload.winnerName} Wins!` : 'Game Over!'}</DialogTitle>
        </DialogHeader>
        <p className="text-sm text-muted-foreground text-center">Redirecting to results in {countdown}s…</p>
        <DialogFooter><Button onClick={() => navigate(`/games/${gameId}/results`)}>View Results Now</Button></DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function UserMenu({ race }: { race: Race | null }) {
  const { user } = useCurrentUser()
  const navigate = useNavigate()
  const { gameId } = useCurrentGame()
  if (!user) return null
  const g = (p: string) => (gameId ? `/games/${gameId}/${p}` : `/${p}`)
  const name = user.displayName ?? user.playerName ?? 'Commander'
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button className="flex items-center gap-2 text-sm pr-1 hover:opacity-80">
          <span className="font-semibold" style={{ color: 'var(--color-race-primary)' }}>{name}</span>
          {race && <span className="text-[11px] text-muted-foreground capitalize">· {race}</span>}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel className="text-xs text-muted-foreground">{name}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => navigate(g('profile'))}><UserIcon className="mr-2 h-4 w-4" />Profile</DropdownMenuItem>
        <DropdownMenuItem onSelect={() => navigate(g('history'))}><HistoryIcon className="mr-2 h-4 w-4" />History</DropdownMenuItem>
        <DropdownMenuItem onSelect={() => navigate(g('stats'))}><LineChartIcon className="mr-2 h-4 w-4" />Stats</DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => { window.location.href = '/signout' }}><LogOutIcon className="mr-2 h-4 w-4" />Sign out</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function SidebarContent({ race }: { race: Race | null }) {
  const { currentGame } = useCurrentGame()
  const raceLabel = race ? race[0].toUpperCase() + race.slice(1) : ''
  return (
    <>
      <RaceStripe />
      <div className="px-4 py-3 border-b border-border flex items-center gap-3">
        <RaceEmblem race={race} size={28} />
        <div className="min-w-0">
          <div className="label" style={{ color: 'var(--color-race-primary)' }}>{raceLabel || 'Commander'}</div>
          <div className="text-[11px] text-muted-foreground truncate">{currentGame?.name ?? 'Game'}</div>
        </div>
      </div>
      <NavLink
        to="/games"
        className="flex items-center gap-1.5 px-4 py-2 text-[11px] text-interactive hover:text-[color:var(--color-race-secondary)] border-b border-border"
      >
        <ArrowLeftIcon className="h-3 w-3" aria-hidden /> All Games
      </NavLink>
      <NavMenu />
    </>
  )
}

function GameLayoutInner() {
  const { gameId, currentGame } = useCurrentGame()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [gameFinalized, setGameFinalized] = useState<GameFinalizedPayload | null>(null)
  const location = useLocation()
  const signalR = useSignalR('/gamehub')
  const { toasts, addToast, dismiss } = useNotificationToasts()
  const gameFinalizedHandlerRef = useRef<((...args: unknown[]) => void) | null>(null)
  const race = usePlayerRace()

  const isFinished = currentGame?.status === 'Finished'

  const { data: tickInfo } = useQuery<TickInfoViewModel>({
    queryKey: ['tickinfo', gameId],
    queryFn: () => apiClient.get('/api/game/tick-info').then((r) => r.data),
    refetchInterval: (query) => {
      const data = query.state.data
      if (!data) return 30_000
      const remaining = new Date(data.nextTickAt).getTime() - Date.now()
      if (remaining <= 0) return 1_000
      return Math.min(30_000, remaining + 500)
    },
    enabled: !!gameId && !isFinished,
  })

  const onRealTimeNotification = useCallback(
    (n: { type: string; title: string; body?: string; createdAt: string }) => addToast(n),
    [addToast],
  )

  useEffect(() => {
    if (!signalR) return
    const handler = (...args: unknown[]) => {
      const p = args[0] as GameFinalizedPayload
      if (p?.gameId === gameId) setGameFinalized(p)
    }
    gameFinalizedHandlerRef.current = handler
    signalR.on('GameFinalized', handler)
    return () => { signalR.off('GameFinalized', handler) }
  }, [signalR, gameId])

  useEffect(() => { setSidebarOpen(false) }, [location.pathname])

  if (!gameId) return <Navigate to="/games" />

  return (
    <div className="flex h-screen overflow-hidden bg-starfield">
      <aside className="hidden md:flex w-56 shrink-0 flex-col bg-card/40 backdrop-blur-sm border-r border-border overflow-y-auto" aria-label="Game sidebar">
        <SidebarContent race={race} />
      </aside>

      {sidebarOpen && (
        <div className="fixed inset-0 z-40 md:hidden" onClick={() => setSidebarOpen(false)} aria-hidden>
          <div className="absolute inset-0 bg-black/60" />
        </div>
      )}
      <aside className={cn(
        'fixed inset-y-0 left-0 z-50 w-56 flex flex-col border-r border-border bg-card overflow-y-auto transition-transform duration-200 md:hidden',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full',
      )} aria-label="Game sidebar">
        <div className="flex items-center justify-end p-2 border-b border-border">
          <Button variant="ghost" size="icon" onClick={() => setSidebarOpen(false)} aria-label="Close menu">
            <XIcon className="h-5 w-5" />
          </Button>
        </div>
        <SidebarContent race={race} />
      </aside>

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <header className="shrink-0 border-b border-border bg-background/40 backdrop-blur-sm">
          <div className="h-[52px] flex items-center justify-between px-3 sm:px-4 gap-3">
            <div className="flex items-center gap-3 min-w-0">
              <Button variant="ghost" size="icon" onClick={() => setSidebarOpen(true)} aria-label="Open menu" className="md:hidden -ml-1">
                <MenuIcon className="h-5 w-5" />
              </Button>
              <UserMenu race={race} />
              {tickInfo && !isFinished && <TimerRing to={tickInfo.nextTickAt} />}
            </div>
            <div className="flex items-center gap-3 sm:gap-4 min-w-0">
              <div className="hidden md:block"><PlayerResources gameId={gameId} /></div>
              <NotificationBell signalR={signalR} onRealTimeNotification={onRealTimeNotification} />
            </div>
          </div>
          <div className="md:hidden border-t border-border px-3 py-1.5">
            <PlayerResources gameId={gameId} />
          </div>
        </header>

        {currentGame && <GameStatusBanner game={currentGame} />}

        <main className="flex-1 overflow-y-auto p-3 sm:p-4">
          <Suspense fallback={<PageLoader />}>
            <div key={location.pathname} className="page-enter">
              <Outlet />
            </div>
          </Suspense>
        </main>
      </div>

      <NotificationToasts toasts={toasts} dismiss={dismiss} gameId={gameId} />
      <TutorialOverlay />
      {gameFinalized && <GameOverDialog payload={gameFinalized} gameId={gameId} onClose={() => setGameFinalized(null)} />}
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
