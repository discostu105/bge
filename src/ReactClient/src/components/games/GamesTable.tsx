import type { ReactNode } from 'react'
import { Link } from 'react-router'
import type { GameSummaryViewModel } from '@/api/types'
import { GameCountdown } from './GameCountdown'
import { cn } from '@/lib/utils'

type Section = 'active' | 'upcoming' | 'finished'

interface GamesTableProps {
  section: Section
  games: GameSummaryViewModel[]
}

const BADGE_CLASSES: Record<Section, string> = {
  active: 'text-resource border-l-2 border-resource bg-resource/15',
  upcoming: 'text-prestige border-l-2 border-prestige bg-prestige/15',
  finished: 'text-muted-foreground border-l-2 border-muted-foreground bg-muted/10',
}

const BADGE_LABEL: Record<Section, string> = {
  active: 'Live',
  upcoming: 'Upcoming',
  finished: 'Finished',
}

function StatusBadge({ section }: { section: Section }) {
  return (
    <span className={cn('inline-block text-[9.5px] uppercase tracking-[0.12em] px-1.5 py-0.5 font-bold', BADGE_CLASSES[section])}>
      {BADGE_LABEL[section]}
    </span>
  )
}

function NameCell({ game, section }: { game: GameSummaryViewModel; section: Section }) {
  const subline: ReactNode = section === 'active' && game.isPlayerEnrolled
    ? <>{game.gameDefType.toUpperCase()} · <span className="text-resource">you are enrolled</span></>
    : section === 'upcoming' && game.isPlayerEnrolled
    ? <>{game.gameDefType.toUpperCase()} · <span className="text-resource">you are registered</span></>
    : game.gameDefType.toUpperCase()
  return (
    <div>
      <div className="font-semibold text-foreground">{game.name}</div>
      <div className="text-[10.5px] text-muted-foreground font-normal mt-0.5">{subline}</div>
    </div>
  )
}

function Actions({ game, section }: { game: GameSummaryViewModel; section: Section }) {
  if (section === 'active') {
    return (
      <>
        {game.isPlayerEnrolled ? (
          <Link to={`/games/${game.gameId}/base`} className="text-[11.5px] font-semibold text-resource hover:underline ml-3">Enter →</Link>
        ) : game.canJoin ? (
          <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-prestige hover:underline ml-3">Join</Link>
        ) : null}
        <Link to={`/games/${game.gameId}/live`} className="text-[11.5px] text-interactive hover:underline ml-3">Spectate</Link>
      </>
    )
  }
  if (section === 'upcoming') {
    if (game.isPlayerEnrolled) {
      return <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-resource hover:underline ml-3">Briefing →</Link>
    }
    return (
      <>
        {game.canJoin && <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] font-semibold text-prestige hover:underline ml-3">Register</Link>}
        <Link to={`/games/${game.gameId}/briefing`} className="text-[11.5px] text-interactive hover:underline ml-3">Details</Link>
      </>
    )
  }
  // finished
  return (
    <>
      <Link to={`/games/${game.gameId}/results`} className="text-[11.5px] text-interactive hover:underline ml-3">Results</Link>
      <Link to={`/replays/${game.gameId}`} className="text-[11.5px] text-interactive hover:underline ml-3">Replay</Link>
    </>
  )
}

function fmtDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function GamesTable({ section, games }: GamesTableProps) {
  if (games.length === 0) return null
  return (
    <div className="overflow-x-auto -mx-3 sm:mx-0">
    <table className="w-full text-[12.5px] border-collapse min-w-[640px]">
      <thead>
        <tr className="text-left [&>th]:border-b [&>th]:border-border">
          <th className="label py-1.5 pr-3 w-[88px] pl-3 sm:pl-0">Status</th>
          <th className="label py-1.5 pr-3">Game</th>
          {section === 'active' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[100px]">Players</th>
              <th className="label py-1.5 pr-3 text-right w-[120px]">Round</th>
              <th className="label py-1.5 pr-3 text-right w-[140px]">Ends</th>
            </>
          )}
          {section === 'upcoming' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[100px]">Registered</th>
              <th className="label py-1.5 pr-3 text-right w-[140px]">Starts in</th>
            </>
          )}
          {section === 'finished' && (
            <>
              <th className="label py-1.5 pr-3 text-right w-[80px]">Players</th>
              <th className="label py-1.5 pr-3">Winner</th>
              <th className="label py-1.5 pr-3 text-right w-[120px]">Ended</th>
            </>
          )}
          <th className="py-1.5 text-right min-w-[200px]" />
        </tr>
      </thead>
      <tbody>
        {games.map((g) => {
          const enrolled = g.isPlayerEnrolled
          // Tables use border-collapse; <tr> borders don't render cross-browser,
          // so rules go on the cells instead. The enrolled-row green left-stripe
          // goes on the first cell only.
          const rowClass = cn(
            '[&>td]:border-b [&>td]:border-border',
            enrolled
              ? 'bg-resource/5 [&>td:first-child]:border-l-2 [&>td:first-child]:border-l-resource'
              : 'hover:bg-white/5',
            section === 'finished' && 'opacity-75 hover:opacity-100',
          )
          return (
            <tr key={g.gameId} className={rowClass}>
              <td className="py-2 pr-3 pl-3 sm:pl-0"><StatusBadge section={section} /></td>
              <td className="py-2 pr-3"><NameCell game={g} section={section} /></td>

              {section === 'active' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}{g.maxPlayers > 0 ? ` / ${g.maxPlayers}` : ''}</td>
                  <td className="py-2 pr-3 mono text-right text-muted-foreground">—</td>
                  <td className="py-2 pr-3 mono text-right">
                    {g.endTime ? <GameCountdown to={g.endTime} className="text-[12.5px] font-semibold" /> : '—'}
                  </td>
                </>
              )}

              {section === 'upcoming' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}{g.maxPlayers > 0 ? ` / ${g.maxPlayers}` : ''}</td>
                  <td className="py-2 pr-3 mono text-right">
                    {g.startTime ? <GameCountdown to={g.startTime} className="text-[12.5px] font-semibold" /> : '—'}
                  </td>
                </>
              )}

              {section === 'finished' && (
                <>
                  <td className="py-2 pr-3 mono text-right">{g.playerCount}</td>
                  <td className="py-2 pr-3">
                    {g.winnerName
                      ? <>◆ <span className="font-semibold">{g.winnerName}</span></>
                      : <span className="text-muted-foreground">—</span>}
                  </td>
                  <td className="py-2 pr-3 mono text-right text-muted-foreground">{fmtDate(g.endTime)}</td>
                </>
              )}

              <td className="py-2 pr-3 sm:pr-0 text-right whitespace-nowrap"><Actions game={g} section={section} /></td>
            </tr>
          )
        })}
      </tbody>
    </table>
    </div>
  )
}
