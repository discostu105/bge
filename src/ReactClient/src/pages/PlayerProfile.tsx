import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import { TrophyIcon, SwordsIcon, StarIcon, KeyIcon, CopyIcon, CheckIcon, Trash2Icon, PlusIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type {
  ProfileViewModel,
  PlayerHistoryViewModel,
  PlayerListViewModel,
  ApiKeyListViewModel,
  CreateApiKeyResponse,
} from '@/api/types'
import { ApiError } from '@/components/ApiError'
import { SkeletonCard, SkeletonLine } from '@/components/Skeleton'
import { relativeTime } from '@/lib/utils'
import { useConfirm } from '@/contexts/ConfirmContext'
import { ResourceIcon } from '@/components/ui/resource-icon'

function ApiKeySection() {
  const queryClient = useQueryClient()
  const confirm = useConfirm()
  const [newKey, setNewKey] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [newKeyName, setNewKeyName] = useState('')

  const { data: playerList, isLoading: isPlayersLoading } = useQuery({
    queryKey: ['my-players'],
    queryFn: () => apiClient.get<PlayerListViewModel>('/api/player-management').then(r => r.data),
  })

  const player = playerList?.players?.[0]
  const playerId = player?.playerId

  const { data: keysData, isLoading: isKeysLoading } = useQuery({
    queryKey: ['api-keys', playerId],
    queryFn: () =>
      apiClient.get<ApiKeyListViewModel>(`/api/player-management/${playerId}/apikeys`).then(r => r.data),
    enabled: !!playerId,
  })

  const createMutation = useMutation({
    mutationFn: ({ playerId, name }: { playerId: string; name: string }) =>
      apiClient
        .post<CreateApiKeyResponse>(`/api/player-management/${playerId}/apikeys`, {
          name: name.trim() || null,
        })
        .then(r => r.data),
    onSuccess: (data) => {
      setNewKey(data.apiKey)
      setNewKeyName('')
      void queryClient.invalidateQueries({ queryKey: ['my-players'] })
      void queryClient.invalidateQueries({ queryKey: ['api-keys', playerId] })
    },
  })

  const revokeMutation = useMutation({
    mutationFn: ({ playerId, keyId }: { playerId: string; keyId: string }) =>
      apiClient.delete(`/api/player-management/${playerId}/apikeys/${keyId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['my-players'] })
      void queryClient.invalidateQueries({ queryKey: ['api-keys', playerId] })
    },
  })

  const handleCopy = async () => {
    if (!newKey) return
    await navigator.clipboard.writeText(newKey)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  if (isPlayersLoading) return null
  if (!player || !playerId) return null

  const keys = keysData?.keys ?? []

  return (
    <div className="rounded-lg border bg-card p-5 space-y-3">
      <strong className="text-sm flex items-center gap-1.5">
        <KeyIcon className="h-4 w-4 text-primary" />
        Bot API Keys
      </strong>
      <p className="text-xs text-muted-foreground">
        Use an API key to control your player programmatically via the REST API. You can have
        multiple keys; revoke any one individually without affecting the others.
      </p>

      {newKey && (
        <div className="rounded border bg-secondary/30 p-3 space-y-2">
          <div className="text-xs font-medium text-warning-foreground">
            Copy this key now — it won't be shown again.
          </div>
          <div className="flex items-center gap-2">
            <code className="flex-1 text-xs bg-background rounded px-2 py-1.5 break-all select-all border">
              {newKey}
            </code>
            <button
              onClick={() => void handleCopy()}
              className="shrink-0 p-1.5 rounded hover:bg-secondary"
              aria-label="Copy API key"
            >
              {copied ? <CheckIcon className="h-4 w-4 text-green-500" /> : <CopyIcon className="h-4 w-4" />}
            </button>
          </div>
        </div>
      )}

      <div className="flex items-center gap-2">
        <input
          type="text"
          value={newKeyName}
          onChange={(e) => setNewKeyName(e.target.value)}
          placeholder="Key name (optional)"
          maxLength={50}
          className="flex-1 min-w-0 rounded border bg-background px-2 py-1.5 text-xs"
          disabled={createMutation.isPending}
        />
        <button
          onClick={() => createMutation.mutate({ playerId, name: newKeyName })}
          disabled={createMutation.isPending}
          className="shrink-0 rounded bg-primary px-3 py-1.5 text-xs text-primary-foreground hover:opacity-90 disabled:opacity-50 flex items-center gap-1"
        >
          <PlusIcon className="h-3 w-3" />
          {createMutation.isPending ? 'Creating...' : 'Create key'}
        </button>
      </div>

      {isKeysLoading ? (
        <div className="text-xs text-muted-foreground">Loading keys...</div>
      ) : keys.length === 0 ? (
        <div className="text-xs text-muted-foreground italic">No API keys yet.</div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-xs" aria-label="API keys">
            <thead className="border-b">
              <tr className="text-left text-muted-foreground">
                <th scope="col" className="py-1.5 pr-2 font-medium">Name</th>
                <th scope="col" className="py-1.5 px-2 font-medium">Identifier</th>
                <th scope="col" className="py-1.5 px-2 font-medium">Created</th>
                <th scope="col" className="py-1.5 px-2 font-medium">Last used</th>
                <th scope="col" className="py-1.5 pl-2"><span className="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.keyId} className="border-b border-border last:border-0">
                  <td className="py-2 pr-2">
                    {k.name ?? <span className="text-muted-foreground italic">unnamed</span>}
                  </td>
                  <td className="py-2 px-2 font-mono text-muted-foreground">{k.keyPrefix}…</td>
                  <td className="py-2 px-2 text-muted-foreground whitespace-nowrap">
                    {relativeTime(k.createdAt)}
                  </td>
                  <td className="py-2 px-2 text-muted-foreground whitespace-nowrap">
                    {k.lastAccessedAt ? relativeTime(k.lastAccessedAt) : <span className="italic">never</span>}
                  </td>
                  <td className="py-2 pl-2 text-right">
                    <button
                      onClick={async () => {
                        const ok = await confirm({
                          title: 'Revoke API key?',
                          description: `Any bot or integration using "${k.name ?? k.keyPrefix}" will immediately stop working. Other keys remain active.`,
                          destructive: true,
                          confirmLabel: 'Revoke key',
                          cancelLabel: 'Keep key',
                        })
                        if (ok) revokeMutation.mutate({ playerId, keyId: k.keyId })
                      }}
                      disabled={revokeMutation.isPending}
                      className="rounded border border-destructive/50 px-2 py-1 text-destructive hover:bg-destructive/10 disabled:opacity-50 inline-flex items-center gap-1"
                      aria-label={`Revoke key ${k.name ?? k.keyPrefix}`}
                    >
                      <Trash2Icon className="h-3 w-3" />
                      Revoke
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(createMutation.isError || revokeMutation.isError) && (
        <p className="text-xs text-destructive">Something went wrong. Please try again.</p>
      )}
    </div>
  )
}

function outcomeLabel(isWin: boolean, gameStatus: string): { label: string; className: string } {
  if (isWin) return { label: 'Win', className: 'bg-green-700/30 text-green-300' }
  if (gameStatus.toLowerCase() === 'active' || gameStatus.toLowerCase() === 'running') {
    return { label: 'DNF', className: 'bg-secondary/40 text-muted-foreground' }
  }
  return { label: 'Loss', className: 'bg-red-900/30 text-red-300' }
}

export function PlayerProfile() {
  const { data: profile, isLoading, error, refetch } = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.get<ProfileViewModel>('/api/profile').then((r) => r.data),
  })

  const { data: historyData } = useQuery<PlayerHistoryViewModel>({
    queryKey: ['player-history'],
    queryFn: () => apiClient.get('/api/history').then(r => r.data),
  })

  if (isLoading) return (
    <div className="max-w-lg space-y-5" aria-hidden="true">
      <SkeletonLine className="h-6 w-40" />
      <div className="rounded-lg border bg-card p-6 space-y-5">
        <div className="flex items-center gap-4 animate-pulse">
          <div className="h-16 w-16 rounded-full bg-muted" />
          <div className="space-y-2">
            <div className="h-5 w-32 rounded bg-muted" />
            <div className="h-3.5 w-24 rounded bg-muted" />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <SkeletonCard />
          <SkeletonCard />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      </div>
    </div>
  )
  if (error) return <ApiError message="Failed to load profile." onRetry={() => void refetch()} />
  if (!profile) return <p className="text-destructive text-sm">Failed to load profile.</p>

  const displayName = profile.displayName ?? profile.playerName ?? 'Unknown'
  const losses = profile.gamesPlayed - profile.wins
  const winRate = profile.gamesPlayed > 0 ? (profile.wins / profile.gamesPlayed) * 100 : 0
  const historyGames = historyData?.games ?? []
  const avgLand = historyGames.length > 0
    ? historyGames.reduce((sum, g) => sum + g.finalLand, 0) / historyGames.length
    : 0

  return (
    <div className="max-w-2xl space-y-5">
      <h1 className="text-xl font-bold">My Profile</h1>

      <div className="rounded-lg border bg-card p-6 space-y-5">
        {/* Avatar + name + join date */}
        <div className="flex items-center gap-4">
          {profile.avatarUrl ? (
            <img
              src={profile.avatarUrl}
              alt={displayName}
              width="64"
              height="64"
              className="h-16 w-16 rounded-full border-2 border-primary/50"
              fetchPriority="high"
            />
          ) : (
            <div className="h-16 w-16 rounded-full bg-secondary flex items-center justify-center text-2xl border-2 border-primary/50">
              {displayName[0]?.toUpperCase() ?? '?'}
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-bold text-lg">{displayName}</span>
            </div>
            {profile.playerName && profile.displayName && profile.playerName !== profile.displayName && (
              <div className="text-sm text-muted-foreground">Playing as {profile.playerName}</div>
            )}
            {profile.joinedAt && (
              <div className="text-xs text-muted-foreground mt-0.5">
                Member since {relativeTime(profile.joinedAt)}
              </div>
            )}
          </div>
        </div>

        {/* Current game stats */}
        {profile.currentGameId ? (
          <>
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Land</div>
                <div className="text-2xl font-bold">{Math.floor(profile.land).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Rank</div>
                <div className="text-2xl font-bold">
                  {profile.rank}
                  <span className="text-sm text-muted-foreground font-normal"> / {profile.totalPlayers}</span>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1 inline-flex items-center gap-1">
                  <ResourceIcon name="land" /> Land
                </div>
                <div className="text-lg font-semibold text-[#b8804a]">{Math.floor(profile.land).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1 inline-flex items-center gap-1">
                  <ResourceIcon name="minerals" /> Minerals
                </div>
                <div className="text-lg font-semibold text-blue-400">{Math.floor(profile.minerals).toLocaleString()}</div>
              </div>
              <div className="rounded border bg-secondary/20 p-3">
                <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1 inline-flex items-center gap-1">
                  <ResourceIcon name="gas" /> Gas
                </div>
                <div className="text-lg font-semibold text-green-400">{Math.floor(profile.gas).toLocaleString()}</div>
              </div>
            </div>

            <div className="rounded border bg-secondary/20 p-3 flex items-center gap-2">
              <SwordsIcon className="h-4 w-4 text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Army Size:</span>
              <span className="font-semibold">{profile.armySize.toLocaleString()}</span>
            </div>

            <Link
              to={`/games/${profile.currentGameId}/base`}
              className="block rounded bg-primary px-4 py-2 text-center text-sm text-primary-foreground hover:opacity-90"
            >
              Go to Game →
            </Link>
          </>
        ) : (
          <div className="rounded border border-dashed px-4 py-3 text-sm text-muted-foreground">
            Not currently in a game.{' '}
            <Link to="/games" className="text-primary hover:underline">Browse games</Link>
          </div>
        )}
      </div>

      {/* Career stats — 6 cells */}
      {profile.gamesPlayed > 0 && (
        <div className="rounded-lg border bg-card p-5 space-y-3">
          <strong className="text-sm flex items-center gap-1.5">
            <TrophyIcon className="h-4 w-4 text-primary" />
            Career Stats
          </strong>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Games</div>
              <div className="text-xl font-bold">{profile.gamesPlayed}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Wins</div>
              <div className="text-xl font-bold text-green-400">{profile.wins}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Losses</div>
              <div className="text-xl font-bold text-red-400">{losses}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Win Rate</div>
              <div className="text-xl font-bold">{winRate.toFixed(0)}%</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Avg Land</div>
              <div className="text-xl font-bold">{avgLand > 0 ? Math.floor(avgLand).toLocaleString() : '—'}</div>
            </div>
            <div className="rounded border bg-secondary/20 p-3 text-center">
              <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Best Rank</div>
              <div className="text-xl font-bold">
                {profile.bestRank > 0 ? (
                  <span className="flex items-center justify-center gap-1">
                    {profile.bestRank === 1 && <StarIcon className="h-4 w-4 text-amber-400" />}
                    #{profile.bestRank}
                  </span>
                ) : '—'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* API Key management */}
      <ApiKeySection />

      {/* Game history — last 20 games */}
      {historyGames.length > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Recent Games</strong>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm" aria-label="Game history">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Game</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Type</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Date</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Rank</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Outcome</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground hidden sm:table-cell">Replay</th>
                </tr>
              </thead>
              <tbody>
                {historyGames.slice(0, 20).map((g) => {
                  const outcome = outcomeLabel(g.isWin, '')
                  return (
                    <tr key={g.gameId} className="border-b border-border last:border-0">
                      <td className="py-2 px-3 font-medium">{g.gameName}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground hidden sm:table-cell">{g.gameDefType || '—'}</td>
                      <td className="py-2 px-3 text-xs text-muted-foreground whitespace-nowrap">
                        {relativeTime(g.finishedAt)}
                      </td>
                      <td className="py-2 px-3 font-mono">#{g.finalRank}</td>
                      <td className="py-2 px-3">
                        <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium ${outcome.className}`}>
                          {g.isWin && <StarIcon className="h-3 w-3" />}
                          {outcome.label}
                        </span>
                      </td>
                      <td className="py-2 px-3 text-muted-foreground hidden sm:table-cell">—</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
