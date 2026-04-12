import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import { useCurrentGame } from '@/contexts/CurrentGameContext'
import type {
  PlayerResourcesViewModel,
  PublicPlayerViewModel,
  PaginatedResponse,
  UnitsViewModel,
  AssetsViewModel,
  PlayerNotificationViewModel,
} from '@/api/types'
import { relativeTime, cn } from '@/lib/utils'
import { useTouchZoomPan } from '@/hooks/useTouchZoomPan'

const POLL_MS = 10_000

function formatNum(v: number): string {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`
  if (v >= 10_000) return `${(v / 1_000).toFixed(1)}K`
  return Math.floor(v).toLocaleString()
}

function notificationIcon(kind: string): string {
  switch (kind) {
    case 'Warning': return '⚠️'
    case 'GameEvent': return '⚡'
    case 'AttackReceived': return '⚔️'
    case 'AllianceRequest': return '🤝'
    case 'MessageReceived': return '✉️'
    default: return 'ℹ️'
  }
}

interface SectionProps {
  title: string
  children: React.ReactNode
  className?: string
}

function Section({ title, children, className }: SectionProps) {
  return (
    <div className={cn('rounded-lg border bg-card', className)}>
      <div className="border-b px-4 py-2.5">
        <h2 className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{title}</h2>
      </div>
      <div className="p-4">{children}</div>
    </div>
  )
}

function Skeleton() {
  return <div className="h-4 w-24 rounded bg-secondary animate-pulse" />
}

interface GameLiveViewProps {
  gameId: string
}

export function GameLiveView({ gameId }: GameLiveViewProps) {
  const { currentGame } = useCurrentGame()
  const isFinished = currentGame?.status === 'Finished'
  const refetchInterval = isFinished ? false : POLL_MS
  const { containerProps, wrapperStyle } = useTouchZoomPan()

  const { data: resources, dataUpdatedAt: resourcesUpdatedAt } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    refetchInterval,
  })

  const { data: rankingData, dataUpdatedAt: rankingUpdatedAt } = useQuery<PaginatedResponse<PublicPlayerViewModel>>({
    queryKey: ['ranking', gameId],
    queryFn: () =>
      apiClient.get<PaginatedResponse<PublicPlayerViewModel>>('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
    refetchInterval,
  })

  const { data: unitsData } = useQuery<UnitsViewModel>({
    queryKey: ['units', gameId],
    queryFn: () => apiClient.get('/api/units').then((r) => r.data),
    refetchInterval,
  })

  const { data: assetsData } = useQuery<AssetsViewModel>({
    queryKey: ['assets', gameId],
    queryFn: () => apiClient.get('/api/assets').then((r) => r.data),
    refetchInterval,
  })

  const { data: notifications } = useQuery<PlayerNotificationViewModel[]>({
    queryKey: ['notifications', gameId],
    queryFn: () => apiClient.get('/api/notifications/recent').then((r) => r.data),
    refetchInterval,
  })

  const lastUpdated = Math.max(resourcesUpdatedAt, rankingUpdatedAt)
  const lastUpdatedDate = lastUpdated ? new Date(lastUpdated) : null

  const players = rankingData?.items ?? []
  const sorted = [...players].sort((a, b) => b.land - a.land)
  const me = sorted.find((p) => p.isCurrentPlayer)
  const myRank = me ? sorted.indexOf(me) + 1 : null

  const builtAssets = assetsData?.assets.filter((a) => a.built) ?? []
  const buildingAssets = assetsData?.assets.filter((a) => a.alreadyQueued && !a.built) ?? []

  const homeUnits = unitsData?.units.filter((u) => u.positionPlayerId === null) ?? []

  return (
    <div {...containerProps}>
    <div className="space-y-4" style={wrapperStyle}>
      {/* Page header */}
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold">Live View</h1>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          {!isFinished && (
            <span className="flex items-center gap-1">
              <span className="h-2 w-2 rounded-full bg-success animate-pulse" aria-hidden="true" />
              Live
            </span>
          )}
          {lastUpdatedDate && (
            <span>Updated {relativeTime(lastUpdatedDate.toISOString())}</span>
          )}
        </div>
      </div>

      {/* Game-over banner */}
      {isFinished && (
        <div className="rounded-lg border border-warning/50 bg-warning/10 p-4 flex items-center justify-between gap-4">
          <div>
            <strong className="text-warning-foreground">Game Over!</strong>
            {currentGame?.winnerName && (
              <span className="ml-2 text-sm text-muted-foreground">
                {currentGame.winnerName} wins
              </span>
            )}
          </div>
          <Link
            to={`/games/${gameId}/results`}
            className="text-sm px-4 py-1.5 rounded bg-warning text-warning-foreground hover:opacity-90 shrink-0"
          >
            View Results
          </Link>
        </div>
      )}

      {/* My position summary strip */}
      {(me || resources) && (
        <div className="rounded-lg border bg-card px-4 py-3 flex flex-wrap gap-4 items-center text-sm">
          {myRank && (
            <span className="font-semibold">
              Rank <span className="text-primary">#{myRank}</span>
              <span className="text-muted-foreground font-normal"> / {sorted.length}</span>
            </span>
          )}
          {me && (
            <span className="font-mono text-sm">
              Land: <strong>{Math.floor(me.land).toLocaleString()}</strong>
            </span>
          )}
          {resources && Object.entries(resources.primaryResource.cost).map(([name, value]) => (
            <span key={name} className="font-mono text-sm capitalize">
              {name}: <strong>{formatNum(value)}</strong>
            </span>
          ))}
          {resources && Object.entries(resources.secondaryResources.cost).map(([name, value]) => (
            <span key={name} className="font-mono text-sm capitalize">
              {name}: <strong>{formatNum(value)}</strong>
            </span>
          ))}
          {!resources && <Skeleton />}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Left column: ranking */}
        <Section title="Live Leaderboard">
          {sorted.length === 0 ? (
            <div className="space-y-2">
              {[...Array(5)].map((_, i) => <Skeleton key={i} />)}
            </div>
          ) : (
            <div className="overflow-hidden rounded border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-secondary/30 text-xs text-muted-foreground uppercase tracking-wide">
                    <th scope="col" className="px-3 py-1.5 text-left w-8">#</th>
                    <th scope="col" className="px-3 py-1.5 text-left">Player</th>
                    <th scope="col" className="px-3 py-1.5 text-right">Land</th>
                    <th scope="col" className="px-3 py-1.5 text-center hidden sm:table-cell">Type</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {sorted.slice(0, 20).map((p, i) => (
                    <tr
                      key={p.playerId ?? i}
                      className={cn(
                        'transition-colors',
                        p.isCurrentPlayer
                          ? 'bg-primary/10 font-semibold border-l-2 border-l-primary'
                          : 'hover:bg-secondary/20'
                      )}
                    >
                      <td className="px-3 py-1.5 font-mono text-muted-foreground text-xs">#{i + 1}</td>
                      <td className="px-3 py-1.5">
                        <Link
                          to={`/games/${gameId}/player/${p.playerId}`}
                          className={cn(
                            'hover:text-primary transition-colors',
                            p.isCurrentPlayer && 'text-primary'
                          )}
                        >
                          {p.playerName}
                          {p.isCurrentPlayer && (
                            <span className="ml-1.5 text-xs text-muted-foreground font-normal">(you)</span>
                          )}
                        </Link>
                      </td>
                      <td className="px-3 py-1.5 text-right font-mono text-xs">
                        {Math.floor(p.land).toLocaleString()}
                      </td>
                      <td className="px-3 py-1.5 text-center hidden sm:table-cell">
                        <span className={cn(
                          'rounded px-1.5 py-0.5 text-[10px] font-medium',
                          p.isAgent ? 'bg-accent text-accent-foreground' : 'bg-info/50 text-info-foreground'
                        )}>
                          {p.isAgent ? 'AI' : 'Human'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {sorted.length > 20 && (
                <div className="border-t px-3 py-2 text-xs text-muted-foreground text-center">
                  +{sorted.length - 20} more players —{' '}
                  <Link to={`/games/${gameId}/ranking`} className="text-primary hover:underline">
                    Full ranking
                  </Link>
                </div>
              )}
            </div>
          )}
        </Section>

        {/* Right column: my state */}
        <div className="space-y-4">
          {/* Units at home */}
          <Section title="Units at Base">
            {!unitsData ? (
              <div className="space-y-2"><Skeleton /><Skeleton /></div>
            ) : homeUnits.length === 0 ? (
              <p className="text-sm text-muted-foreground">No units at base.</p>
            ) : (
              <div className="grid grid-cols-2 gap-2">
                {homeUnits.map((u) => (
                  <div key={u.unitId} className="rounded border bg-secondary/20 px-3 py-2">
                    <div className="text-xs text-muted-foreground truncate">{u.definition.name}</div>
                    <div className="font-mono font-semibold">{u.count.toLocaleString()}</div>
                  </div>
                ))}
              </div>
            )}
          </Section>

          {/* Buildings */}
          <Section title="Buildings">
            {!assetsData ? (
              <div className="space-y-2"><Skeleton /><Skeleton /></div>
            ) : builtAssets.length === 0 && buildingAssets.length === 0 ? (
              <p className="text-sm text-muted-foreground">No buildings yet.</p>
            ) : (
              <div className="space-y-1">
                {builtAssets.map((a) => (
                  <div key={a.definition.id} className="flex items-center gap-2 text-sm">
                    <span className="text-success text-xs">✓</span>
                    <span>{a.definition.name}</span>
                  </div>
                ))}
                {buildingAssets.map((a) => (
                  <div key={a.definition.id} className="flex items-center gap-2 text-sm text-muted-foreground">
                    <span className="text-warning text-xs">⚙</span>
                    <span>{a.definition.name}</span>
                    {a.ticksLeftForBuild > 0 && (
                      <span className="text-xs">({a.ticksLeftForBuild} rounds)</span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </Section>

          {/* Events feed */}
          <Section title="Recent Events">
            {!notifications ? (
              <div className="space-y-2"><Skeleton /><Skeleton /><Skeleton /></div>
            ) : notifications.length === 0 ? (
              <p className="text-sm text-muted-foreground">No recent events.</p>
            ) : (
              <ul className="divide-y">
                {notifications.slice(0, 8).map((n) => (
                  <li key={n.id} className="flex items-start gap-2 py-2">
                    <span className="text-sm shrink-0">{notificationIcon(n.kind)}</span>
                    <div className="flex-1 min-w-0">
                      <div className="text-sm">{n.message}</div>
                      <div className="text-xs text-muted-foreground">{relativeTime(n.createdAt)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Section>
        </div>
      </div>
    </div>
    </div>
  )
}
