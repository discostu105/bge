import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { PublicPlayerViewModel, PaginatedResponse } from '@/api/types'
import { cn } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface PlayerRankingProps {
  gameId: string
}

type Filter = 'all' | 'human' | 'agent'

export function PlayerRanking({ gameId }: PlayerRankingProps) {
  const [filter, setFilter] = useState<Filter>('all')

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['ranking', gameId],
    queryFn: () =>
      apiClient.get<PaginatedResponse<PublicPlayerViewModel>>('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
    refetchInterval: 30_000,
  })

  const players = data?.items
  const sorted = [...(players ?? [])].sort((a, b) => b.score - a.score)
  const filtered = sorted.filter((p) => {
    if (filter === 'human') return !p.isAgent
    if (filter === 'agent') return p.isAgent
    return true
  })

  const filterBtn = (f: Filter, label: string) => (
    <button
      key={f}
      onClick={() => setFilter(f)}
      aria-pressed={filter === f}
      className={cn(
        'rounded px-3 py-1 text-sm',
        filter === f
          ? 'bg-primary text-primary-foreground'
          : 'border hover:bg-secondary text-muted-foreground'
      )}
    >
      {label}
    </button>
  )

  return (
    <div className="max-w-3xl space-y-4">
      <h1 className="text-xl font-bold">Player Ranking</h1>

      <div className="flex gap-2">
        {filterBtn('all', 'All')}
        {filterBtn('human', 'Human')}
        {filterBtn('agent', 'Agent')}
      </div>

      {isLoading ? (
        <PageLoader message="Loading ranking..." />
      ) : error ? (
        <ApiError message="Failed to load ranking." onRetry={() => void refetch()} />
      ) : (
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide">
                <th scope="col" className="px-4 py-2 text-left w-10">#</th>
                <th scope="col" className="px-4 py-2 text-left">Player</th>
                <th scope="col" className="px-4 py-2 text-right">Score</th>
                <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">Minerals</th>
                <th scope="col" className="px-4 py-2 text-right hidden sm:table-cell">Gas</th>
                <th scope="col" className="px-4 py-2 text-right hidden md:table-cell">Units</th>
                <th scope="col" className="px-4 py-2 text-center hidden md:table-cell">Type</th>
                <th scope="col" className="px-4 py-2 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filtered.map((p, i) => (
                <tr key={p.playerId} className={cn(
                  'transition-colors',
                  p.isCurrentPlayer
                    ? 'bg-primary/10 border-l-2 border-l-primary font-semibold'
                    : 'hover:bg-secondary/20'
                )}>
                  <td className="px-4 py-2 font-mono text-muted-foreground">#{i + 1}</td>
                  <td className="px-4 py-2">
                    <Link
                      to={`/games/${gameId}/player/${p.playerId}`}
                      className={cn(
                        'hover:text-primary transition-colors font-medium',
                        p.isCurrentPlayer && 'text-primary'
                      )}
                    >
                      {p.playerName}
                      {p.isCurrentPlayer && <span className="ml-1.5 text-xs text-muted-foreground font-normal">(you)</span>}
                    </Link>
                    {p.protectionTicksRemaining > 0 && (
                      <span className="ml-1.5 text-[10px] text-warning-foreground" title="Under protection">🛡</span>
                    )}
                  </td>
                  <td className="px-4 py-2 text-right font-mono">{Math.floor(p.score).toLocaleString()}</td>
                  <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">
                    {p.approxMinerals != null ? Math.floor(p.approxMinerals).toLocaleString() : <span className="text-muted-foreground italic">?</span>}
                  </td>
                  <td className="px-4 py-2 text-right font-mono hidden sm:table-cell">
                    {p.approxGas != null ? Math.floor(p.approxGas).toLocaleString() : <span className="text-muted-foreground italic">?</span>}
                  </td>
                  <td className="px-4 py-2 text-right font-mono hidden md:table-cell">
                    {p.approxHomeUnitCount ?? <span className="text-muted-foreground italic">?</span>}
                  </td>
                  <td className="px-4 py-2 text-center hidden md:table-cell">
                    <span className={cn(
                      'rounded px-1.5 py-0.5 text-[10px] font-medium',
                      p.isAgent ? 'bg-accent text-accent-foreground' : 'bg-info/50 text-info-foreground'
                    )}>
                      {p.isAgent ? 'AI' : 'Human'}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-center">
                    <span className={cn(
                      'inline-block h-2 w-2 rounded-full',
                      p.isOnline ? 'bg-success' : 'bg-muted-foreground'
                    )} title={p.isOnline ? 'Online' : 'Offline'} />
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
