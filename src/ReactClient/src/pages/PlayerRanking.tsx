import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { PublicPlayerViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

interface PlayerRankingProps {
  gameId: string
}

type Filter = 'all' | 'human' | 'agent'

export function PlayerRanking({ gameId }: PlayerRankingProps) {
  const [filter, setFilter] = useState<Filter>('all')

  const { data: players, isLoading } = useQuery({
    queryKey: ['ranking', gameId],
    queryFn: () =>
      apiClient.get<PublicPlayerViewModel[]>('/api/playerranking').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const filtered = (players ?? []).filter((p) => {
    if (filter === 'human') return !p.isAgent
    if (filter === 'agent') return p.isAgent
    return true
  })

  const filterBtn = (f: Filter, label: string) => (
    <button
      key={f}
      onClick={() => setFilter(f)}
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
        <p className="text-muted-foreground text-sm">Loading…</p>
      ) : (
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide">
                <th className="px-4 py-2 text-left w-10">#</th>
                <th className="px-4 py-2 text-left">Player</th>
                <th className="px-4 py-2 text-right">Score</th>
                <th className="px-4 py-2 text-right hidden sm:table-cell">Minerals</th>
                <th className="px-4 py-2 text-right hidden sm:table-cell">Gas</th>
                <th className="px-4 py-2 text-right hidden md:table-cell">Units</th>
                <th className="px-4 py-2 text-center hidden md:table-cell">Type</th>
                <th className="px-4 py-2 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filtered.map((p, i) => (
                <tr key={p.playerId} className="hover:bg-secondary/20 transition-colors">
                  <td className="px-4 py-2 font-mono text-muted-foreground">#{i + 1}</td>
                  <td className="px-4 py-2">
                    <Link
                      to={`/games/${gameId}/player/${p.playerId}`}
                      className="hover:text-primary transition-colors font-medium"
                    >
                      {p.playerName}
                    </Link>
                    {p.protectionTicksRemaining > 0 && (
                      <span className="ml-1.5 text-[10px] text-yellow-400" title="Under protection">🛡</span>
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
                      p.isAgent ? 'bg-purple-900/50 text-purple-300' : 'bg-blue-900/50 text-blue-300'
                    )}>
                      {p.isAgent ? 'AI' : 'Human'}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-center">
                    <span className={cn(
                      'inline-block h-2 w-2 rounded-full',
                      p.isOnline ? 'bg-green-400' : 'bg-muted-foreground'
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
