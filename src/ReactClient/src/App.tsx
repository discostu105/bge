import { createBrowserRouter, RouterProvider, Outlet, useParams, Navigate } from 'react-router'
import { Base } from '@/pages/Base'
import { Units } from '@/pages/Units'
import { Research } from '@/pages/Research'
import { Market } from '@/pages/Market'

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

const router = createBrowserRouter([
  {
    path: '/',
    element: <TodoPage name="Index" />,
  },
  {
    path: '/games',
    element: <TodoPage name="Games" />,
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
      { path: 'diplomacy', element: <TodoPage name="Diplomacy" /> },
      { path: 'spies', element: <TodoPage name="Spies" /> },
      { path: 'spy', element: <TodoPage name="Spy Reports" /> },
      { path: 'selectenemy/:unitId', element: <TodoPage name="Select Enemy" /> },
      { path: 'unitdefinitions', element: <TodoPage name="Unit Definitions" /> },
      { path: 'player/:playerId', element: <TodoPage name="Player Profile" /> },
      { path: 'enemybase/:enemyPlayerId', element: <TodoPage name="Enemy Base" /> },
      { path: 'join', element: <TodoPage name="Join Game" /> },
      { path: 'summary', element: <TodoPage name="Game Summary" /> },
      { path: 'results', element: <TodoPage name="Game Results" /> },
    ],
  },
  {
    path: '/createplayer',
    element: <TodoPage name="Create Player" />,
  },
  {
    path: '/lobby',
    element: <TodoPage name="Game Lobby" />,
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
    element: <TodoPage name="Achievements" />,
  },
  {
    path: '/history',
    element: <TodoPage name="Player History" />,
  },
  {
    path: '/admin/games',
    element: <TodoPage name="Admin — Games" />,
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
