import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import apiClient from '@/api/client'
import type { SpectatorSnapshotViewModel } from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonRow } from '@/components/Skeleton'
import { cn } from '@/lib/utils'
import { useSignalR } from '@/hooks/useSignalR'

interface SpectatorProps {
  gameId: string
}


export function Spectator({ gameId }: SpectatorProps) {
  const [liveSnapshot, setLiveSnapshot] = useState<SpectatorSnapshotViewModel | null>(null)

  const hubUrl = useMemo(() => `/hubs/spectator?gameId=${encodeURIComponent(gameId)}`, [gameId])
  const signalR = useSignalR(hubUrl)

  useEffect(() => {
    const handler = (...args: unknown[]) => {
      setLiveSnapshot(args[0] as SpectatorSnapshotViewModel)
    }
    signalR.on('SpectatorSnapshot', handler)
    return () => { signalR.off('SpectatorSnapshot', handler) }
  }, [signalR])

  const { data: initialSnapshot, isLoading, error, refetch } = useQuery({
    queryKey: ['spectator', gameId],
    queryFn: () =>
      apiClient.get<SpectatorSnapshotViewModel>(`/api/games/${gameId}/spectate`).then((r) => r.data),
    refetchOnWindowFocus: false,
  })

  const snapshot = liveSnapshot ?? initialSnapshot

  return (
    <div className="max-w-3xl mx-auto space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold">
            {snapshot ? snapshot.gameName : 'Live Spectator'}
          </h1>
          {snapshot && (
            <p className="text-sm text-muted-foreground">
              Status: <span className="font-medium">{snapshot.gameStatus}</span>
              {' · '}Tick: <span className="font-mono">{snapshot.tick}</span>
              {' · '}
              <span className={cn(
                'text-xs font-medium',
                signalR.status === 'connected' ? 'text-green-600' : 'text-muted-foreground'
              )}>
                {signalR.status === 'connected' ? '● Live' : '○ Connecting…'}
              </span>
            </p>
          )}
        </div>
        <Link to={`/lobby/${gameId}`} className="text-sm text-primary hover:underline">
          Game Lobby →
        </Link>
      </div>

      {error ? (
        <ApiError message="Failed to load spectator data." onRetry={() => void refetch()} />
      ) : (
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide">
                <th scope="col" className="px-4 py-2 text-left w-10">#</th>
                <th scope="col" className="px-4 py-2 text-left">Player</th>
                <th scope="col" className="px-4 py-2 text-right">Land</th>
                <th scope="col" className="px-4 py-2 text-center hidden sm:table-cell">Type</th>
                <th scope="col" className="px-4 py-2 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {isLoading && !snapshot && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}
              {snapshot?.topPlayers.map((p) => {
                return (
                  <tr key={p.playerId} className="hover:bg-secondary/20 transition-colors">
                    <td className="px-4 py-2 font-mono text-muted-foreground">#{p.rank}</td>
                    <td className="px-4 py-2 font-medium">{p.playerName}</td>
                    <td className="px-4 py-2 text-right font-mono">{Math.floor(p.land).toLocaleString()}</td>
                    <td className="px-4 py-2 text-center hidden sm:table-cell">
                      <span className={cn(
                        'rounded px-1.5 py-0.5 text-[10px] font-medium',
                        p.isAgent ? 'bg-accent text-accent-foreground' : 'bg-info/50 text-info-foreground'
                      )}>
                        {p.isAgent ? 'AI' : 'Human'}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-center">
                      <span
                        className={cn(
                          'inline-block h-2 w-2 rounded-full',
                          p.isOnline ? 'bg-success' : 'bg-muted-foreground'
                        )}
                        title={p.isOnline ? 'Online' : 'Offline'}
                      />
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
