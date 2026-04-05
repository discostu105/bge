import { lazy } from 'react'
import { createBrowserRouter, RouterProvider, useParams } from 'react-router'
import { RootLayout } from '@/layouts/RootLayout'
import { GameLayout } from '@/layouts/GameLayout'
import { BareRouteRedirect } from '@/components/BareRouteRedirect'

// Eagerly loaded — must render instantly
import { SignIn } from '@/pages/SignIn'
import { Index } from '@/pages/Index'

// Lazy-loaded pages — split into separate chunks
const Base = lazy(() => import('@/pages/Base').then(m => ({ default: m.Base })))
const Units = lazy(() => import('@/pages/Units').then(m => ({ default: m.Units })))
const Research = lazy(() => import('@/pages/Research').then(m => ({ default: m.Research })))
const Market = lazy(() => import('@/pages/Market').then(m => ({ default: m.Market })))
const Spies = lazy(() => import('@/pages/Spies').then(m => ({ default: m.Spies })))
const SpyReports = lazy(() => import('@/pages/SpyReports').then(m => ({ default: m.SpyReports })))
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
const JoinGame = lazy(() => import('@/pages/JoinGame').then(m => ({ default: m.JoinGame })))
const GameSummary = lazy(() => import('@/pages/GameSummary').then(m => ({ default: m.GameSummary })))
const GameResults = lazy(() => import('@/pages/GameResults').then(m => ({ default: m.GameResults })))
const CreatePlayer = lazy(() => import('@/pages/CreatePlayer').then(m => ({ default: m.CreatePlayer })))
const UnitDefinitions = lazy(() => import('@/pages/UnitDefinitions').then(m => ({ default: m.UnitDefinitions })))
const Alliances = lazy(() => import('@/pages/Alliances').then(m => ({ default: m.Alliances })))
const AllianceDetail = lazy(() => import('@/pages/AllianceDetail').then(m => ({ default: m.AllianceDetail })))
const Achievements = lazy(() => import('@/pages/Achievements').then(m => ({ default: m.Achievements })))
const PlayerHistory = lazy(() => import('@/pages/PlayerHistory').then(m => ({ default: m.PlayerHistory })))
const PublicProfile = lazy(() => import('@/pages/PublicProfile').then(m => ({ default: m.PublicProfile })))
const AdminGames = lazy(() => import('@/pages/admin/AdminGames').then(m => ({ default: m.AdminGames })))
const AdminPlayers = lazy(() => import('@/pages/admin/AdminPlayers').then(m => ({ default: m.AdminPlayers })))
const AdminTickControl = lazy(() => import('@/pages/admin/AdminTickControl').then(m => ({ default: m.AdminTickControl })))
const AdminStats = lazy(() => import('@/pages/admin/AdminStats').then(m => ({ default: m.AdminStats })))
const PlayersList = lazy(() => import('@/pages/PlayersList').then(m => ({ default: m.PlayersList })))
const Trade = lazy(() => import('@/pages/Trade').then(m => ({ default: m.Trade })))

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
function OperationsPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Spies gameId={gameId} />
}
function SpyReportsPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <SpyReports gameId={gameId} />
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
function AlliancesPage() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return null
  return <Alliances gameId={gameId} />
}

const router = createBrowserRouter([
  {
    element: <RootLayout />,
    children: [
      { path: '/signin', element: <SignIn /> },

      { path: '/', element: <Index /> },
      { path: '/games', element: <Games /> },

      {
        path: '/games/:gameId',
        element: <GameLayout />,
        children: [
          { path: 'base', element: <BasePage /> },
          { path: 'units', element: <UnitsPage /> },
          { path: 'research', element: <ResearchPage /> },
          { path: 'upgrades', element: <ResearchPage /> },
          { path: 'market', element: <MarketPage /> },
          { path: 'trade', element: <TradePage /> },
          { path: 'chat', element: <ChatPage /> },
          { path: 'messages', element: <MessagesPage /> },
          { path: 'ranking', element: <PlayerRankingPage /> },
          { path: 'alliances', element: <AlliancesPage /> },
          { path: 'allianceranking', element: <TodoPage name="Alliance Ranking" /> },
          { path: 'diplomacy', element: <DiplomacyPage /> },
          { path: 'operations', element: <OperationsPage /> },
          { path: 'spy', element: <SpyReportsPage /> },
          { path: 'selectenemy/:unitId', element: <SelectEnemyPage /> },
          { path: 'unitdefinitions', element: <UnitDefinitions /> },
          { path: 'player/:playerId', element: <InGamePlayerProfilePage /> },
          { path: 'enemybase/:enemyPlayerId', element: <EnemyBasePage /> },
          { path: 'join', element: <JoinGame /> },
          { path: 'summary', element: <GameSummary /> },
          { path: 'results', element: <GameResults /> },
        ],
      },

      // Bare routes — redirect to current game
      { path: '/base', element: <BareRouteRedirect path="base" /> },
      { path: '/units', element: <BareRouteRedirect path="units" /> },
      { path: '/research', element: <BareRouteRedirect path="research" /> },
      { path: '/market', element: <BareRouteRedirect path="market" /> },
      { path: '/trade', element: <BareRouteRedirect path="trade" /> },
      { path: '/spy', element: <BareRouteRedirect path="spy" /> },
      { path: '/operations', element: <BareRouteRedirect path="operations" /> },

      { path: '/createplayer', element: <CreatePlayer /> },
      { path: '/lobby', element: <GameLobby /> },
      { path: '/profile', element: <PlayerProfile /> },
      { path: '/profile/:userId', element: <PublicProfile /> },
      { path: '/players', element: <PlayersList /> },
      { path: '/alliances/:allianceId', element: <AllianceDetail /> },
      { path: '/achievements', element: <Achievements /> },
      { path: '/history', element: <PlayerHistory /> },
      { path: '/admin/games', element: <AdminGames /> },
      { path: '/admin/players', element: <AdminPlayers /> },
      { path: '/admin/ticks', element: <AdminTickControl /> },
      { path: '/admin/stats', element: <AdminStats /> },
    ],
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
