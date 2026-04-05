import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  AllianceDetailViewModel,
  MyAllianceStatusViewModel,
  AllianceChatPostViewModel,
  AllianceWarViewModel,
  AllianceViewModel,
  PublicPlayerViewModel,
  VoteLeaderRequest,
  SetAllianceMessageRequest,
  SetAlliancePasswordRequest,
  PostAllianceChatRequest,
  InvitePlayerRequest,
  DeclareWarRequest,
  PeaceRequest,
} from '@/api/types'

interface AllianceDetailProps {
  allianceId: string
}

export function AllianceDetail({ allianceId }: AllianceDetailProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [newMessage, setNewMessage] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [newChatBody, setNewChatBody] = useState('')
  const [invitePlayerId, setInvitePlayerId] = useState('')
  const [inviteResult, setInviteResult] = useState<string | null>(null)
  const [warTargetAllianceId, setWarTargetAllianceId] = useState('')
  const [warResult, setWarResult] = useState<string | null>(null)

  const invalidateDetail = () => {
    void queryClient.invalidateQueries({ queryKey: ['alliance-detail', allianceId] })
    void queryClient.invalidateQueries({ queryKey: ['alliance-status', allianceId] })
    void queryClient.invalidateQueries({ queryKey: ['alliance-chat', allianceId] })
    void queryClient.invalidateQueries({ queryKey: ['alliance-wars', allianceId] })
  }

  const { data: detail } = useQuery<AllianceDetailViewModel>({
    queryKey: ['alliance-detail', allianceId],
    queryFn: () => apiClient.get(`/api/alliances/${allianceId}`).then((r) => r.data),
    refetchInterval: 10_000,
  })

  const { data: myStatus } = useQuery<MyAllianceStatusViewModel>({
    queryKey: ['alliance-status', allianceId],
    queryFn: () => apiClient.get('/api/alliances/my-status').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const { data: chatPosts } = useQuery<AllianceChatPostViewModel[]>({
    queryKey: ['alliance-chat', allianceId],
    queryFn: () =>
      apiClient.get(`/api/alliances/${allianceId}/posts`).then((r) => r.data),
    enabled: myStatus?.isMember === true && myStatus.isPending === false,
    refetchInterval: 10_000,
  })

  const { data: wars, refetch: refetchWars } = useQuery<AllianceWarViewModel[]>({
    queryKey: ['alliance-wars', allianceId],
    queryFn: () =>
      apiClient
        .get<AllianceWarViewModel[]>(`/api/alliances/${allianceId}/wars`)
        .then((r) => r.data)
        .catch(() => []),
    refetchInterval: 15_000,
  })

  const { data: allPlayers } = useQuery<PublicPlayerViewModel[]>({
    queryKey: ['playerranking'],
    queryFn: () =>
      apiClient
        .get<PublicPlayerViewModel[]>('/api/playerranking')
        .then((r) => r.data)
        .catch(() => []),
    enabled: myStatus?.isLeader === true && myStatus.isPending === false,
    refetchInterval: 60_000,
  })

  const { data: allAlliances } = useQuery<AllianceViewModel[]>({
    queryKey: ['alliances-list'],
    queryFn: () =>
      apiClient
        .get<AllianceViewModel[]>('/api/alliances')
        .then((r) => r.data)
        .catch(() => []),
    enabled: myStatus?.isLeader === true && myStatus.isPending === false,
    refetchInterval: 60_000,
  })

  const withError = (err: unknown) => {
    if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Request failed')
  }

  const acceptMember = useMutation({
    mutationFn: (playerId: string) =>
      apiClient.post(`/api/alliances/${allianceId}/members/${playerId}/accept`, ''),
    onSuccess: () => { setError(null); invalidateDetail() },
    onError: withError,
  })

  const rejectMember = useMutation({
    mutationFn: (playerId: string) =>
      apiClient.post(`/api/alliances/${allianceId}/members/${playerId}/reject`, ''),
    onSuccess: () => { setError(null); invalidateDetail() },
    onError: withError,
  })

  const kickMember = useMutation({
    mutationFn: (playerId: string) =>
      apiClient.delete(`/api/alliances/${allianceId}/members/${playerId}`),
    onSuccess: () => { setError(null); invalidateDetail() },
    onError: withError,
  })

  const voteMutation = useMutation({
    mutationFn: (req: VoteLeaderRequest) =>
      apiClient.post(`/api/alliances/${allianceId}/leader-vote`, req),
    onSuccess: () => { setError(null); invalidateDetail() },
    onError: withError,
  })

  const messageMutation = useMutation({
    mutationFn: (req: SetAllianceMessageRequest) =>
      apiClient.patch(`/api/alliances/${allianceId}/message`, req),
    onSuccess: () => { setError(null); setNewMessage(''); invalidateDetail() },
    onError: withError,
  })

  const passwordMutation = useMutation({
    mutationFn: (req: SetAlliancePasswordRequest) =>
      apiClient.patch(`/api/alliances/${allianceId}/password`, req),
    onSuccess: () => { setError(null); setNewPassword('') },
    onError: withError,
  })

  const chatMutation = useMutation({
    mutationFn: (req: PostAllianceChatRequest) =>
      apiClient.post(`/api/alliances/${allianceId}/posts`, req),
    onSuccess: () => {
      setError(null)
      setNewChatBody('')
      void queryClient.invalidateQueries({ queryKey: ['alliance-chat', allianceId] })
    },
    onError: withError,
  })

  const inviteMutation = useMutation({
    mutationFn: (req: InvitePlayerRequest) =>
      apiClient.post(`/api/alliances/${allianceId}/invite`, req),
    onSuccess: () => {
      setInviteResult('Invite sent.')
      setInvitePlayerId('')
    },
    onError: (err) => {
      if (axios.isAxiosError(err))
        setInviteResult(`Error: ${(err.response?.data as string) ?? 'failed'}`)
    },
  })

  const declareWarMutation = useMutation({
    mutationFn: (req: DeclareWarRequest) =>
      apiClient.post(`/api/alliances/${allianceId}/declare-war`, req),
    onSuccess: () => {
      setWarResult('War declared.')
      setWarTargetAllianceId('')
      void refetchWars()
    },
    onError: (err) => {
      if (axios.isAxiosError(err))
        setWarResult(`Error: ${(err.response?.data as string) ?? 'failed'}`)
    },
  })

  const peaceMutation = useMutation({
    mutationFn: (req: PeaceRequest) =>
      apiClient.post(`/api/alliances/wars/${req.warId}/peace`, req),
    onSuccess: () => { setError(null); void refetchWars() },
    onError: withError,
  })

  const isLeader = myStatus?.isMember && myStatus.isLeader && !myStatus.isPending
  const isActiveMember = myStatus?.isMember && !myStatus.isPending

  if (!detail || !myStatus) {
    return <div className="text-muted-foreground text-sm">Loading...</div>
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Alliance: {detail.name}</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}

      {detail.message && (
        <div className="rounded-lg border border-blue-700 bg-blue-900/10 px-4 py-3 text-sm text-blue-200">
          {detail.message}
        </div>
      )}

      <p className="text-sm text-muted-foreground">
        Founded: {new Date(detail.created).toLocaleDateString()}
      </p>

      {/* Members table */}
      <div className="rounded-lg border bg-card">
        <div className="border-b px-4 py-3">
          <strong className="text-sm">Members</strong>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b">
              <tr>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Status</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground">Votes</th>
                <th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
              </tr>
            </thead>
            <tbody>
              {detail.members.map((m) => (
                <tr key={m.playerId} className="border-b border-border">
                  <td className="py-2 px-3">
                    {m.playerName} {m.isLeader ? '★' : ''}
                  </td>
                  <td className="py-2 px-3 text-muted-foreground">
                    {m.isPending ? 'Pending' : 'Member'}
                  </td>
                  <td className="py-2 px-3">{m.voteCount}</td>
                  <td className="py-2 px-3">
                    <div className="flex gap-2">
                      {isLeader && m.isPending && (
                        <>
                          <button
                            onClick={() => acceptMember.mutate(m.playerId)}
                            className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90"
                          >
                            Accept
                          </button>
                          <button
                            onClick={() => rejectMember.mutate(m.playerId)}
                            className="rounded bg-destructive px-2 py-0.5 text-xs text-destructive-foreground hover:opacity-90"
                          >
                            Reject
                          </button>
                        </>
                      )}
                      {isLeader && !m.isPending && !m.isLeader && (
                        <button
                          onClick={() => kickMember.mutate(m.playerId)}
                          className="rounded border border-yellow-600 px-2 py-0.5 text-xs text-yellow-400 hover:bg-yellow-600/10"
                        >
                          Kick
                        </button>
                      )}
                      {isActiveMember && !m.isPending && m.playerId !== myStatus.allianceId && (
                        <button
                          onClick={() => voteMutation.mutate({ voteePlayerId: m.playerId })}
                          className="rounded border border-primary px-2 py-0.5 text-xs text-primary hover:bg-primary/10"
                        >
                          Vote Leader
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Leader Controls */}
      {isLeader && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Leader Controls</strong>
          </div>
          <div className="p-4 space-y-4">
            <div>
              <label className="block text-sm text-muted-foreground mb-1">Alliance Message</label>
              <div className="flex gap-2">
                <input
                  className="flex-1 max-w-sm rounded border bg-input px-2 py-1.5 text-sm"
                  placeholder="Set message..."
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                />
                <button
                  onClick={() => messageMutation.mutate({ message: newMessage })}
                  disabled={messageMutation.isPending}
                  className="rounded bg-primary px-3 py-1.5 text-xs text-primary-foreground hover:opacity-90 disabled:opacity-50"
                >
                  Save
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm text-muted-foreground mb-1">Change Password</label>
              <div className="flex gap-2">
                <input
                  type="password"
                  className="flex-1 max-w-sm rounded border bg-input px-2 py-1.5 text-sm"
                  placeholder="New password..."
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                />
                <button
                  onClick={() => passwordMutation.mutate({ newPassword })}
                  disabled={passwordMutation.isPending}
                  className="rounded bg-secondary px-3 py-1.5 text-xs text-secondary-foreground hover:opacity-90 disabled:opacity-50"
                >
                  Change
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm text-muted-foreground mb-1">Invite Player</label>
              <div className="flex gap-2">
                <select
                  value={invitePlayerId}
                  onChange={(e) => setInvitePlayerId(e.target.value)}
                  className="flex-1 max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
                >
                  <option value="">— Select a player —</option>
                  {(allPlayers ?? []).map((p) => (
                    <option key={p.playerId} value={p.playerId}>
                      {p.playerName}
                    </option>
                  ))}
                </select>
                <button
                  onClick={() => {
                    setInviteResult(null)
                    if (invitePlayerId) inviteMutation.mutate({ targetPlayerId: invitePlayerId })
                  }}
                  disabled={!invitePlayerId || inviteMutation.isPending}
                  className="rounded border border-green-600 px-3 py-1.5 text-xs text-green-400 hover:bg-green-600/10 disabled:opacity-50"
                >
                  Invite
                </button>
              </div>
              {inviteResult && (
                <div
                  className={`text-xs mt-1 ${inviteResult.startsWith('Error') ? 'text-destructive' : 'text-green-400'}`}
                >
                  {inviteResult}
                </div>
              )}
            </div>

            <div>
              <label className="block text-sm text-muted-foreground mb-1">Declare War</label>
              <div className="flex gap-2">
                <select
                  value={warTargetAllianceId}
                  onChange={(e) => setWarTargetAllianceId(e.target.value)}
                  className="flex-1 max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
                >
                  <option value="">— Select an alliance —</option>
                  {(allAlliances ?? [])
                    .filter((a) => a.allianceId !== allianceId)
                    .map((a) => (
                      <option key={a.allianceId} value={a.allianceId}>
                        {a.name}
                      </option>
                    ))}
                </select>
                <button
                  onClick={() => {
                    setWarResult(null)
                    if (warTargetAllianceId)
                      declareWarMutation.mutate({ targetAllianceId: warTargetAllianceId })
                  }}
                  disabled={!warTargetAllianceId || declareWarMutation.isPending}
                  className="rounded border border-destructive px-3 py-1.5 text-xs text-destructive hover:bg-destructive/10 disabled:opacity-50"
                >
                  Declare War
                </button>
              </div>
              {warResult && (
                <div
                  className={`text-xs mt-1 ${warResult.startsWith('Error') ? 'text-destructive' : 'text-green-400'}`}
                >
                  {warResult}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Wars */}
      {(wars?.length ?? 0) > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Wars</strong>
          </div>
          <div className="p-3 space-y-2">
            {wars!.map((war) => {
              const opponent =
                war.attackerAllianceId === allianceId
                  ? war.defenderAllianceName
                  : war.attackerAllianceName
              const peaceOfferedToUs =
                war.status === 'PeaceProposed' && war.proposerAllianceId !== allianceId
              return (
                <div
                  key={war.warId}
                  className="flex items-center justify-between rounded border px-3 py-2"
                >
                  <div className="text-sm">
                    <span
                      className={`rounded px-1.5 py-0.5 text-xs mr-2 ${
                        war.status === 'PeaceProposed'
                          ? 'bg-yellow-700/50 text-yellow-200'
                          : 'bg-red-800/60 text-red-200'
                      }`}
                    >
                      {war.status}
                    </span>
                    vs <strong>{opponent}</strong>
                    <span className="text-muted-foreground ml-2 text-xs">
                      since {new Date(war.declaredAt).toLocaleDateString()}
                    </span>
                    {peaceOfferedToUs && (
                      <span className="ml-2 rounded bg-blue-700/50 px-1.5 py-0.5 text-xs text-blue-200">
                        Peace offered to us
                      </span>
                    )}
                  </div>
                  {isLeader && (
                    <button
                      onClick={() => peaceMutation.mutate({ warId: war.warId })}
                      disabled={peaceMutation.isPending}
                      className="rounded border border-yellow-600 px-2 py-0.5 text-xs text-yellow-400 hover:bg-yellow-600/10 disabled:opacity-50"
                    >
                      {peaceOfferedToUs ? 'Accept Peace' : 'Propose Peace'}
                    </button>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {/* Chat */}
      {isActiveMember && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Alliance Chat</strong>
          </div>
          <div className="p-4 space-y-3">
            <div className="flex gap-2">
              <input
                className="flex-1 rounded border bg-input px-2 py-1.5 text-sm"
                placeholder="Write a message..."
                value={newChatBody}
                onChange={(e) => setNewChatBody(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newChatBody.trim())
                    chatMutation.mutate({ body: newChatBody })
                }}
              />
              <button
                onClick={() => {
                  if (newChatBody.trim()) chatMutation.mutate({ body: newChatBody })
                }}
                disabled={!newChatBody.trim() || chatMutation.isPending}
                className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
              >
                Send
              </button>
            </div>
          </div>
          {!chatPosts ? (
            <div className="px-4 pb-4 text-muted-foreground text-sm">Loading chat...</div>
          ) : chatPosts.length === 0 ? (
            <div className="px-4 pb-4 text-muted-foreground text-sm">
              No messages yet. Be the first to post!
            </div>
          ) : (
            <div className="divide-y">
              {chatPosts.map((post) => (
                <div key={post.postId} className="px-4 py-3">
                  <div className="flex items-center justify-between mb-1">
                    <strong className="text-sm">{post.authorName}</strong>
                    <span className="text-xs text-muted-foreground">
                      {new Date(post.createdAt).toLocaleString()}
                    </span>
                  </div>
                  <p className="text-sm">{post.body}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
