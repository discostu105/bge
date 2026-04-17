import { NavLink } from 'react-router'
import { Trophy, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { GameDetailViewModel } from '@/api/types'
import { Countdown } from '@/components/ui/countdown'

interface Props {
  game: GameDetailViewModel
}

export function GameStatusBanner({ game }: Props) {
  if (game.status === 'Finished') {
    return (
      <div role="alert" className="bg-warning/15 border-b border-warning/40 px-4 py-2 text-sm flex items-center gap-2">
        <Trophy className="h-4 w-4 text-warning" aria-hidden />
        <span>
          {game.winnerName ? <><strong>{game.winnerName} wins!</strong></> : <strong>Game Over!</strong>}
          {' — '}
          <NavLink to="results" className="underline hover:opacity-80">View final results</NavLink>
        </span>
      </div>
    )
  }

  if (game.endTime) {
    const remaining = new Date(game.endTime).getTime() - Date.now()
    if (remaining > 0 && remaining <= 24 * 3_600_000) {
      const danger = remaining < 3_600_000
      return (
        <div role="status" aria-live="polite" className={cn(
          'border-b px-4 py-2 text-sm flex items-center gap-2',
          danger ? 'bg-danger/15 border-danger/40' : 'bg-warning/10 border-warning/30',
        )}>
          <AlertTriangle className={cn('h-4 w-4', danger ? 'text-danger' : 'text-warning')} aria-hidden />
          <span>Game ends in <strong><Countdown to={game.endTime} dangerBelowMs={3_600_000} /></strong></span>
        </div>
      )
    }
  }
  return null
}
