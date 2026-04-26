import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router'
import apiClient from '@/api/client'
import type { GameListViewModel } from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonLine } from '@/components/Skeleton'
import { GamesTable } from '@/components/games/GamesTable'
import { useSignalR } from '@/hooks/useSignalR'
import { useEffect, useCallback } from 'react'

function SectionHeader({ label, meta }: { label: string; meta?: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between border-b border-border mb-2 pt-6 pb-1.5">
      <h2 className="label">{label}</h2>
      {meta && <div className="text-[10.5px] text-muted-foreground mono">{meta}</div>}
    </div>
  )
}

export function Games() {
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const isNewPlayer = searchParams.get('welcome') === '1'
  const [showAllFinished, setShowAllFinished] = useState(false)
  const { on, off } = useSignalR('/gamehub')

  const { data, isLoading, error, refetch } = useQuery<GameListViewModel>({
    queryKey: ['games'],
    queryFn: () => apiClient.get('/api/games').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const handleLobbyUpdate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['games'] })
  }, [queryClient])

  useEffect(() => {
    on('LobbyUpdated', handleLobbyUpdate)
    return () => off('LobbyUpdated', handleLobbyUpdate)
  }, [on, off, handleLobbyUpdate])

  const games = data?.games ?? []
  const active = games.filter((g) => g.status === 'Active')
  const upcoming = games.filter((g) => g.status === 'Upcoming')
  const finished = games.filter((g) => g.status === 'Finished')
  const visibleFinished = showAllFinished || finished.length <= 5 ? finished : finished.slice(0, 5)

  return (
    <div className="max-w-5xl mx-auto p-3 sm:p-4">
      <h1 className="text-[22px] font-bold tracking-tight">Games</h1>
      <p className="text-[11.5px] text-muted-foreground mt-1 mb-4">
        Season schedule · live, upcoming, and finished seasons · {games.length} total
      </p>

      {isNewPlayer && (
        <div className="border-l-2 border-[color:var(--color-resource)] bg-[color:var(--color-resource)]/5 px-3 py-2 text-sm mb-4">
          <strong className="text-resource">Welcome to BGE.</strong>{' '}
          <span className="text-muted-foreground">Your commander is ready. Join an active game below, or register for an upcoming season.</span>
        </div>
      )}

      {isLoading && (
        <div className="space-y-3 mt-4" aria-hidden="true">
          <SkeletonLine className="h-5 w-24" />
          <SkeletonLine className="h-4 w-full" />
          <SkeletonLine className="h-4 w-full" />
          <SkeletonLine className="h-4 w-full" />
        </div>
      )}
      {error && !data && <ApiError message="Failed to load games." onRetry={() => void refetch()} />}

      {data && (
        <>
          <SectionHeader label="Active — in progress" meta={`${active.length} game${active.length === 1 ? '' : 's'}`} />
          {active.length === 0 ? (
            <p className="text-[11.5px] text-muted-foreground py-2">
              No games are currently running. Check upcoming seasons below or come back later.
            </p>
          ) : (
            <GamesTable section="active" games={active} />
          )}

          <SectionHeader label="Upcoming — pre-season" meta={`${upcoming.length} game${upcoming.length === 1 ? '' : 's'}`} />
          {upcoming.length === 0 ? (
            <p className="text-[11.5px] text-muted-foreground py-2">
              No upcoming games scheduled yet. Admins post new seasons here.
            </p>
          ) : (
            <GamesTable section="upcoming" games={upcoming} />
          )}

          {finished.length > 0 && (
            <>
              <SectionHeader
                label="Finished"
                meta={
                  <>
                    {visibleFinished.length} shown
                    {finished.length > 5 && (
                      <>
                        {' · '}
                        <button
                          className="text-interactive hover:underline"
                          onClick={() => setShowAllFinished((v) => !v)}
                        >
                          {showAllFinished ? 'collapse' : `show all (${finished.length})`}
                        </button>
                      </>
                    )}
                  </>
                }
              />
              <GamesTable section="finished" games={visibleFinished} />
            </>
          )}
        </>
      )}
    </div>
  )
}
