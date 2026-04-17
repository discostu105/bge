import { Suspense, useState, useCallback, useEffect, useRef } from 'react'
import { Outlet, useParams, Navigate, useLocation, useNavigate, NavLink } from 'react-router'
import {
	GamepadIcon, LogOutIcon, MenuIcon, XIcon, WifiIcon, WifiOffIcon, SunIcon, MoonIcon,
	UserIcon, HistoryIcon, LineChartIcon, Trophy,
} from 'lucide-react'
import { CurrentGameProvider, useCurrentGame } from '@/contexts/CurrentGameContext'
import { useTheme } from '@/contexts/ThemeContext'
import { useCurrentUser } from '@/contexts/CurrentUserContext'
import { NavMenu } from '@/components/NavMenu'
import { PlayerResources } from '@/components/PlayerResources'
import { NotificationBell } from '@/components/NotificationBell'
import { NotificationToasts, useNotificationToasts } from '@/components/NotificationToast'
import { TutorialOverlay } from '@/components/TutorialOverlay'
import { PageLoader } from '@/components/PageLoader'
import { GameStatusBanner } from '@/components/GameStatusBanner'
import { useSignalR } from '@/hooks/useSignalR'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
	Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog'
import {
	DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'

interface GameFinalizedPayload {
	gameId: string
	winnerId: string | null
	winnerName: string | null
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
					<div className="flex justify-center mb-2">
						<Trophy className="h-10 w-10 text-primary" aria-hidden />
					</div>
					<DialogTitle className="text-center text-2xl">
						{payload.winnerName ? `${payload.winnerName} Wins!` : 'Game Over!'}
					</DialogTitle>
				</DialogHeader>
				<p className="text-sm text-muted-foreground text-center">Redirecting to results in {countdown}s…</p>
				<DialogFooter>
					<Button onClick={() => navigate(`/games/${gameId}/results`)}>View Results Now</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	)
}

function ThemeToggleButton() {
	const { theme, toggleTheme } = useTheme()
	return (
		<Button variant="ghost" size="icon" onClick={toggleTheme} aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}>
			{theme === 'dark' ? <SunIcon className="h-4 w-4" /> : <MoonIcon className="h-4 w-4" />}
		</Button>
	)
}

function UserMenu() {
	const { user } = useCurrentUser()
	const navigate = useNavigate()
	const { gameId } = useCurrentGame()
	if (!user) return null
	const g = (p: string) => (gameId ? `/games/${gameId}/${p}` : `/${p}`)
	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button variant="ghost" size="sm" className="gap-2">
					<UserIcon className="h-4 w-4" />
					<span className="hidden sm:inline max-w-[120px] truncate">{user.displayName ?? user.playerName ?? 'Account'}</span>
				</Button>
			</DropdownMenuTrigger>
			<DropdownMenuContent align="end" className="w-48">
				<DropdownMenuLabel className="text-xs text-muted-foreground">{user.displayName ?? user.playerName}</DropdownMenuLabel>
				<DropdownMenuSeparator />
				<DropdownMenuItem onSelect={() => navigate(g('profile'))}><UserIcon className="mr-2 h-4 w-4" />Profile</DropdownMenuItem>
				<DropdownMenuItem onSelect={() => navigate(g('history'))}><HistoryIcon className="mr-2 h-4 w-4" />History</DropdownMenuItem>
				<DropdownMenuItem onSelect={() => navigate(g('stats'))}><LineChartIcon className="mr-2 h-4 w-4" />Stats</DropdownMenuItem>
				<DropdownMenuSeparator />
				<DropdownMenuItem onSelect={() => { window.location.href = '/signout' }}>
					<LogOutIcon className="mr-2 h-4 w-4" />Sign out
				</DropdownMenuItem>
			</DropdownMenuContent>
		</DropdownMenu>
	)
}

function ConnectionPill({ status }: { status: string }) {
	const ok = status === 'connected'
	return (
		<Badge variant="secondary" className={cn('gap-1.5 hidden sm:inline-flex', ok ? '' : 'text-muted-foreground')} title={`Real-time: ${status}`}>
			{ok ? <WifiIcon className="h-3 w-3 text-success" /> : <WifiOffIcon className="h-3 w-3" />}
			<span className="text-[10px] uppercase tracking-wider">{ok ? 'Live' : status === 'connecting' ? 'Connecting' : 'Offline'}</span>
		</Badge>
	)
}

function SidebarContent({ currentGame }: { currentGame: { name: string } | null }) {
	return (
		<>
			<div className="px-4 py-3 border-b">
				<div className="flex items-center gap-2">
					<GamepadIcon className="h-5 w-5 text-primary" aria-hidden />
					<span className="font-semibold text-sm truncate">{currentGame?.name ?? 'Game'}</span>
				</div>
				<NavLink to="/games" className="text-xs text-muted-foreground hover:text-foreground mt-1 block">
					← All Games
				</NavLink>
			</div>
			<div className="flex-1 py-2"><NavMenu /></div>
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
		<div className="flex h-screen overflow-hidden bg-background">
			{/* Desktop sidebar */}
			<aside className="hidden md:flex w-56 shrink-0 flex-col border-r bg-card overflow-y-auto" aria-label="Game sidebar">
				<SidebarContent currentGame={currentGame} />
			</aside>

			{/* Mobile sidebar overlay */}
			{sidebarOpen && (
				<div className="fixed inset-0 z-40 md:hidden" onClick={() => setSidebarOpen(false)} aria-hidden>
					<div className="absolute inset-0 bg-black/60" />
				</div>
			)}
			<aside
				className={cn(
					'fixed inset-y-0 left-0 z-50 w-56 flex flex-col border-r bg-card overflow-y-auto transition-transform duration-200 md:hidden',
					sidebarOpen ? 'translate-x-0' : '-translate-x-full',
				)}
				aria-label="Game sidebar"
			>
				<div className="flex items-center justify-end p-2 border-b">
					<Button variant="ghost" size="icon" onClick={() => setSidebarOpen(false)} aria-label="Close menu">
						<XIcon className="h-5 w-5" />
					</Button>
				</div>
				<SidebarContent currentGame={currentGame} />
			</aside>

			{/* Main */}
			<div className="flex-1 flex flex-col min-w-0 overflow-hidden">
				{/* Top bar */}
				<header className="shrink-0 border-b bg-card">
					<div className="h-12 flex items-center justify-between px-3 sm:px-4">
						<div className="flex items-center gap-2 min-w-0">
							<Button variant="ghost" size="icon" onClick={() => setSidebarOpen(true)} aria-label="Open menu" className="md:hidden -ml-1">
								<MenuIcon className="h-5 w-5" />
							</Button>
							<span className="font-semibold text-sm truncate hidden md:inline">{currentGame?.name ?? 'Game'}</span>
						</div>
						<div className="flex items-center gap-1 sm:gap-2">
							<div className="hidden md:block"><PlayerResources gameId={gameId} /></div>
							<ConnectionPill status={signalR.status} />
							<NotificationBell signalR={signalR} onRealTimeNotification={onRealTimeNotification} />
							<ThemeToggleButton />
							<UserMenu />
						</div>
					</div>
					{/* Mobile-only second row for resources */}
					<div className="md:hidden border-t px-3 py-1.5">
						<PlayerResources gameId={gameId} />
					</div>
				</header>

				{/* Banners */}
				{currentGame && <GameStatusBanner game={currentGame} />}

				{/* Page content */}
				<main className="flex-1 overflow-y-auto p-3 sm:p-4 bg-grid">
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
