import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams, Link } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel, GameSummaryViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { vcBadgeCss, vcText } from '@/lib/victory'
import { formatDateTime } from '@/lib/formatters'

function GameCard({ game, onJoin }: { game: GameSummaryViewModel; onJoin: (game: GameSummaryViewModel) => void }) {
  if (game.status === 'Active') {
    return (
      <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
        <div className="flex justify-between items-start">
          <h3 className="font-semibold">{game.name}</h3>
          <span className="text-xs font-bold px-2 py-0.5 rounded bg-success text-success-foreground">LIVE</span>
        </div>
        <div className="text-sm text-muted-foreground">
          <div>Players: {game.playerCount}{game.maxPlayers > 0 ? ` / ${game.maxPlayers}` : ''}</div>
          <div>Ends: {formatDateTime(game.endTime)}</div>
        </div>
        <div className="flex gap-2 mt-auto flex-wrap">
          {game.canJoin && (
            <button
              className="text-sm px-3 py-1 rounded bg-success text-success-foreground hover:opacity-90"
              onClick={() => onJoin(game)}
            >
              Join
            </button>
          )}
          {game.isPlayerEnrolled && (
            <Link
              to={`/games/${game.gameId}/base`}
              className="text-sm px-3 py-1 rounded bg-primary text-primary-foreground hover:opacity-90"
            >
              Play Now
            </Link>
          )}
          <Link
            to={`/games/${game.gameId}/live`}
            className="text-sm px-3 py-1 rounded border border-border hover:bg-muted"
          >
            Spectate
          </Link>
        </div>
      </div>
    )
  }

  if (game.status === 'Upcoming') {
    return (
      <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
        <div className="flex justify-between items-start">
          <h3 className="font-semibold">{game.name}</h3>
          <span className="text-xs font-bold px-2 py-0.5 rounded bg-warning text-warning-foreground">UPCOMING</span>
        </div>
        <div className="text-sm text-muted-foreground">
          <div>Players: {game.playerCount}{game.maxPlayers > 0 ? ` / ${game.maxPlayers}` : ''}</div>
          <div>Starts: {formatDateTime(game.startTime)}</div>
        </div>
        <div className="mt-auto">
          {game.canJoin ? (
            <button
              className="text-sm px-3 py-1 rounded bg-success text-success-foreground hover:opacity-90"
              onClick={() => onJoin(game)}
            >
              Register Now
            </button>
          ) : game.maxPlayers > 0 && game.playerCount >= game.maxPlayers ? (
            <span className="text-xs px-2 py-0.5 rounded bg-danger text-danger-foreground">Full</span>
          ) : (
            <span className="text-xs px-2 py-0.5 rounded bg-muted text-muted-foreground">Registered</span>
          )}
        </div>
      </div>
    )
  }

  // Finished
  return (
    <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-3">
      <div className="flex justify-between items-start">
        <h3 className="font-semibold">{game.name}</h3>
        <span className={`text-xs font-bold px-2 py-0.5 rounded ${vcBadgeCss(game.victoryConditionType)}`}>{vcText(game.victoryConditionType)}</span>
      </div>
      <div className="text-sm text-muted-foreground">
        {game.winnerName && (
          <div>Winner: <strong>{game.winnerName}</strong></div>
        )}
        <div>Ended: {formatDateTime(game.endTime)}</div>
      </div>
      <div className="mt-auto">
        <Link
          to={`/games/${game.gameId}/results`}
          className="text-sm px-3 py-1 rounded border border-border hover:bg-muted"
        >
          View Results
        </Link>
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

  const { data, isLoading: queryLoading, error: queryError, refetch } = useQuery<GameListViewModel>({
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
        <div className="rounded-lg border border-success/50 bg-success/10 p-4 text-sm">
          <strong>Welcome to BGE!</strong> Your commander is ready.{' '}
          <span className="text-muted-foreground">
            Join an active game below, or register for an upcoming season.
          </span>
        </div>
      )}

      {success && <div className="rounded-lg border border-success/50 bg-success/10 p-3 text-sm">{success}</div>}
      {error && <div className="rounded-lg border border-danger/50 bg-danger/10 p-3 text-sm text-danger-foreground">{error}</div>}

      {queryLoading && <PageLoader message="Loading games..." />}
      {queryError && !data && <ApiError message="Failed to load games." onRetry={() => void refetch()} />}

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
                    className="text-sm text-primary hover:underline"
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
