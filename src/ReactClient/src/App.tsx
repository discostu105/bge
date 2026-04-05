import { createBrowserRouter, RouterProvider, useParams } from 'react-router'
import { RootLayout } from '@/layouts/RootLayout'
import { GameLayout } from '@/layouts/GameLayout'
import { BareRouteRedirect } from '@/components/BareRouteRedirect'
import { SignIn } from '@/pages/SignIn'
import { Base } from '@/pages/Base'
import { Units } from '@/pages/Units'
import { Research } from '@/pages/Research'
import { Market } from '@/pages/Market'
import { Spies } from '@/pages/Spies'
import { SpyReports } from '@/pages/SpyReports'
import { Diplomacy } from '@/pages/Diplomacy'
import { EnemyBase } from '@/pages/EnemyBase'
import { SelectEnemy } from '@/pages/SelectEnemy'

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
    element: <RootLayout />,
    children: [
      { path: '/signin', element: <SignIn /> },

      { path: '/', element: <TodoPage name="Home" /> },
      { path: '/games', element: <TodoPage name="Games" /> },

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
          { path: 'unitdefinitions', element: <TodoPage name="Unit Definitions" /> },
          { path: 'player/:playerId', element: <TodoPage name="Player Profile" /> },
          { path: 'enemybase/:enemyPlayerId', element: <EnemyBasePage /> },
          { path: 'join', element: <TodoPage name="Join Game" /> },
          { path: 'summary', element: <TodoPage name="Game Summary" /> },
          { path: 'results', element: <TodoPage name="Game Results" /> },
        ],
      },

      // Bare routes — redirect to current game
      { path: '/base', element: <BareRouteRedirect path="base" /> },
      { path: '/units', element: <BareRouteRedirect path="units" /> },
      { path: '/research', element: <BareRouteRedirect path="research" /> },
      { path: '/market', element: <BareRouteRedirect path="market" /> },
      { path: '/spy', element: <BareRouteRedirect path="spy" /> },
      { path: '/spies', element: <BareRouteRedirect path="spies" /> },

      { path: '/createplayer', element: <TodoPage name="Create Player" /> },
      { path: '/lobby', element: <TodoPage name="Game Lobby" /> },
      { path: '/profile', element: <TodoPage name="Player Profile" /> },
      { path: '/profile/:userId', element: <TodoPage name="Public Profile" /> },
      { path: '/players', element: <TodoPage name="Players" /> },
      { path: '/alliances/:allianceId', element: <TodoPage name="Alliance Detail" /> },
      { path: '/achievements', element: <TodoPage name="Achievements" /> },
      { path: '/history', element: <TodoPage name="Player History" /> },
      { path: '/admin/games', element: <TodoPage name="Admin — Games" /> },
    ],
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
