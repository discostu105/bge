import { lazy, Suspense, type ReactNode } from 'react'
import { createBrowserRouter, RouterProvider, useParams } from 'react-router'
import { RootLayout } from '@/layouts/RootLayout'
import { GameLayout } from '@/layouts/GameLayout'
import { BareRouteRedirect } from '@/components/BareRouteRedirect'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { PageLoader } from '@/components/PageLoader'

// Eagerly loaded — must render instantly
import { SignIn } from '@/pages/SignIn'
import { Index } from '@/pages/Index'

// Wraps a lazy route element in ErrorBoundary + Suspense with a page-level loading fallback
function RouteWithBoundary({ children }: { children: ReactNode }) {
	return (
		<ErrorBoundary>
			<Suspense fallback={<PageLoader />}>
				{children}
			</Suspense>
		</ErrorBoundary>
	)
}

// Lazy-loaded pages — split into separate chunks
const Base = lazy(() => import('@/pages/Base').then(m => ({ default: m.Base })))
const Units = lazy(() => import('@/pages/Units').then(m => ({ default: m.Units })))
const Research = lazy(() => import('@/pages/Research').then(m => ({ default: m.Research })))
const Market = lazy(() => import('@/pages/Market').then(m => ({ default: m.Market })))
const Diplomacy = lazy(() => import('@/pages/Diplomacy').then(m => ({ default: m.Diplomacy })))
const EnemyBase = lazy(() => import('@/pages/EnemyBase').then(m => ({ default: m.EnemyBase })))
const SelectEnemy = lazy(() => import('@/pages/SelectEnemy').then(m => ({ default: m.SelectEnemy })))
const Chat = lazy(() => import('@/pages/Chat').then(m => ({ default: m.Chat })))
const Messages = lazy(() => import('@/pages/Messages').then(m => ({ default: m.Messages })))
const PlayerRanking = lazy(() => import('@/pages/PlayerRanking').then(m => ({ default: m.PlayerRanking })))
const PlayerProfile = lazy(() => import('@/pages/PlayerProfile').then(m => ({ default: m.PlayerProfile })))
const InGamePlayerProfile = lazy(() => import('@/pages/InGamePlayerProfile').then(m => ({ default: m.InGamePlayerProfile })))
const Games = lazy(() => import('@/pages/Games').then(m => ({ default: m.Games })))
const GameLobby = lazy(() => import('@/pages/GameLobby').then(m => ({ default: m.GameLobby })))
const GameLobbyDetail = lazy(() => import('@/pages/GameLobbyDetail').then(m => ({ default: m.GameLobbyDetail })))
const JoinGame = lazy(() => import('@/pages/JoinGame').then(m => ({ default: m.JoinGame })))
const GameSummary = lazy(() => import('@/pages/GameSummary').then(m => ({ default: m.GameSummary })))
const GameResults = lazy(() => import('@/pages/GameResults').then(m => ({ default: m.GameResults })))
const CreatePlayer = lazy(() => import('@/pages/CreatePlayer').then(m => ({ default: m.CreatePlayer })))
const UnitDefinitions = lazy(() => import('@/pages/UnitDefinitions').then(m => ({ default: m.UnitDefinitions })))
const Alliances = lazy(() => import('@/pages/Alliances').then(m => ({ default: m.Alliances })))
const AllianceDetail = lazy(() => import('@/pages/AllianceDetail').then(m => ({ default: m.AllianceDetail })))
const PlayerHistory = lazy(() => import('@/pages/PlayerHistory').then(m => ({ default: m.PlayerHistory })))
const PublicProfile = lazy(() => import('@/pages/PublicProfile').then(m => ({ default: m.PublicProfile })))
const AdminGames = lazy(() => import('@/pages/admin/AdminGames').then(m => ({ default: m.AdminGames })))
const AdminPlayers = lazy(() => import('@/pages/admin/AdminPlayers').then(m => ({ default: m.AdminPlayers })))
const AdminTickControl = lazy(() => import('@/pages/admin/AdminTickControl').then(m => ({ default: m.AdminTickControl })))
const AdminStats = lazy(() => import('@/pages/admin/AdminStats').then(m => ({ default: m.AdminStats })))
const AdminMetrics = lazy(() => import('@/pages/admin/AdminMetrics').then(m => ({ default: m.AdminMetrics })))
const AdminAuditLog = lazy(() => import('@/pages/admin/AdminAuditLog').then(m => ({ default: m.AdminAuditLog })))
const AdminReports = lazy(() => import('@/pages/admin/AdminReports').then(m => ({ default: m.AdminReports })))
const PlayersList = lazy(() => import('@/pages/PlayersList').then(m => ({ default: m.PlayersList })))
const Trade = lazy(() => import('@/pages/Trade').then(m => ({ default: m.Trade })))
const Help = lazy(() => import('@/pages/Help').then(m => ({ default: m.Help })))
const BattleReplay = lazy(() => import('@/pages/BattleReplay').then(m => ({ default: m.BattleReplay })))
const ReplayViewer = lazy(() => import('@/pages/ReplayViewer').then(m => ({ default: m.ReplayViewer })))
const GameLiveView = lazy(() => import('@/pages/GameLiveView').then(m => ({ default: m.GameLiveView })))
const PlayerStats = lazy(() => import('@/pages/PlayerStats').then(m => ({ default: m.PlayerStats })))
const TournamentResults = lazy(() => import('@/pages/TournamentResults').then(m => ({ default: m.TournamentResults })))
const TournamentList = lazy(() => import('@/pages/TournamentList').then(m => ({ default: m.TournamentList })))
const TournamentDetail = lazy(() => import('@/pages/TournamentDetail').then(m => ({ default: m.TournamentDetail })))
const Spectator = lazy(() => import('@/pages/Spectator').then(m => ({ default: m.Spectator })))
const Shop = lazy(() => import('@/pages/Shop').then(m => ({ default: m.Shop })))
const Economy = lazy(() => import('@/pages/Economy').then(m => ({ default: m.Economy })))

// Stub component for pages not yet implemented
function TodoPage({ name }: { name: string }) {
  return (
    <div className="flex items-center justify-center min-h-64">
      <div className="text-center">
        <div className="text-4xl mb-3">🚧</div>
        <div className="text-lg font-semibold">{name}</div>
        <div className="text-sm text-muted-foreground mt-1">Coming soon</div>
      </div>
    </div>
  )
}

// Thin wrappers that pluck gameId from router params and pass to page components
function BasePage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Base gameId={gameId} />
}
function UnitsPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Units gameId={gameId} />
}
function ResearchPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Research gameId={gameId} />
}
function MarketPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Market gameId={gameId} />
}
function DiplomacyPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Diplomacy gameId={gameId} />
}
function EnemyBasePage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <EnemyBase gameId={gameId} />
}
function SelectEnemyPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <SelectEnemy gameId={gameId} />
}
function ChatPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Chat gameId={gameId} />
}
function MessagesPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Messages gameId={gameId} />
}
function PlayerRankingPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <PlayerRanking gameId={gameId} />
}
function InGamePlayerProfilePage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <InGamePlayerProfile gameId={gameId} />
}
function TradePage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Trade gameId={gameId} />
}
function BattleReplayPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <BattleReplay gameId={gameId} />
}
function AlliancesPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Alliances gameId={gameId} />
}
function GameLiveViewPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <GameLiveView gameId={gameId} />
}
function SpectatorPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Spectator gameId={gameId} />
}

const router = createBrowserRouter([
	{
		element: <RootLayout />,
		children: [
			{ path: '/signin', element: <SignIn /> },

			{ path: '/', element: <Index /> },
			{ path: '/games', element: <RouteWithBoundary><Games /></RouteWithBoundary> },

			{
				path: '/games/:gameId',
				element: <GameLayout />,
				children: [
					{ path: 'base', element: <RouteWithBoundary><BasePage /></RouteWithBoundary> },
					{ path: 'units', element: <RouteWithBoundary><UnitsPage /></RouteWithBoundary> },
					{ path: 'research', element: <RouteWithBoundary><ResearchPage /></RouteWithBoundary> },
					{ path: 'upgrades', element: <RouteWithBoundary><ResearchPage /></RouteWithBoundary> },
					{ path: 'market', element: <RouteWithBoundary><MarketPage /></RouteWithBoundary> },
					{ path: 'trade', element: <RouteWithBoundary><TradePage /></RouteWithBoundary> },
					{ path: 'chat', element: <RouteWithBoundary><ChatPage /></RouteWithBoundary> },
					{ path: 'messages', element: <RouteWithBoundary><MessagesPage /></RouteWithBoundary> },
					{ path: 'ranking', element: <RouteWithBoundary><PlayerRankingPage /></RouteWithBoundary> },
					{ path: 'alliances', element: <RouteWithBoundary><AlliancesPage /></RouteWithBoundary> },
					{ path: 'allianceranking', element: <TodoPage name="Alliance Ranking" /> },
					{ path: 'diplomacy', element: <RouteWithBoundary><DiplomacyPage /></RouteWithBoundary> },
					{ path: 'selectenemy/:unitId', element: <RouteWithBoundary><SelectEnemyPage /></RouteWithBoundary> },
					{ path: 'unitdefinitions', element: <RouteWithBoundary><UnitDefinitions /></RouteWithBoundary> },
					{ path: 'help', element: <RouteWithBoundary><Help /></RouteWithBoundary> },
					{ path: 'player/:playerId', element: <RouteWithBoundary><InGamePlayerProfilePage /></RouteWithBoundary> },
					{ path: 'enemybase/:enemyPlayerId', element: <RouteWithBoundary><EnemyBasePage /></RouteWithBoundary> },
					{ path: 'battles/:reportId', element: <RouteWithBoundary><BattleReplayPage /></RouteWithBoundary> },
					{ path: 'live', element: <RouteWithBoundary><GameLiveViewPage /></RouteWithBoundary> },
					{ path: 'join', element: <RouteWithBoundary><JoinGame /></RouteWithBoundary> },
					{ path: 'summary', element: <RouteWithBoundary><GameSummary /></RouteWithBoundary> },
					{ path: 'results', element: <RouteWithBoundary><GameResults /></RouteWithBoundary> },

					// Account pages (also available at top-level, but rendered inside game layout when accessed from nav)
					{ path: 'history', element: <RouteWithBoundary><PlayerHistory /></RouteWithBoundary> },
					{ path: 'stats', element: <RouteWithBoundary><PlayerStats /></RouteWithBoundary> },
					{ path: 'profile', element: <RouteWithBoundary><PlayerProfile /></RouteWithBoundary> },
					{ path: 'shop', element: <RouteWithBoundary><Shop /></RouteWithBoundary> },
					{ path: 'economy', element: <RouteWithBoundary><Economy /></RouteWithBoundary> },
				],
			},

			// Bare routes — redirect to current game
			{ path: '/base', element: <BareRouteRedirect path="base" /> },
			{ path: '/units', element: <BareRouteRedirect path="units" /> },
			{ path: '/research', element: <BareRouteRedirect path="research" /> },
			{ path: '/market', element: <BareRouteRedirect path="market" /> },
			{ path: '/trade', element: <BareRouteRedirect path="trade" /> },

			{ path: '/createplayer', element: <RouteWithBoundary><CreatePlayer /></RouteWithBoundary> },
			{ path: '/lobby', element: <RouteWithBoundary><GameLobby /></RouteWithBoundary> },
			{ path: '/lobby/:gameId', element: <RouteWithBoundary><GameLobbyDetail /></RouteWithBoundary> },
			{ path: '/profile', element: <BareRouteRedirect path="profile" /> },
			{ path: '/profile/:userId', element: <RouteWithBoundary><PublicProfile /></RouteWithBoundary> },
			{ path: '/players', element: <RouteWithBoundary><PlayersList /></RouteWithBoundary> },
			{ path: '/alliances/:allianceId', element: <RouteWithBoundary><AllianceDetail /></RouteWithBoundary> },
			{ path: '/history', element: <BareRouteRedirect path="history" /> },
			{ path: '/replays/:gameId', element: <RouteWithBoundary><ReplayViewer /></RouteWithBoundary> },
			{ path: '/stats', element: <BareRouteRedirect path="stats" /> },
			{ path: '/tournaments', element: <RouteWithBoundary><TournamentList /></RouteWithBoundary> },
			{ path: '/tournaments/:tournamentId', element: <RouteWithBoundary><TournamentDetail /></RouteWithBoundary> },
			{ path: '/tournaments/:tournamentId/results', element: <RouteWithBoundary><TournamentResults /></RouteWithBoundary> },
			{ path: '/shop', element: <BareRouteRedirect path="shop" /> },
			{ path: '/economy', element: <BareRouteRedirect path="economy" /> },
			{ path: '/games/:gameId/spectate', element: <RouteWithBoundary><SpectatorPage /></RouteWithBoundary> },
			{ path: '/admin/games', element: <RouteWithBoundary><AdminGames /></RouteWithBoundary> },
			{ path: '/admin/players', element: <RouteWithBoundary><AdminPlayers /></RouteWithBoundary> },
			{ path: '/admin/ticks', element: <RouteWithBoundary><AdminTickControl /></RouteWithBoundary> },
			{ path: '/admin/stats', element: <RouteWithBoundary><AdminStats /></RouteWithBoundary> },
			{ path: '/admin/metrics', element: <RouteWithBoundary><AdminMetrics /></RouteWithBoundary> },
			{ path: '/admin/audit', element: <RouteWithBoundary><AdminAuditLog /></RouteWithBoundary> },
			{ path: '/admin/reports', element: <RouteWithBoundary><AdminReports /></RouteWithBoundary> },
		],
	},
])

export default function App() {
  return <RouterProvider router={router} />
}
