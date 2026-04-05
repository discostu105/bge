import { useState, useEffect, useRef } from 'react'
import { useParams, Link } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
	ShieldIcon,
	CrownIcon,
	SwordsIcon,
	SendIcon,
	UserPlusIcon,
	VoteIcon,
} from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
	AllianceDetailViewModel,
	MyAllianceStatusViewModel,
	AllianceChatPostViewModel,
	AllianceWarViewModel,
	AllianceViewModel,
	PublicPlayerViewModel,
} from '@/api/types'
import { cn, relativeTime } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

export function AllianceDetail() {
	const { allianceId } = useParams<{ allianceId: string }>()
	const queryClient = useQueryClient()
	const [error, setError] = useState<string | null>(null)
	const [success, setSuccess] = useState<string | null>(null)

	// Leader controls state
	const [editMessage, setEditMessage] = useState(false)
	const [messageText, setMessageText] = useState('')
	const [editPassword, setEditPassword] = useState(false)
	const [newPassword, setNewPassword] = useState('')
	const [showInvite, setShowInvite] = useState(false)
	const [invitePlayerId, setInvitePlayerId] = useState('')
	const [showDeclareWar, setShowDeclareWar] = useState(false)
	const [warTargetId, setWarTargetId] = useState('')

	// Chat state
	const [chatBody, setChatBody] = useState('')
	const chatEndRef = useRef<HTMLDivElement>(null)

	// Vote state
	const [votePlayerId, setVotePlayerId] = useState('')

	const { data: detail, isLoading, error: detailError, refetch: refetchDetail } = useQuery<AllianceDetailViewModel>({
		queryKey: ['alliance-detail', allianceId],
		queryFn: () => apiClient.get(`/api/alliances/${allianceId}`).then((r) => r.data),
		refetchInterval: 10_000,
	})

	const { data: myStatus } = useQuery<MyAllianceStatusViewModel>({
		queryKey: ['alliance-status-detail'],
		queryFn: () => apiClient.get('/api/alliances/my-status').then((r) => r.data),
		refetchInterval: 10_000,
	})

	const isMember = myStatus?.allianceId === allianceId && myStatus?.isMember && !myStatus?.isPending
	const isLeader = myStatus?.allianceId === allianceId && myStatus?.isLeader

	const { data: posts } = useQuery<AllianceChatPostViewModel[]>({
		queryKey: ['alliance-chat', allianceId],
		queryFn: () => apiClient.get(`/api/alliances/${allianceId}/posts`).then((r) => r.data),
		refetchInterval: 5_000,
		enabled: !!isMember,
	})

	const { data: wars } = useQuery<AllianceWarViewModel[]>({
		queryKey: ['alliance-wars', allianceId],
		queryFn: () => apiClient.get(`/api/alliances/${allianceId}/wars`).then((r) => r.data),
		refetchInterval: 10_000,
	})

	const { data: allAlliances } = useQuery<AllianceViewModel[]>({
		queryKey: ['alliances-for-war'],
		queryFn: () => apiClient.get('/api/alliances').then((r) => r.data),
		enabled: !!isLeader && showDeclareWar,
	})

	const { data: allPlayers } = useQuery<PublicPlayerViewModel[]>({
		queryKey: ['players-for-invite'],
		queryFn: () => apiClient.get('/api/playerranking').then((r) => r.data),
		enabled: !!isLeader && showInvite,
	})

	// Scroll chat on new posts
	useEffect(() => {
		chatEndRef.current?.scrollIntoView({ behavior: 'smooth' })
	}, [posts])

	// Sync message text when detail loads
	useEffect(() => {
		if (detail?.message && !editMessage) setMessageText(detail.message)
	}, [detail?.message])

	function invalidateAll() {
		void queryClient.invalidateQueries({ queryKey: ['alliance-detail', allianceId] })
		void queryClient.invalidateQueries({ queryKey: ['alliance-chat', allianceId] })
		void queryClient.invalidateQueries({ queryKey: ['alliance-wars', allianceId] })
		void queryClient.invalidateQueries({ queryKey: ['alliance-status-detail'] })
	}

	function handleError(err: unknown) {
		if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Request failed')
		else setError('Request failed')
	}

	// --- Mutations ---

	const acceptMemberMut = useMutation({
		mutationFn: (pid: string) =>
			apiClient.post(`/api/alliances/${allianceId}/members/${pid}/accept`),
		onSuccess: () => { setSuccess('Member accepted.'); invalidateAll() },
		onError: handleError,
	})

	const rejectMemberMut = useMutation({
		mutationFn: (pid: string) =>
			apiClient.post(`/api/alliances/${allianceId}/members/${pid}/reject`),
		onSuccess: () => { setSuccess('Member rejected.'); invalidateAll() },
		onError: handleError,
	})

	const kickMemberMut = useMutation({
		mutationFn: (pid: string) =>
			apiClient.delete(`/api/alliances/${allianceId}/members/${pid}`),
		onSuccess: () => { setSuccess('Member kicked.'); invalidateAll() },
		onError: handleError,
	})

	const voteMut = useMutation({
		mutationFn: (voteePlayerId: string) =>
			apiClient.post(`/api/alliances/${allianceId}/leader-vote`, { voteePlayerId }),
		onSuccess: () => { setSuccess('Vote cast!'); invalidateAll() },
		onError: handleError,
	})

	const setMessageMut = useMutation({
		mutationFn: (message: string) =>
			apiClient.patch(`/api/alliances/${allianceId}/message`, { message }),
		onSuccess: () => { setSuccess('Message updated.'); setEditMessage(false); invalidateAll() },
		onError: handleError,
	})

	const setPasswordMut = useMutation({
		mutationFn: (pw: string) =>
			apiClient.patch(`/api/alliances/${allianceId}/password`, { newPassword: pw }),
		onSuccess: () => { setSuccess('Password changed.'); setEditPassword(false); setNewPassword(''); invalidateAll() },
		onError: handleError,
	})

	const inviteMut = useMutation({
		mutationFn: (targetPlayerId: string) =>
			apiClient.post(`/api/alliances/${allianceId}/invite`, { targetPlayerId }),
		onSuccess: () => { setSuccess('Invite sent!'); setShowInvite(false); setInvitePlayerId(''); invalidateAll() },
		onError: handleError,
	})

	const declareWarMut = useMutation({
		mutationFn: (targetAllianceId: string) =>
			apiClient.post(`/api/alliances/${allianceId}/declare-war`, { targetAllianceId }),
		onSuccess: () => { setSuccess('War declared!'); setShowDeclareWar(false); setWarTargetId(''); invalidateAll() },
		onError: handleError,
	})

	const peaceMut = useMutation({
		mutationFn: (warId: string) =>
			apiClient.post(`/api/alliances/wars/${warId}/peace`, { warId }),
		onSuccess: () => { setSuccess('Peace action sent.'); invalidateAll() },
		onError: handleError,
	})

	const postChatMut = useMutation({
		mutationFn: (body: string) =>
			apiClient.post(`/api/alliances/${allianceId}/posts`, { body }),
		onSuccess: () => {
			setChatBody('')
			void queryClient.invalidateQueries({ queryKey: ['alliance-chat', allianceId] })
		},
		onError: handleError,
	})

	if (!allianceId) return null
	if (isLoading) return <PageLoader message="Loading alliance..." />
	if (detailError) return <ApiError message="Failed to load alliance." onRetry={() => void refetchDetail()} />
	if (!detail) return <div className="text-destructive text-sm p-4">Alliance not found.</div>

	const confirmedMembers = detail.members.filter((m) => !m.isPending)
	const pendingMembers = detail.members.filter((m) => m.isPending)

	return (
		<div className="space-y-6 max-w-3xl">
			{/* Header */}
			<div>
				<Link to="/alliances" className="text-xs text-muted-foreground hover:text-foreground">
					← All Alliances
				</Link>
				<h1 className="text-2xl font-bold flex items-center gap-2 mt-1">
					<ShieldIcon className="h-6 w-6 text-primary" />
					{detail.name}
				</h1>
				<div className="text-sm text-muted-foreground mt-1">
					Founded {relativeTime(detail.created)}
				</div>
			</div>

			{error && <div className="text-destructive text-sm">{error}</div>}
			{success && <div className="text-green-400 text-sm">{success}</div>}

			{/* Alliance Message */}
			<div className="rounded-lg border bg-card p-4">
				<div className="flex items-center justify-between mb-2">
					<strong className="text-sm text-muted-foreground">Alliance Message</strong>
					{isLeader && !editMessage && (
						<button
							onClick={() => { setEditMessage(true); setMessageText(detail.message ?? '') }}
							className="text-xs text-primary hover:underline"
						>
							Edit
						</button>
					)}
				</div>
				{editMessage ? (
					<div className="space-y-2">
						<textarea
							value={messageText}
							onChange={(e) => setMessageText(e.target.value)}
							className="w-full rounded border bg-input px-2 py-1.5 text-sm resize-none"
							rows={3}
							maxLength={200}
						/>
						<div className="flex gap-2">
							<button
								onClick={() => { setError(null); setMessageMut.mutate(messageText) }}
								disabled={setMessageMut.isPending}
								className="rounded bg-primary px-3 py-1 text-xs text-primary-foreground hover:opacity-90 disabled:opacity-50"
							>
								Save
							</button>
							<button onClick={() => setEditMessage(false)} className="text-xs text-muted-foreground hover:text-foreground">
								Cancel
							</button>
						</div>
					</div>
				) : (
					<p className="text-sm">{detail.message || 'No message set.'}</p>
				)}
			</div>

			{/* Members */}
			<div className="rounded-lg border bg-card">
				<div className="border-b px-4 py-3 flex items-center justify-between">
					<strong className="text-sm">Members ({confirmedMembers.length})</strong>
					{isMember && (
						<div className="flex items-center gap-2">
							{/* Vote for leader */}
							<select
								value={votePlayerId}
								onChange={(e) => setVotePlayerId(e.target.value)}
								className="rounded border bg-input px-2 py-1 text-xs"
							>
								<option value="">Vote for leader…</option>
								{confirmedMembers.map((m) => (
									<option key={m.playerId} value={m.playerId}>{m.playerName}</option>
								))}
							</select>
							<button
								onClick={() => {
									if (votePlayerId) {
										setError(null); setSuccess(null)
										voteMut.mutate(votePlayerId)
									}
								}}
								disabled={!votePlayerId || voteMut.isPending}
								className="rounded bg-secondary px-2 py-1 text-xs hover:opacity-90 disabled:opacity-50 flex items-center gap-1"
							>
								<VoteIcon className="h-3 w-3" />
								Vote
							</button>
						</div>
					)}
				</div>
				<div className="overflow-x-auto">
					<table className="w-full text-sm">
						<thead className="border-b">
							<tr>
								<th className="py-2 px-3 text-left font-medium text-muted-foreground">Player</th>
								<th className="py-2 px-3 text-left font-medium text-muted-foreground">Votes</th>
								<th className="py-2 px-3 text-left font-medium text-muted-foreground">Joined</th>
								{isLeader && <th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>}
							</tr>
						</thead>
						<tbody>
							{confirmedMembers.map((m) => (
								<tr key={m.playerId} className="border-b border-border">
									<td className="py-2 px-3 font-medium flex items-center gap-1.5">
										{m.isLeader && <CrownIcon className="h-3.5 w-3.5 text-amber-400" />}
										{m.playerName}
									</td>
									<td className="py-2 px-3">{m.voteCount}</td>
									<td className="py-2 px-3 text-xs text-muted-foreground">{relativeTime(m.joinedAt)}</td>
									{isLeader && (
										<td className="py-2 px-3">
											{!m.isLeader && (
												<button
													onClick={() => { setError(null); kickMemberMut.mutate(m.playerId) }}
													className="text-xs text-destructive hover:underline"
												>
													Kick
												</button>
											)}
										</td>
									)}
								</tr>
							))}
						</tbody>
					</table>
				</div>
			</div>

			{/* Pending Members (leader only) */}
			{isLeader && pendingMembers.length > 0 && (
				<div className="rounded-lg border border-yellow-700 bg-yellow-900/10">
					<div className="border-b border-yellow-700 bg-yellow-900/20 px-4 py-3">
						<strong className="text-sm text-yellow-200">
							Pending Members ({pendingMembers.length})
						</strong>
					</div>
					<div className="divide-y divide-yellow-700/30">
						{pendingMembers.map((m) => (
							<div key={m.playerId} className="flex items-center justify-between px-4 py-2">
								<span className="font-medium text-sm">{m.playerName}</span>
								<div className="flex gap-2">
									<button
										onClick={() => { setError(null); acceptMemberMut.mutate(m.playerId) }}
										className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90"
									>
										Accept
									</button>
									<button
										onClick={() => { setError(null); rejectMemberMut.mutate(m.playerId) }}
										className="rounded border border-destructive px-2 py-0.5 text-xs text-destructive hover:bg-destructive/10"
									>
										Reject
									</button>
								</div>
							</div>
						))}
					</div>
				</div>
			)}

			{/* Leader Controls */}
			{isLeader && (
				<div className="rounded-lg border bg-card">
					<div className="border-b px-4 py-3">
						<strong className="text-sm">Leader Controls</strong>
					</div>
					<div className="p-4 flex flex-wrap gap-2">
						<button
							onClick={() => { setShowInvite(!showInvite); setShowDeclareWar(false); setEditPassword(false) }}
							className={cn(
								'rounded px-3 py-1.5 text-xs flex items-center gap-1',
								showInvite ? 'bg-primary text-primary-foreground' : 'border border-primary text-primary'
							)}
						>
							<UserPlusIcon className="h-3.5 w-3.5" />
							Invite Player
						</button>
						<button
							onClick={() => { setShowDeclareWar(!showDeclareWar); setShowInvite(false); setEditPassword(false) }}
							className={cn(
								'rounded px-3 py-1.5 text-xs flex items-center gap-1',
								showDeclareWar ? 'bg-red-600 text-white' : 'border border-red-600 text-red-400'
							)}
						>
							<SwordsIcon className="h-3.5 w-3.5" />
							Declare War
						</button>
						<button
							onClick={() => { setEditPassword(!editPassword); setShowInvite(false); setShowDeclareWar(false) }}
							className={cn(
								'rounded px-3 py-1.5 text-xs',
								editPassword ? 'bg-secondary text-secondary-foreground' : 'border text-muted-foreground hover:text-foreground'
							)}
						>
							Change Password
						</button>
					</div>

					{/* Invite Player Panel */}
					{showInvite && (
						<div className="border-t p-4 space-y-3">
							<label className="block text-sm text-muted-foreground">Select a player to invite</label>
							{allPlayers ? (
								<select
									value={invitePlayerId}
									onChange={(e) => setInvitePlayerId(e.target.value)}
									className="w-full max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
								>
									<option value="">— Select a player —</option>
									{allPlayers
										.filter((p) => !detail.members.some((m) => m.playerId === p.playerId))
										.map((p) => (
											<option key={p.playerId ?? ""} value={p.playerId ?? ""}>{p.playerName}</option>
										))}
								</select>
							) : (
								<div className="text-xs text-muted-foreground">Loading players…</div>
							)}
							<button
								onClick={() => {
									if (invitePlayerId) { setError(null); inviteMut.mutate(invitePlayerId) }
								}}
								disabled={!invitePlayerId || inviteMut.isPending}
								className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
							>
								{inviteMut.isPending ? 'Sending…' : 'Send Invite'}
							</button>
						</div>
					)}

					{/* Declare War Panel */}
					{showDeclareWar && (
						<div className="border-t p-4 space-y-3">
							<label className="block text-sm text-muted-foreground">Select an alliance to declare war on</label>
							{allAlliances ? (
								<select
									value={warTargetId}
									onChange={(e) => setWarTargetId(e.target.value)}
									className="w-full max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
								>
									<option value="">— Select an alliance —</option>
									{allAlliances
										.filter((a) => a.allianceId !== allianceId)
										.map((a) => (
											<option key={a.allianceId} value={a.allianceId}>{a.name}</option>
										))}
								</select>
							) : (
								<div className="text-xs text-muted-foreground">Loading alliances…</div>
							)}
							<button
								onClick={() => {
									if (warTargetId) { setError(null); declareWarMut.mutate(warTargetId) }
								}}
								disabled={!warTargetId || declareWarMut.isPending}
								className="rounded bg-red-600 px-4 py-1.5 text-sm text-white hover:opacity-90 disabled:opacity-50"
							>
								{declareWarMut.isPending ? 'Declaring…' : 'Declare War'}
							</button>
						</div>
					)}

					{/* Change Password Panel */}
					{editPassword && (
						<div className="border-t p-4 space-y-3">
							<label className="block text-sm text-muted-foreground">New alliance password</label>
							<input
								type="password"
								value={newPassword}
								onChange={(e) => setNewPassword(e.target.value)}
								className="w-full max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
							/>
							<button
								onClick={() => {
									if (newPassword.trim()) { setError(null); setPasswordMut.mutate(newPassword) }
								}}
								disabled={!newPassword.trim() || setPasswordMut.isPending}
								className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
							>
								{setPasswordMut.isPending ? 'Saving…' : 'Update Password'}
							</button>
						</div>
					)}
				</div>
			)}

			{/* Wars */}
			{(wars?.length ?? 0) > 0 && (
				<div className="rounded-lg border border-red-700/50 bg-red-900/10">
					<div className="border-b border-red-700/50 bg-red-900/20 px-4 py-3">
						<strong className="text-sm text-red-200 flex items-center gap-1.5">
							<SwordsIcon className="h-4 w-4" />
							Active Wars ({wars!.length})
						</strong>
					</div>
					<div className="divide-y divide-red-700/30">
						{wars!.map((w) => {
							const isAttacker = w.attackerAllianceId === allianceId
							const opponent = isAttacker ? w.defenderAllianceName : w.attackerAllianceName
							return (
								<div key={w.warId} className="flex items-center justify-between px-4 py-3">
									<div>
										<span className="font-medium text-sm">vs {opponent}</span>
										<span className="ml-2 text-xs text-muted-foreground">
											declared {relativeTime(w.declaredAt)}
										</span>
										{w.status === 'PeaceProposed' && (
											<span className="ml-2 rounded bg-yellow-700/30 px-2 py-0.5 text-xs text-yellow-300">
												Peace Proposed
											</span>
										)}
									</div>
									{isLeader && w.status === 'Active' && (
										<button
											onClick={() => { setError(null); peaceMut.mutate(w.warId) }}
											disabled={peaceMut.isPending}
											className="rounded border border-green-600 px-2 py-0.5 text-xs text-green-400 hover:bg-green-600/10"
										>
											Propose Peace
										</button>
									)}
									{isLeader && w.status === 'PeaceProposed' && w.proposerAllianceId !== allianceId && (
										<button
											onClick={() => { setError(null); peaceMut.mutate(w.warId) }}
											disabled={peaceMut.isPending}
											className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90"
										>
											Accept Peace
										</button>
									)}
									{w.status === 'PeaceProposed' && w.proposerAllianceId === allianceId && (
										<span className="text-xs text-muted-foreground">Awaiting response…</span>
									)}
								</div>
							)
						})}
					</div>
				</div>
			)}

			{/* Alliance Chat (members only) */}
			{isMember && (
				<div className="rounded-lg border bg-card">
					<div className="border-b px-4 py-3">
						<strong className="text-sm">Alliance Chat</strong>
					</div>
					<div className="h-64 overflow-y-auto p-2 space-y-1.5">
						{!posts || posts.length === 0 ? (
							<p className="text-center text-muted-foreground text-sm mt-8">No messages yet.</p>
						) : (
							posts.map((p) => (
								<div key={p.postId} className="rounded-md border-l-2 border-primary/60 bg-secondary/30 px-3 py-2">
									<div className="flex justify-between text-xs mb-0.5">
										<span className="font-semibold text-primary">{p.authorName}</span>
										<span className="text-muted-foreground">
											{new Date(p.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
										</span>
									</div>
									<p className="text-sm">{p.body}</p>
								</div>
							))
						)}
						<div ref={chatEndRef} />
					</div>
					<div className="border-t px-3 py-2 flex gap-2">
						<input
							className="flex-1 rounded-md border bg-input px-3 py-1.5 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring"
							placeholder="Type a message…"
							maxLength={500}
							value={chatBody}
							onChange={(e) => setChatBody(e.target.value)}
							onKeyDown={(e) => {
								if (e.key === 'Enter' && !e.shiftKey && chatBody.trim()) {
									e.preventDefault()
									postChatMut.mutate(chatBody.trim())
								}
							}}
						/>
						<button
							onClick={() => { if (chatBody.trim()) postChatMut.mutate(chatBody.trim()) }}
							disabled={!chatBody.trim() || postChatMut.isPending}
							className="flex items-center gap-1 rounded-md bg-primary px-3 py-1.5 text-sm text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
						>
							<SendIcon className="h-4 w-4" />
							Send
						</button>
					</div>
				</div>
			)}
		</div>
	)
}
