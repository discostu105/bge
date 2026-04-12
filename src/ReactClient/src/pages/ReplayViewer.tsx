import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router'
import { useCallback, useEffect, useRef, useState } from 'react'
import apiClient from '@/api/client'
import type { GameReplayViewModel, ReplayBattleEventViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { rankBadgeClass } from '@/lib/formatters'

const SPEED_OPTIONS: { label: string; ms: number }[] = [
  { label: '0.5x', ms: 4000 },
  { label: '1x', ms: 2000 },
  { label: '2x', ms: 1000 },
  { label: '4x', ms: 500 },
]

function outcomeBadgeClass(outcome: string, isAttacker: boolean, isDefender: boolean): string {
  if (!isAttacker && !isDefender) return 'bg-muted text-muted-foreground'
  const won =
    (isAttacker && outcome === 'AttackerWon') || (isDefender && outcome === 'DefenderWon')
  if (won) return 'bg-success/20 text-success-foreground'
  if (outcome === 'Draw') return 'bg-warning/20 text-warning-foreground'
  return 'bg-destructive/20 text-destructive-foreground'
}

function outcomeLabel(
  outcome: string,
  isAttacker: boolean,
  isDefender: boolean
): string {
  if (!isAttacker && !isDefender) return outcome
  const won =
    (isAttacker && outcome === 'AttackerWon') || (isDefender && outcome === 'DefenderWon')
  if (outcome === 'Draw') return 'Draw'
  return won ? 'Win' : 'Loss'
}

function EventCard({ event }: { event: ReplayBattleEventViewModel }) {
  const { isCurrentPlayerAttacker: isAttacker, isCurrentPlayerDefender: isDefender } = event
  const badgeClass = outcomeBadgeClass(event.outcome, isAttacker, isDefender)
  const label = outcomeLabel(event.outcome, isAttacker, isDefender)

  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-2 text-sm">
          <span className={`font-semibold ${isAttacker ? 'text-primary' : ''}`}>
            {event.attackerName}
          </span>
          <span className="text-muted-foreground">⚔️</span>
          <span className={`font-semibold ${isDefender ? 'text-primary' : ''}`}>
            {event.defenderName}
          </span>
        </div>
        <span className={`text-xs px-2 py-0.5 rounded font-bold ${badgeClass}`}>{label}</span>
      </div>
      <div className="text-xs text-muted-foreground mt-2">
        {new Date(event.occurredAt).toLocaleString(undefined, {
          day: 'numeric',
          month: 'short',
          hour: '2-digit',
          minute: '2-digit',
        })}
      </div>
    </div>
  )
}

export function ReplayViewer() {
  const { gameId } = useParams<{ gameId: string }>()
  const { data, isLoading, error, refetch } = useQuery<GameReplayViewModel>({
    queryKey: ['replay', gameId],
    queryFn: () => apiClient.get(`/api/replay/${gameId}`).then((r) => r.data),
    enabled: !!gameId,
  })

  const [currentIndex, setCurrentIndex] = useState(0)
  const [isPlaying, setIsPlaying] = useState(false)
  const [speedIndex, setSpeedIndex] = useState(1) // default 1x
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  const events: ReplayBattleEventViewModel[] = data?.battleEvents ?? []
  const total = events.length

  const stopPlayback = useCallback(() => {
    if (intervalRef.current !== null) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
    setIsPlaying(false)
  }, [])

  useEffect(() => {
    if (!isPlaying || total === 0) return
    intervalRef.current = setInterval(() => {
      setCurrentIndex((prev) => {
        if (prev >= total - 1) {
          stopPlayback()
          return prev
        }
        return prev + 1
      })
    }, SPEED_OPTIONS[speedIndex].ms)
    return () => {
      if (intervalRef.current !== null) clearInterval(intervalRef.current)
    }
  }, [isPlaying, speedIndex, total, stopPlayback])

  const handlePlay = () => {
    if (currentIndex >= total - 1) setCurrentIndex(0)
    setIsPlaying(true)
  }

  const handlePause = () => stopPlayback()

  const handlePrev = () => {
    stopPlayback()
    setCurrentIndex((prev) => Math.max(0, prev - 1))
  }

  const handleNext = () => {
    stopPlayback()
    setCurrentIndex((prev) => Math.min(total - 1, prev + 1))
  }

  if (isLoading) return <PageLoader message="Loading replay..." />
  if (error) return <ApiError message="Failed to load replay." onRetry={() => void refetch()} />
  if (!data) return null

  const currentEvent = total > 0 ? events[currentIndex] : null

  return (
    <div className="p-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-3 mb-6 flex-wrap">
        <h1 className="text-2xl font-bold">{data.gameName}</h1>
        <span
          className={`text-xs px-2 py-0.5 rounded font-bold ${
            data.status === 'Finished'
              ? 'bg-muted text-muted-foreground'
              : 'bg-success/20 text-success-foreground'
          }`}
        >
          {data.status}
        </span>
        <span className="text-sm text-muted-foreground">
          {new Date(data.startTime).toLocaleDateString(undefined, {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
          })}
          {data.actualEndTime &&
            ` – ${new Date(data.actualEndTime).toLocaleDateString(undefined, {
              day: 'numeric',
              month: 'short',
              year: 'numeric',
            })}`}
        </span>
        <span className="text-sm text-muted-foreground">
          · {data.finalStandings.length} player{data.finalStandings.length !== 1 ? 's' : ''}
        </span>
      </div>

      {/* Final Standings */}
      {data.finalStandings.length > 0 && (
        <div className="rounded-lg border border-border overflow-hidden mb-6">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th scope="col" className="px-3 py-2 text-left font-medium">Rank</th>
                <th scope="col" className="px-3 py-2 text-left font-medium">Player</th>
                <th scope="col" className="px-3 py-2 text-left font-medium hidden sm:table-cell">Race</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">Land</th>
              </tr>
            </thead>
            <tbody>
              {data.finalStandings.map((p) => {
                const isMe = currentEvent != null
                  ? currentEvent.isCurrentPlayerAttacker
                    ? currentEvent.attackerName === p.playerName
                    : currentEvent.defenderName === p.playerName
                  : false
                return (
                  <tr
                    key={p.playerId}
                    className={`border-b border-border/50 transition-colors ${
                      isMe ? 'bg-primary/10' : 'hover:bg-muted/30'
                    }`}
                  >
                    <td className="px-3 py-2">
                      <span
                        className={`text-xs px-2 py-0.5 rounded font-bold ${rankBadgeClass(
                          p.finalRank,
                          data.finalStandings.length
                        )}`}
                      >
                        #{p.finalRank}
                      </span>
                    </td>
                    <td className="px-3 py-2 font-medium">{p.playerName}</td>
                    <td className="px-3 py-2 text-muted-foreground hidden sm:table-cell">{p.race}</td>
                    <td className="px-3 py-2 text-right font-mono">
                      {Number(p.finalLand).toLocaleString()}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Battle Timeline */}
      {total === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center border border-border rounded-lg">
          <div className="text-4xl mb-3 opacity-30">⚔️</div>
          <p className="text-muted-foreground">No battles recorded for this game.</p>
        </div>
      ) : (
        <>
          {/* Scrub bar */}
          <div className="mb-4">
            <input
              type="range"
              min={0}
              max={total - 1}
              value={currentIndex}
              onChange={(e) => {
                stopPlayback()
                setCurrentIndex(Number(e.target.value))
              }}
              className="w-full accent-primary"
              aria-label="Battle timeline"
            />
            <div className="flex justify-between text-xs text-muted-foreground mt-1">
              <span>{total > 0 ? new Date(events[0].occurredAt).toLocaleDateString() : ''}</span>
              <span>Event {currentIndex + 1} of {total}</span>
              <span>{total > 0 ? new Date(events[total - 1].occurredAt).toLocaleDateString() : ''}</span>
            </div>
          </div>

          {/* Current event card */}
          {currentEvent && <EventCard event={currentEvent} />}

          {/* Playback controls */}
          <div className="flex items-center gap-3 mt-4 flex-wrap">
            <button
              onClick={() => { stopPlayback(); setCurrentIndex(0) }}
              className="text-xs px-2 py-1 rounded border border-border hover:bg-muted/50 transition-colors"
              title="Jump to first"
            >
              ⏮
            </button>
            <button
              onClick={handlePrev}
              className="text-xs px-2 py-1 rounded border border-border hover:bg-muted/50 transition-colors"
              title="Previous"
            >
              ◀
            </button>
            {isPlaying ? (
              <button
                onClick={handlePause}
                className="text-sm px-3 py-1 rounded bg-primary text-primary-foreground hover:bg-primary/90 transition-colors"
              >
                ⏸ Pause
              </button>
            ) : (
              <button
                onClick={handlePlay}
                className="text-sm px-3 py-1 rounded bg-primary text-primary-foreground hover:bg-primary/90 transition-colors"
                disabled={total === 0}
              >
                ▶ Play
              </button>
            )}
            <button
              onClick={handleNext}
              className="text-xs px-2 py-1 rounded border border-border hover:bg-muted/50 transition-colors"
              title="Next"
            >
              ▶
            </button>
            <button
              onClick={() => { stopPlayback(); setCurrentIndex(total - 1) }}
              className="text-xs px-2 py-1 rounded border border-border hover:bg-muted/50 transition-colors"
              title="Jump to last"
            >
              ⏭
            </button>

            <div className="flex items-center gap-1 ml-auto">
              <span className="text-xs text-muted-foreground">Speed:</span>
              {SPEED_OPTIONS.map((opt, i) => (
                <button
                  key={opt.label}
                  onClick={() => setSpeedIndex(i)}
                  className={`text-xs px-2 py-0.5 rounded transition-colors ${
                    speedIndex === i
                      ? 'bg-primary text-primary-foreground'
                      : 'border border-border hover:bg-muted/50'
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
