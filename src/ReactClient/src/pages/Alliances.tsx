import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  AllianceViewModel,
  MyAllianceStatusViewModel,
  AllianceInviteViewModel,
  CreateAllianceRequest,
  JoinAllianceRequest,
  AcceptInviteRequest,
} from '@/api/types'

interface AlliancesProps {
  gameId: string
}

export function Alliances({ gameId }: AlliancesProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [createName, setCreateName] = useState('')
  const [createPassword, setCreatePassword] = useState('')
  const [joiningAlliance, setJoiningAlliance] = useState<AllianceViewModel | null>(null)
  const [joinPassword, setJoinPassword] = useState('')

  const { data: alliances } = useQuery<AllianceViewModel[]>({
    queryKey: ['alliances', gameId],
    queryFn: () => apiClient.get('/api/alliances').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const { data: myStatus } = useQuery<MyAllianceStatusViewModel>({
    queryKey: ['alliances-status', gameId],
    queryFn: () => apiClient.get('/api/alliances/my-status').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const { data: myInvites } = useQuery<AllianceInviteViewModel[]>({
    queryKey: ['alliances-invites', gameId],
    queryFn: () =>
      apiClient
        .get<AllianceInviteViewModel[]>('/api/alliances/my-invites')
        .then((r) => r.data)
        .catch(() => []),
    refetchInterval: 10_000,
  })

  const invalidateAll = () => {
    void queryClient.invalidateQueries({ queryKey: ['alliances', gameId] })
    void queryClient.invalidateQueries({ queryKey: ['alliances-status', gameId] })
    void queryClient.invalidateQueries({ queryKey: ['alliances-invites', gameId] })
  }

  const createMutation = useMutation({
    mutationFn: (req: CreateAllianceRequest) => apiClient.post('/api/alliances', req),
    onSuccess: () => {
      setError(null)
      setCreateName('')
      setCreatePassword('')
      invalidateAll()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Create failed')
    },
  })

  const joinMutation = useMutation({
    mutationFn: ({ allianceId, req }: { allianceId: string; req: JoinAllianceRequest }) =>
      apiClient.post(`/api/alliances/${allianceId}/join`, req),
    onSuccess: () => {
      setError(null)
      setJoiningAlliance(null)
      setJoinPassword('')
      invalidateAll()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Join failed')
    },
  })

  const leaveMutation = useMutation({
    mutationFn: () => apiClient.delete('/api/alliances/leave'),
    onSuccess: () => { setError(null); invalidateAll() },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Leave failed')
    },
  })

  const acceptInviteMutation = useMutation({
    mutationFn: ({ allianceId, req }: { allianceId: string; req: AcceptInviteRequest }) =>
      apiClient.post(`/api/alliances/${allianceId}/accept-invite`, req),
    onSuccess: () => { setError(null); invalidateAll() },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Accept failed')
    },
  })

  const declineInviteMutation = useMutation({
    mutationFn: ({ allianceId, req }: { allianceId: string; req: AcceptInviteRequest }) =>
      apiClient.post(`/api/alliances/${allianceId}/decline-invite`, req),
    onSuccess: () => { setError(null); invalidateAll() },
    onError: () => {},
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Alliances</h1>

      {myStatus?.isMember && (
        <div
          className={`rounded-lg border px-4 py-3 text-sm flex items-center justify-between ${
            myStatus.isPending
              ? 'border-yellow-700 bg-yellow-900/10 text-yellow-200'
              : 'border-green-700 bg-green-900/10 text-green-200'
          }`}
        >
          {myStatus.isPending ? (
            <span>
              Your request to join <strong>{myStatus.allianceName}</strong> is pending leader
              approval.
            </span>
          ) : (
            <span>
              You are a member of{' '}
              <strong>
                <a href={`/alliances/${myStatus.allianceId}`} className="underline">
                  {myStatus.allianceName}
                </a>
              </strong>
              {myStatus.isLeader ? ' (Leader)' : ''}.
            </span>
          )}
          <button
            onClick={() => leaveMutation.mutate()}
            disabled={leaveMutation.isPending}
            className="rounded bg-destructive px-3 py-1 text-xs text-destructive-foreground hover:opacity-90 ml-4"
          >
            Leave
          </button>
        </div>
      )}

      {error && <div className="text-destructive text-sm">{error}</div>}

      {(myInvites?.length ?? 0) > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Pending Invites</strong>
          </div>
          <div className="p-3 space-y-2">
            {myInvites!.map((invite) => (
              <div
                key={invite.inviteId}
                className="flex items-center justify-between rounded border px-3 py-2"
              >
                <div className="text-sm">
                  <strong>{invite.allianceName}</strong>
                  <span className="text-muted-foreground ml-2">
                    invited by {invite.inviterPlayerName}
                  </span>
                  <span className="text-muted-foreground ml-2 text-xs">
                    expires {new Date(invite.expiresAt).toLocaleDateString()}
                  </span>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() =>
                      acceptInviteMutation.mutate({
                        allianceId: invite.allianceId,
                        req: { inviteId: invite.inviteId },
                      })
                    }
                    className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90"
                  >
                    Accept
                  </button>
                  <button
                    onClick={() =>
                      declineInviteMutation.mutate({
                        allianceId: invite.allianceId,
                        req: { inviteId: invite.inviteId },
                      })
                    }
                    className="rounded border border-destructive px-2 py-0.5 text-xs text-destructive hover:bg-destructive/10"
                  >
                    Decline
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {myStatus && !myStatus.isMember && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Create Alliance</strong>
          </div>
          <div className="p-4 space-y-3">
            <input
              className="w-full max-w-sm rounded border bg-input px-2 py-1.5 text-sm"
              placeholder="Alliance name"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
            />
            <input
              type="password"
              className="w-full max-w-sm rounded border bg-input px-2 py-1.5 text-sm"
              placeholder="Password"
              value={createPassword}
              onChange={(e) => setCreatePassword(e.target.value)}
            />
            <button
              onClick={() =>
                createMutation.mutate({ allianceName: createName, password: createPassword })
              }
              disabled={!createName || createMutation.isPending}
              className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {createMutation.isPending ? 'Creating…' : 'Create'}
            </button>
          </div>
        </div>
      )}

      {!alliances ? (
        <div className="text-muted-foreground text-sm">Loading...</div>
      ) : (
        <div className="rounded-lg border bg-card overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b">
              <tr>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Members</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Status</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Created</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
              </tr>
            </thead>
            <tbody>
              {alliances.map((a) => (
                <tr key={a.allianceId} className="border-b border-border">
                  <td className="py-2 px-3 font-medium">
                    <a href={`/alliances/${a.allianceId}`} className="text-primary hover:underline">
                      {a.name}
                    </a>
                  </td>
                  <td className="py-2 px-3">{a.memberCount}</td>
                  <td className="py-2 px-3">
                    {a.isAtWar ? (
                      <span className="rounded bg-red-800/60 px-1.5 py-0.5 text-xs text-red-200">
                        At War
                      </span>
                    ) : (
                      <span className="rounded bg-green-800/60 px-1.5 py-0.5 text-xs text-green-200">
                        Peaceful
                      </span>
                    )}
                  </td>
                  <td className="py-2 px-3 text-xs text-muted-foreground">
                    {new Date(a.created).toLocaleDateString()}
                  </td>
                  <td className="py-2 px-3">
                    {myStatus && !myStatus.isMember && (
                      <button
                        onClick={() => {
                          setJoiningAlliance(a)
                          setJoinPassword('')
                          setError(null)
                        }}
                        className="rounded border border-primary px-2 py-0.5 text-xs text-primary hover:bg-primary/10"
                      >
                        Join
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {joiningAlliance && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Join {joiningAlliance.name}</strong>
          </div>
          <div className="p-4 space-y-3">
            <input
              type="password"
              className="w-full max-w-sm rounded border bg-input px-2 py-1.5 text-sm"
              placeholder="Password"
              value={joinPassword}
              onChange={(e) => setJoinPassword(e.target.value)}
            />
            <div className="flex gap-2">
              <button
                onClick={() =>
                  joinMutation.mutate({
                    allianceId: joiningAlliance.allianceId,
                    req: { password: joinPassword },
                  })
                }
                disabled={joinMutation.isPending}
                className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
              >
                {joinMutation.isPending ? 'Joining…' : 'Join'}
              </button>
              <button
                onClick={() => setJoiningAlliance(null)}
                className="text-sm text-muted-foreground hover:text-foreground"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
