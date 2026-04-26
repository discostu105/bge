import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel } from '@/api/types'

export type AdminGameSection = 'players' | 'ticks' | 'stats'

function pickDefaultGame(games: GameSummaryViewModel[]): GameSummaryViewModel | null {
  return games.find((g) => g.status === 'Active')
    ?? games.find((g) => g.status === 'Upcoming')
    ?? games[0]
    ?? null
}

interface PickerProps {
  section: AdminGameSection
  currentGameId: string | null
}

export function AdminGameSwitcher({ section, currentGameId }: PickerProps) {
  const navigate = useNavigate()
  const { data } = useQuery<GameListViewModel>({
    queryKey: ['admin-game-switcher'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
  })
  const games = data?.games ?? []
  const current = currentGameId ? games.find((g) => g.gameId === currentGameId) : null

  return (
    <div className="flex items-center gap-2 mb-4 text-sm">
      <span className="text-muted-foreground">Game:</span>
      <select
        className="rounded border border-input bg-background px-2 py-1 text-sm"
        value={currentGameId ?? ''}
        onChange={(e) => {
          const id = e.target.value
          if (!id) return
          navigate(`/admin/${section}/${id}`)
        }}
      >
        {!current && <option value="">— select a game —</option>}
        {games.map((g) => (
          <option key={g.gameId} value={g.gameId}>
            {g.name} ({g.status})
          </option>
        ))}
      </select>
    </div>
  )
}

export function AdminGamePickerPage({ section, title }: { section: AdminGameSection; title: string }) {
  const { data, isLoading } = useQuery<GameListViewModel>({
    queryKey: ['admin-game-picker'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
  })
  const games = data?.games ?? []
  const defaultGame = pickDefaultGame(games)

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        {title} are scoped per game. Pick a game to manage:
      </p>
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading games…</p>
      ) : games.length === 0 ? (
        <p className="text-sm text-muted-foreground">No games found.</p>
      ) : (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th scope="col" className="px-3 py-2 text-left font-medium">Name</th>
                <th scope="col" className="px-3 py-2 text-left font-medium">Status</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Players</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Action</th>
              </tr>
            </thead>
            <tbody>
              {games.map((g) => (
                <tr key={g.gameId} className="border-b border-border/50">
                  <td className="px-3 py-2 font-medium">{g.name}</td>
                  <td className="px-3 py-2">
                    <span
                      className={`text-xs px-2 py-0.5 rounded font-bold ${
                        g.status === 'Active'
                          ? 'bg-success text-success-foreground'
                          : g.status === 'Upcoming'
                          ? 'bg-warning text-warning-foreground'
                          : 'bg-secondary text-secondary-foreground'
                      }`}
                    >
                      {g.status}
                    </span>
                    {defaultGame?.gameId === g.gameId && (
                      <span className="ml-2 text-xs text-muted-foreground">(default)</span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right text-muted-foreground">{g.playerCount}</td>
                  <td className="px-3 py-2 text-right">
                    <a
                      href={`/admin/${section}/${g.gameId}`}
                      className="px-2 py-1 rounded border border-border text-xs hover:bg-muted"
                    >
                      Open
                    </a>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
