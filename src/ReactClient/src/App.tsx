import { createBrowserRouter, RouterProvider, Outlet, useParams, Navigate } from 'react-router'
import { Base } from '@/pages/Base'
import { Units } from '@/pages/Units'
import { Research } from '@/pages/Research'
import { Market } from '@/pages/Market'
import { Spies } from '@/pages/Spies'
import { SpyReports } from '@/pages/SpyReports'
import { Diplomacy } from '@/pages/Diplomacy'
import { EnemyBase } from '@/pages/EnemyBase'
import { SelectEnemy } from '@/pages/SelectEnemy'
import { Index } from '@/pages/Index'
import { Games } from '@/pages/Games'
import { GameLobby } from '@/pages/GameLobby'
import { JoinGame } from '@/pages/JoinGame'
import { GameSummary } from '@/pages/GameSummary'
import { GameResults } from '@/pages/GameResults'
import { CreatePlayer } from '@/pages/CreatePlayer'
import { UnitDefinitions } from '@/pages/UnitDefinitions'
import { Achievements } from '@/pages/Achievements'
import { PlayerHistory } from '@/pages/PlayerHistory'
import { AdminGames } from '@/pages/admin/AdminGames'

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

// Game layout wrapper — provides gameId to child pages
function GameLayout() {
  const { gameId } = useParams<{ gameId: string }>()
  if (!gameId) return <Navigate to="/games" />
  return <Outlet context={{ gameId }} />
}

// Wrappers that pull gameId from router context
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

function SpiesPage() {
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

const router = createBrowserRouter([
  {
    path: '/',
    element: <Index />,
  },
  {
    path: '/games',
    element: <Games />,
  },
  {
    path: '/games/:gameId',
    element: <GameLayout />,
    children: [
      { path: 'base', element: <BasePage /> },
      { path: 'units', element: <UnitsPage /> },
      { path: 'research', element: <ResearchPage /> },
      { path: 'upgrades', element: <ResearchPage /> },
      { path: 'market', element: <MarketPage /> },
      { path: 'chat', element: <TodoPage name="Chat" /> },
      { path: 'messages', element: <TodoPage name="Messages" /> },
      { path: 'ranking', element: <TodoPage name="Player Ranking" /> },
      { path: 'alliances', element: <TodoPage name="Alliances" /> },
      { path: 'allianceranking', element: <TodoPage name="Alliance Ranking" /> },
      { path: 'diplomacy', element: <DiplomacyPage /> },
      { path: 'spies', element: <SpiesPage /> },
      { path: 'spy', element: <SpyReportsPage /> },
      { path: 'selectenemy/:unitId', element: <SelectEnemyPage /> },
      { path: 'unitdefinitions', element: <UnitDefinitions /> },
      { path: 'player/:playerId', element: <TodoPage name="Player Profile" /> },
      { path: 'enemybase/:enemyPlayerId', element: <EnemyBasePage /> },
      { path: 'join', element: <JoinGame /> },
      { path: 'summary', element: <GameSummary /> },
      { path: 'results', element: <GameResults /> },
    ],
  },
  {
    path: '/createplayer',
    element: <CreatePlayer />,
  },
  {
    path: '/lobby',
    element: <GameLobby />,
  },
  {
    path: '/profile',
    element: <TodoPage name="Player Profile" />,
  },
  {
    path: '/profile/:userId',
    element: <TodoPage name="Public Profile" />,
  },
  {
    path: '/players',
    element: <TodoPage name="Players" />,
  },
  {
    path: '/alliances/:allianceId',
    element: <TodoPage name="Alliance Detail" />,
  },
  {
    path: '/achievements',
    element: <Achievements />,
  },
  {
    path: '/history',
    element: <PlayerHistory />,
  },
  {
    path: '/admin/games',
    element: <AdminGames />,
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
