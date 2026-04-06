import { Link, useParams } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { TournamentDetailViewModel, MatchViewModel, RoundViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function TournamentDetail() {
  const { tournamentId } = useParams<{ tournamentId: string }>()
  const queryClient = useQueryClient()

  const { data: tournament, error, isLoading, refetch } = useQuery<TournamentDetailViewModel>({
    queryKey: ['tournament', tournamentId],
    queryFn: () => apiClient.get(`/api/tournaments/${tournamentId}`).then((r) => r.data),
    enabled: !!tournamentId,
  })

  const registerMutation = useMutation({
    mutationFn: () => apiClient.post(`/api/tournaments/${tournamentId}/register`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['tournament', tournamentId] }),
  })

  const unregisterMutation = useMutation({
    mutationFn: () => apiClient.delete(`/api/tournaments/${tournamentId}/register`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['tournament', tournamentId] }),
  })

  const startMutation = useMutation({
    mutationFn: () => apiClient.post(`/api/tournaments/${tournamentId}/start`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['tournament', tournamentId] }),
  })

  if (isLoading) return <PageLoader message="Loading tournament..." />
  if (error) return <ApiError message="Failed to load tournament." onRetry={() => void refetch()} />
  if (!tournament) return null

  const isRegistration = tournament.status === 'Registration'
  const isInProgress = tournament.status === 'InProgress'
  const isFinished = tournament.status === 'Finished'
  const deadlinePassed = new Date(tournament.registrationDeadline) < new Date()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">{tournament.name}</h1>
          <p className="text-muted-foreground text-sm mt-1">
            {tournament.format} · <StatusBadge status={tournament.status} />
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {isRegistration && !deadlinePassed && (
            tournament.isRegistered ? (
              <button
                className="px-4 py-2 text-sm rounded border border-border hover:bg-muted disabled:opacity-50"
                onClick={() => unregisterMutation.mutate()}
                disabled={unregisterMutation.isPending}
              >
                {unregisterMutation.isPending ? 'Withdrawing...' : 'Withdraw'}
              </button>
            ) : (
              <button
                className="px-4 py-2 text-sm rounded bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                onClick={() => registerMutation.mutate()}
                disabled={registerMutation.isPending}
              >
                {registerMutation.isPending ? 'Registering...' : 'Register'}
              </button>
            )
          )}

          {isRegistration && tournament.isCreator && tournament.registrations.length >= 2 && (
            <button
              className="px-4 py-2 text-sm rounded bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
              onClick={() => startMutation.mutate()}
              disabled={startMutation.isPending}
            >
              {startMutation.isPending ? 'Starting...' : 'Start Tournament'}
            </button>
          )}
        </div>
      </div>

      {registerMutation.error && (
        <p className="text-destructive text-sm">{String(registerMutation.error)}</p>
      )}
      {startMutation.error && (
        <p className="text-destructive text-sm">{String(startMutation.error)}</p>
      )}

      {/* Details row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
        <div>
          <p className="text-muted-foreground text-xs">Deadline</p>
          <p className="font-medium">{new Date(tournament.registrationDeadline).toLocaleString()}</p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs">Players</p>
          <p className="font-medium">
            {tournament.registrations.length}
            {tournament.maxPlayers > 0 ? ` / ${tournament.maxPlayers}` : ''}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs">Format</p>
          <p className="font-medium">{tournament.format}</p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs">Results</p>
          <p className="font-medium">
            <Link to={`/tournaments/${tournament.tournamentId}/results`} className="text-primary hover:underline">
              View standings
            </Link>
          </p>
        </div>
      </div>

      {/* Bracket or registrations */}
      {(isInProgress || isFinished) && tournament.bracket ? (
        <BracketView bracket={tournament.bracket} format={tournament.format} />
      ) : (
        <RegistrationsList registrations={tournament.registrations} />
      )}
    </div>
  )
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Registration: 'bg-blue-100 text-blue-800',
    InProgress: 'bg-green-100 text-green-800',
    Finished: 'bg-gray-100 text-gray-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${colors[status] ?? 'bg-muted'}`}>
      {status}
    </span>
  )
}

function RegistrationsList({ registrations }: { registrations: TournamentDetailViewModel['registrations'] }) {
  return (
    <div>
      <h2 className="text-lg font-semibold mb-3">
        Registrations ({registrations.length})
      </h2>
      {registrations.length === 0 ? (
        <p className="text-muted-foreground text-sm">No players registered yet.</p>
      ) : (
        <ul className="space-y-1">
          {registrations.map((r) => (
            <li key={r.userId} className="text-sm flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-primary inline-block" />
              {r.displayName}
              <span className="text-muted-foreground text-xs">
                {new Date(r.registeredAt).toLocaleDateString()}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function BracketView({ bracket, format }: { bracket: NonNullable<TournamentDetailViewModel['bracket']>, format: string }) {
  if (format === 'RoundRobin') {
    return <RoundRobinBracket rounds={bracket.rounds} />
  }
  return <EliminationBracket rounds={bracket.rounds} />
}

function RoundRobinBracket({ rounds }: { rounds: RoundViewModel[] }) {
  const allMatches = rounds.flatMap((r) => r.matches)

  return (
    <div>
      <h2 className="text-lg font-semibold mb-3">Match Schedule</h2>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-muted-foreground">
              <th className="pb-2 pr-4">Match</th>
              <th className="pb-2 pr-4">Player 1</th>
              <th className="pb-2 pr-4">Player 2</th>
              <th className="pb-2 pr-4">Winner</th>
              <th className="pb-2">Game</th>
            </tr>
          </thead>
          <tbody>
            {allMatches.map((m) => (
              <MatchRow key={m.matchId} match={m} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function EliminationBracket({ rounds }: { rounds: RoundViewModel[] }) {
  return (
    <div>
      <h2 className="text-lg font-semibold mb-3">Bracket</h2>
      <div className="flex gap-6 overflow-x-auto pb-4">
        {rounds.map((round) => (
          <div key={round.round} className="flex flex-col gap-4 min-w-[200px]">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
              {round.round === rounds.length ? 'Final' : `Round ${round.round}`}
            </p>
            {round.matches.map((match) => (
              <MatchCard key={match.matchId} match={match} />
            ))}
          </div>
        ))}
      </div>
    </div>
  )
}

function MatchCard({ match }: { match: MatchViewModel }) {
  const isBye = match.status === 'Bye'
  const isCompleted = match.status === 'Completed'

  return (
    <div className={`border border-border rounded-lg p-3 text-sm space-y-1 ${isCompleted ? 'bg-muted/30' : 'bg-card'}`}>
      {isBye ? (
        <p className="text-muted-foreground italic">Bye</p>
      ) : (
        <>
          <PlayerLine
            name={match.player1?.displayName ?? '—'}
            isWinner={match.winnerId === match.player1?.userId}
          />
          <div className="border-t border-border/50" />
          <PlayerLine
            name={match.player2?.displayName ?? 'TBD'}
            isWinner={match.winnerId === match.player2?.userId}
          />
        </>
      )}
      {match.gameId && (
        <div className="pt-1">
          <Link
            to={`/games/${match.gameId}`}
            className="text-xs text-primary hover:underline"
          >
            View game →
          </Link>
        </div>
      )}
    </div>
  )
}

function PlayerLine({ name, isWinner }: { name: string; isWinner: boolean }) {
  return (
    <p className={`truncate ${isWinner ? 'font-bold text-green-600' : 'text-foreground'}`}>
      {isWinner && '🏆 '}
      {name}
    </p>
  )
}

function MatchRow({ match }: { match: MatchViewModel }) {
  return (
    <tr className="border-b border-border">
      <td className="py-2 pr-4 text-muted-foreground">{match.matchNumber}</td>
      <td className={`py-2 pr-4 ${match.winnerId === match.player1?.userId ? 'font-bold text-green-600' : ''}`}>
        {match.player1?.displayName ?? '—'}
      </td>
      <td className={`py-2 pr-4 ${match.winnerId === match.player2?.userId ? 'font-bold text-green-600' : ''}`}>
        {match.player2?.displayName ?? '—'}
      </td>
      <td className="py-2 pr-4 text-muted-foreground">
        {match.winnerId
          ? (match.player1?.userId === match.winnerId ? match.player1?.displayName : match.player2?.displayName)
          : match.status}
      </td>
      <td className="py-2">
        {match.gameId && (
          <Link to={`/games/${match.gameId}`} className="text-primary hover:underline text-xs">
            Game →
          </Link>
        )}
      </td>
    </tr>
  )
}
