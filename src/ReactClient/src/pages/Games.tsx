import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel } from '@/api/types'

function vcBadge(type: string | null | undefined): { css: string; text: string } {
  switch (type) {
    case 'EconomicThreshold': return { css: 'bg-yellow-500 text-black', text: 'CONQUERED' }
    case 'TimeExpired': return { css: 'bg-gray-500 text-white', text: 'TIME EXPIRED' }
    case 'AdminFinalized': return { css: 'bg-red-600 text-white', text: 'ADMIN ENDED' }
    default: return { css: 'bg-gray-500 text-white', text: 'FINISHED' }
  }
}

function GameCard({ game, onJoin }: { game: GameSummaryViewModel; onJoin: (game: GameSummaryViewModel) => void }) {
  const formatDate = (d: string | null | undefined) =>
    d ? new Date(d).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : 'TBD'

  if (game.status === 'Active') {
    return (
      <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
        <div className="flex justify-between items-start">
          <h3 className="font-semibold">{game.name}</h3>
          <span className="text-xs font-bold px-2 py-0.5 rounded bg-green-700 text-white">LIVE</span>
        </div>
        <div className="text-sm text-muted-foreground">
          <div>Players: {game.playerCount}{game.maxPlayers > 0 ? ` / ${game.maxPlayers}` : ''}</div>
          <div>Ends: {formatDate(game.endTime)}</div>
        </div>
        <div className="flex gap-2 mt-auto flex-wrap">
          {game.canJoin && (
            <button
              className="text-sm px-3 py-1 rounded bg-green-700 text-white hover:bg-green-600"
              onClick={() => onJoin(game)}
            >
              Join
            </button>
          )}
          {game.isPlayerEnrolled && (
            <a
              href={`/games/${game.gameId}/base`}
              className="text-sm px-3 py-1 rounded bg-blue-700 text-white hover:bg-blue-600"
            >
              Play Now
            </a>
          )}
          <a
            href={`/games/${game.gameId}/ranking`}
            className="text-sm px-3 py-1 rounded border border-border hover:bg-muted"
          >
            Spectate
          </a>
        </div>
      </div>
    )
  }

  if (game.status === 'Upcoming') {
    return (
      <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
        <div className="flex justify-between items-start">
          <h3 className="font-semibold">{game.name}</h3>
          <span className="text-xs font-bold px-2 py-0.5 rounded bg-yellow-500 text-black">UPCOMING</span>
        </div>
        <div className="text-sm text-muted-foreground">
          <div>Players: {game.playerCount}{game.maxPlayers > 0 ? ` / ${game.maxPlayers}` : ''}</div>
          <div>Starts: {formatDate(game.startTime)}</div>
        </div>
        <div className="mt-auto">
          {game.canJoin ? (
            <button
              className="text-sm px-3 py-1 rounded bg-green-700 text-white hover:bg-green-600"
              onClick={() => onJoin(game)}
            >
              Register Now
            </button>
          ) : game.maxPlayers > 0 && game.playerCount >= game.maxPlayers ? (
            <span className="text-xs px-2 py-0.5 rounded bg-red-700 text-white">Full</span>
          ) : (
            <span className="text-xs px-2 py-0.5 rounded bg-gray-600 text-white">Registered</span>
          )}
        </div>
      </div>
    )
  }

  // Finished
  const badge = vcBadge(game.victoryConditionType)
  return (
    <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
      <div className="flex justify-between items-start">
        <h3 className="font-semibold">{game.name}</h3>
        <span className={`text-xs font-bold px-2 py-0.5 rounded ${badge.css}`}>{badge.text}</span>
      </div>
      <div className="text-sm text-muted-foreground">
        {game.winnerName && game.victoryConditionType === 'EconomicThreshold' && (
          <div>🏅 Conqueror: <strong>{game.winnerName}</strong></div>
        )}
        {game.winnerName && game.victoryConditionType !== 'EconomicThreshold' && (
          <div>Winner: <strong>{game.winnerName}</strong></div>
        )}
        <div>Ended: {formatDate(game.endTime)}</div>
      </div>
      <div className="mt-auto">
        <a
          href={`/games/${game.gameId}/results`}
          className="text-sm px-3 py-1 rounded border border-border hover:bg-muted"
        >
          View Results
        </a>
      </div>
    </div>
  )
}

export function Games() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const isNewPlayer = searchParams.get('welcome') === '1'
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [showAllFinished, setShowAllFinished] = useState(false)

  const { data } = useQuery<GameListViewModel>({
    queryKey: ['games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const joinMutation = useMutation({
    mutationFn: (game: GameSummaryViewModel) =>
      apiClient.post(`/api/games/${game.gameId}/players`, {}),
    onSuccess: (_, game) => {
      setError(null)
      setSuccess(`You have joined "${game.name}"!`)
      void queryClient.invalidateQueries({ queryKey: ['games'] })
      void navigate(`/games/${game.gameId}/base`)
    },
    onError: async (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to join game.'
      setError(String(msg))
    },
  })

  const games = data?.games ?? []
  const active = games.filter((g) => g.status === 'Active')
  const upcoming = games.filter((g) => g.status === 'Upcoming')
  const finished = games.filter((g) => g.status === 'Finished')
  const visibleFinished = showAllFinished || finished.length <= 5 ? finished : finished.slice(0, 5)

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Season Schedule</h1>

      {isNewPlayer && (
        <div className="rounded-lg border border-green-700 bg-green-900/20 p-4 text-sm">
          <strong>Welcome to BGE!</strong> Your commander is ready.{' '}
          <span className="text-muted-foreground">
            Join an active game below, or register for an upcoming season.
          </span>
        </div>
      )}

      {success && <div className="rounded-lg border border-green-700 bg-green-900/20 p-3 text-sm">{success}</div>}
      {error && <div className="rounded-lg border border-red-700 bg-red-900/20 p-3 text-sm text-red-400">{error}</div>}

      {!data && (
        <div className="text-muted-foreground text-sm">Loading...</div>
      )}

      {data && (
        <>
          <section>
            <h2 className="text-lg font-semibold mb-3">Active</h2>
            {active.length === 0 ? (
              <p className="text-muted-foreground text-sm">
                No games are currently running.
                {upcoming.length === 0 && ' New seasons are started by the game administrator. Check back soon.'}
              </p>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {active.map((g) => (
                  <GameCard key={g.gameId} game={g} onJoin={(game) => joinMutation.mutate(game)} />
                ))}
              </div>
            )}
          </section>

          <section>
            <h2 className="text-lg font-semibold mb-3">Upcoming</h2>
            {upcoming.length === 0 ? (
              <p className="text-muted-foreground text-sm">
                No upcoming games are scheduled yet. New seasons are announced here — check back soon.
              </p>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {upcoming.map((g) => (
                  <GameCard key={g.gameId} game={g} onJoin={(game) => joinMutation.mutate(game)} />
                ))}
              </div>
            )}
          </section>

          {finished.length > 0 && (
            <section>
              <div className="flex items-center gap-3 mb-3">
                <h2 className="text-lg font-semibold">Finished</h2>
                {finished.length > 5 && (
                  <button
                    className="text-sm text-blue-400 hover:underline"
                    onClick={() => setShowAllFinished((v) => !v)}
                  >
                    {showAllFinished ? '▲ Collapse' : `▼ Show all (${finished.length})`}
                  </button>
                )}
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {visibleFinished.map((g) => (
                  <GameCard key={g.gameId} game={g} onJoin={(game) => joinMutation.mutate(game)} />
                ))}
              </div>
            </section>
          )}
        </>
      )}
    </div>
  )
}
