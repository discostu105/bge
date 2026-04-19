import { useEffect, useCallback, useMemo, useState } from 'react'
import { useParams, Link, useNavigate, Navigate } from 'react-router'
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameLobbyViewModel, JoinGameRequest, RaceListViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useSignalR } from '@/hooks/useSignalR'
import { GameCountdown } from '@/components/games/GameCountdown'
import { RaceTile } from '@/components/games/RaceTile'
import { normalizeRace, RACES, type Race } from '@/lib/race'
import { cn } from '@/lib/utils'

function StatusBadge({ status }: { status: string }) {
  const s = status.toLowerCase()
  const cls = s === 'active'
    ? 'text-resource border-[color:var(--color-resource)] bg-[color:var(--color-resource)]/15'
    : s === 'upcoming'
    ? 'text-prestige border-[color:var(--color-prestige)] bg-[color:var(--color-prestige)]/15'
    : 'text-muted-foreground border-muted-foreground bg-muted/10'
  const label = s === 'active' ? 'Live' : s === 'upcoming' ? 'Upcoming' : 'Finished'
  return (
    <span className={cn('inline-block text-[9.5px] uppercase tracking-[0.12em] px-1.5 py-0.5 border-l-2 font-bold ml-3 align-middle', cls)}>
      {label}
    </span>
  )
}

function SectionHeader({ label }: { label: string }) {
  return <h2 className="label mb-2.5 mt-5 pb-1.5 border-b border-border">{label}</h2>
}

function Stat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className="label text-[9.5px]">{label}</div>
      <div className="mono text-[16px] font-bold mt-0.5">{value}</div>
    </div>
  )
}

function PlayerList({ players, currentPlayerId }: { players: GameLobbyViewModel['players']; currentPlayerId: string | null }) {
  const raceClass: Record<string, string> = {
    terran: 'text-[#CBD5E1]',
    zerg: 'text-[#B45309]',
    protoss: 'text-[#FBBF24]',
  }
  if (players.length === 0) return <p className="text-[11.5px] text-muted-foreground">No commanders have joined yet.</p>
  return (
    <div className="mono text-[11.5px] leading-relaxed">
      {players.map((p) => {
        const race = normalizeRace(p.playerType)
        const isMe = p.playerId === currentPlayerId
        return (
          <div key={p.playerId}>
            <span className={cn(race && raceClass[race], isMe && 'font-bold text-resource')}>{p.playerName}</span>
            <span className="text-muted-foreground text-[10.5px] ml-2.5">
              {new Date(p.joined).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} · {p.playerType}
              {isMe ? ' · you' : ''}
            </span>
          </div>
        )
      })}
    </div>
  )
}

export function GameBriefing() {
  const { gameId } = useParams<{ gameId: string }>()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { on, off } = useSignalR('/gamehub')

  const { data: lobby, isLoading, error, refetch } = useQuery<GameLobbyViewModel>({
    queryKey: ['game-lobby', gameId],
    queryFn: () => apiClient.get(`/api/games/${gameId}/lobby`).then((r) => r.data),
    enabled: !!gameId,
    refetchInterval: 15_000,
  })

  const { data: racesData } = useQuery<RaceListViewModel>({
    queryKey: ['races'],
    queryFn: () => apiClient.get('/api/games/races').then((r) => r.data),
    staleTime: Infinity,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['game-lobby', gameId] })
  }, [queryClient, gameId])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  const [selectedRace, setSelectedRace] = useState<Race>('terran')
  const [playerName, setPlayerName] = useState('')
  const [joinError, setJoinError] = useState<string | null>(null)

  const joinMutation = useMutation({
    mutationFn: (req: JoinGameRequest) => apiClient.post(`/api/games/${gameId}/join`, req),
    onSuccess: () => {
      setJoinError(null)
      void queryClient.invalidateQueries({ queryKey: ['game-lobby', gameId] })
      if (lobby?.status.toLowerCase() === 'active') {
        void navigate(`/games/${gameId}/base`)
      }
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to join.'
      setJoinError(String(msg))
    },
  })

  const racesForUi = useMemo(
    () => (racesData?.races.map((r) => normalizeRace(r.id)).filter((r): r is Race => !!r) ?? RACES),
    [racesData],
  )

  if (isLoading) return <PageLoader message="Loading briefing..." />
  if (error) return <ApiError message="Failed to load game." onRetry={() => void refetch()} />
  if (!lobby || !gameId) return <ApiError message="Game not found." />

  if (lobby.status.toLowerCase() === 'finished') {
    return <Navigate to={`/games/${gameId}/results`} replace />
  }

  const status = lobby.status.toLowerCase() as 'active' | 'upcoming'
  const isEnrolled = lobby.isCurrentPlayerEnrolled

  const enrollmentLine = isEnrolled ? (
    <>
      You are {status === 'active' ? 'enrolled' : 'registered'} ·{' '}
      <span style={{ color: `var(--color-race-primary)` }}>{lobby.currentPlayerRace ?? '—'}</span>
      {lobby.currentPlayerName && <> · {lobby.currentPlayerName}</>}
      {status === 'active' && lobby.currentPlayerRank != null && <> · Round rank #{lobby.currentPlayerRank}</>}
    </>
  ) : null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!playerName.trim()) {
      setJoinError('Please enter a commander name.')
      return
    }
    setJoinError(null)
    joinMutation.mutate({ playerName: playerName.trim(), playerType: selectedRace })
  }

  return (
    <div className="max-w-4xl">
      <Link to="/games" className="text-[11.5px] text-interactive hover:underline">← All Games</Link>

      <h1 className="text-[22px] font-bold tracking-tight mt-2">
        {lobby.gameName}<StatusBadge status={lobby.status} />
      </h1>
      {enrollmentLine && (
        <p className="text-[11.5px] text-muted-foreground mt-1">{enrollmentLine}</p>
      )}

      {status === 'upcoming' && lobby.startTime && (
        <>
          <SectionHeader label="Starts in" />
          <div>
            <GameCountdown to={lobby.startTime} />
            <span className="text-[11.5px] text-muted-foreground ml-2">
              {new Date(lobby.startTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
        </>
      )}

      {status === 'active' && lobby.endTime && (
        <>
          <SectionHeader label="Ends in" />
          <div>
            <GameCountdown to={lobby.endTime} />
            <span className="text-[11.5px] text-muted-foreground ml-2">
              {new Date(lobby.endTime).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
        </>
      )}

      {lobby.settings && (
        <>
          <SectionHeader label="Rules" />
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-5">
            <Stat label="Starting resources" value={`${lobby.settings.startingMinerals.toLocaleString()}m / ${lobby.settings.startingGas.toLocaleString()}g`} />
            <Stat label="Starting land" value={lobby.settings.startingLand} />
            <Stat label="End tick" value={lobby.settings.endTick.toLocaleString()} />
            <Stat label="Protection" value={`${lobby.settings.protectionTicks} ticks`} />
          </div>
        </>
      )}

      {status === 'active' && isEnrolled && (
        <div className="flex gap-3 mt-6">
          <Link
            to={`/games/${gameId}/base`}
            className="border border-[color:var(--color-resource)] text-resource px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-resource)]/10"
          >
            Enter game →
          </Link>
          <Link
            to={`/games/${gameId}/live`}
            className="border border-interactive text-interactive px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-interactive)]/10"
          >
            Live scoreboard
          </Link>
        </div>
      )}

      {status === 'active' && !isEnrolled && lobby.canJoin && (
        <>
          <SectionHeader label="Join this game" />
          <RegisterForm
            races={racesForUi}
            selectedRace={selectedRace}
            onRaceChange={setSelectedRace}
            playerName={playerName}
            onNameChange={setPlayerName}
            submitLabel={`Join as ${selectedRace}`}
            onSubmit={handleSubmit}
            busy={joinMutation.isPending}
            error={joinError}
          />
          <div className="mt-4">
            <Link
              to={`/games/${gameId}/live`}
              className="border border-interactive text-interactive px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-interactive)]/10"
            >
              Or spectate live →
            </Link>
          </div>
        </>
      )}

      {status === 'upcoming' && !isEnrolled && lobby.canJoin && (
        <>
          <SectionHeader label="Register — choose your race" />
          <RegisterForm
            races={racesForUi}
            selectedRace={selectedRace}
            onRaceChange={setSelectedRace}
            playerName={playerName}
            onNameChange={setPlayerName}
            submitLabel={`Register as ${selectedRace}`}
            onSubmit={handleSubmit}
            busy={joinMutation.isPending}
            error={joinError}
          />
        </>
      )}

      {status === 'upcoming' && isEnrolled && (
        <p className="text-[12.5px] text-muted-foreground mt-5">
          You're in. The game hasn't started — you'll be able to enter when the clock hits 0.
        </p>
      )}

      <SectionHeader label={`${status === 'upcoming' ? 'Registered commanders' : 'Commanders'} — ${lobby.players.length}${lobby.maxPlayers > 0 ? ` / ${lobby.maxPlayers}` : ''}`} />
      <PlayerList players={lobby.players} currentPlayerId={lobby.currentPlayerId} />
    </div>
  )
}

function RegisterForm({
  races, selectedRace, onRaceChange, playerName, onNameChange, submitLabel, onSubmit, busy, error,
}: {
  races: Race[]
  selectedRace: Race
  onRaceChange: (r: Race) => void
  playerName: string
  onNameChange: (v: string) => void
  submitLabel: string
  onSubmit: (e: React.FormEvent) => void
  busy: boolean
  error: string | null
}) {
  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="flex gap-3">
        {races.map((r) => (
          <RaceTile key={r} race={r} selected={selectedRace === r} onSelect={onRaceChange} />
        ))}
      </div>
      {error && (
        <div role="alert" className="border-l-2 border-[color:var(--color-danger)] bg-[color:var(--color-danger)]/10 px-3 py-2 text-[12px] text-[color:var(--color-danger)]">
          {error}
        </div>
      )}
      <div className="flex gap-2">
        <input
          type="text"
          value={playerName}
          onChange={(e) => onNameChange(e.target.value)}
          placeholder="Commander name"
          maxLength={32}
          className="flex-1 bg-input border border-border px-3 py-2 text-[13px] focus:outline-none focus:border-[color:var(--color-prestige)]"
        />
        <button
          type="submit"
          disabled={busy}
          className="border border-[color:var(--color-prestige)] text-prestige px-4 py-2 text-[12.5px] font-semibold uppercase tracking-[0.12em] hover:bg-[color:var(--color-prestige)]/10 disabled:opacity-50"
        >
          {busy ? 'Submitting…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
