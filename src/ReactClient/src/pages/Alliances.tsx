import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import { ShieldIcon, SwordsIcon, UsersIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
	AllianceViewModel,
	MyAllianceStatusViewModel,
	AllianceInviteViewModel,
	CreateAllianceRequest,
	JoinAllianceRequest,
} from '@/api/types'
import { relativeTime } from '@/lib/utils'

interface AlliancesProps {
	gameId: string
}

export function Alliances({ gameId }: AlliancesProps) {
	const queryClient = useQueryClient()
	const [error, setError] = useState<string | null>(null)
	const [success, setSuccess] = useState<string | null>(null)

	// Create alliance form
	const [showCreate, setShowCreate] = useState(false)
	const [newName, setNewName] = useState('')
	const [newPassword, setNewPassword] = useState('')

	// Join alliance modal
	const [joinAllianceId, setJoinAllianceId] = useState<string | null>(null)
	const [joinPassword, setJoinPassword] = useState('')

	const { data: myStatus } = useQuery<MyAllianceStatusViewModel>({
		queryKey: ['alliance-status', gameId],
		queryFn: () => apiClient.get('/api/alliances/my-status').then((r) => r.data),
		refetchInterval: 10_000,
	})

	const { data: alliances } = useQuery<AllianceViewModel[]>({
		queryKey: ['alliances', gameId],
		queryFn: () => apiClient.get('/api/alliances').then((r) => r.data),
		refetchInterval: 10_000,
	})

	const { data: invites } = useQuery<AllianceInviteViewModel[]>({
		queryKey: ['alliance-invites', gameId],
		queryFn: () => apiClient.get('/api/alliances/my-invites').then((r) => r.data),
		refetchInterval: 10_000,
	})

	function invalidateAll() {
		void queryClient.invalidateQueries({ queryKey: ['alliance-status', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['alliances', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['alliance-invites', gameId] })
	}

	function handleError(err: unknown) {
		if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Request failed')
		else setError('Request failed')
	}

	const createMutation = useMutation({
		mutationFn: (req: CreateAllianceRequest) =>
			apiClient.post('/api/alliances', req),
		onSuccess: () => {
			setSuccess('Alliance created!')
			setShowCreate(false)
			setNewName('')
			setNewPassword('')
			invalidateAll()
		},
		onError: handleError,
	})

	const joinMutation = useMutation({
		mutationFn: ({ allianceId, password }: { allianceId: string; password: string }) => {
			const body: JoinAllianceRequest = { password }
			return apiClient.post(`/api/alliances/${allianceId}/join`, body)
		},
		onSuccess: () => {
			setSuccess('Join request sent!')
			setJoinAllianceId(null)
			setJoinPassword('')
			invalidateAll()
		},
		onError: handleError,
	})

	const leaveMutation = useMutation({
		mutationFn: () => apiClient.delete('/api/alliances/leave'),
		onSuccess: () => {
			setSuccess('You left the alliance.')
			invalidateAll()
		},
		onError: handleError,
	})

	const acceptInviteMutation = useMutation({
		mutationFn: ({ allianceId, inviteId }: { allianceId: string; inviteId: string }) =>
			apiClient.post(`/api/alliances/${allianceId}/accept-invite`, { inviteId }),
		onSuccess: () => {
			setSuccess('Invite accepted!')
			invalidateAll()
		},
		onError: handleError,
	})

	const declineInviteMutation = useMutation({
		mutationFn: ({ allianceId, inviteId }: { allianceId: string; inviteId: string }) =>
			apiClient.post(`/api/alliances/${allianceId}/decline-invite`, { inviteId }),
		onSuccess: () => {
			setSuccess('Invite declined.')
			invalidateAll()
		},
		onError: handleError,
	})

	const inAlliance = myStatus?.isMember && !myStatus.isPending

	return (
		<div className="space-y-6">
			<div className="flex items-center justify-between">
				<h1 className="text-2xl font-bold flex items-center gap-2">
					<ShieldIcon className="h-6 w-6 text-primary" />
					Alliances
				</h1>
				{!inAlliance && !showCreate && (
					<button
						onClick={() => { setShowCreate(true); setError(null); setSuccess(null) }}
						className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90"
					>
						Create Alliance
					</button>
				)}
			</div>

			{error && <div className="text-destructive text-sm">{error}</div>}
			{success && <div className="text-green-400 text-sm">{success}</div>}

			{/* Current Alliance Status */}
			{myStatus?.isMember && (
				<div className="rounded-lg border border-primary/40 bg-primary/5 p-4">
					<div className="flex items-center justify-between">
						<div>
							<div className="text-sm text-muted-foreground">Your Alliance</div>
							<Link
								to={`/alliances/${myStatus.allianceId}`}
								className="text-lg font-semibold text-primary hover:underline"
							>
								{myStatus.allianceName}
							</Link>
							{myStatus.isPending && (
								<span className="ml-2 rounded bg-yellow-700/30 px-2 py-0.5 text-xs text-yellow-300">
									Pending Approval
								</span>
							)}
							{myStatus.isLeader && (
								<span className="ml-2 rounded bg-amber-700/30 px-2 py-0.5 text-xs text-amber-300">
									Leader
								</span>
							)}
						</div>
						<button
							onClick={() => { setError(null); setSuccess(null); leaveMutation.mutate() }}
							disabled={leaveMutation.isPending}
							className="rounded border border-destructive px-3 py-1 text-xs text-destructive hover:bg-destructive/10 disabled:opacity-50"
						>
							{leaveMutation.isPending ? 'Leaving…' : 'Leave Alliance'}
						</button>
					</div>
				</div>
			)}

			{/* Pending Invites */}
			{(invites?.length ?? 0) > 0 && !inAlliance && (
				<div className="rounded-lg border border-yellow-700 bg-yellow-900/10">
					<div className="border-b border-yellow-700 bg-yellow-900/20 px-4 py-3">
						<strong className="text-sm text-yellow-200">
							Pending Invites ({invites!.length})
						</strong>
					</div>
					<div className="divide-y divide-yellow-700/30">
						{invites!.map((inv) => (
							<div key={inv.inviteId} className="flex items-center justify-between px-4 py-3">
								<div>
									<span className="font-medium">{inv.allianceName}</span>
									<span className="ml-2 text-sm text-muted-foreground">
										invited by {inv.inviterPlayerName}
									</span>
									<span className="ml-2 text-xs text-muted-foreground">
										expires {relativeTime(inv.expiresAt)}
									</span>
								</div>
								<div className="flex gap-2">
									<button
										onClick={() => {
											setError(null); setSuccess(null)
											acceptInviteMutation.mutate({ allianceId: inv.allianceId, inviteId: inv.inviteId })
										}}
										className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90"
									>
										Accept
									</button>
									<button
										onClick={() => {
											setError(null); setSuccess(null)
											declineInviteMutation.mutate({ allianceId: inv.allianceId, inviteId: inv.inviteId })
										}}
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

			{/* Create Alliance Form */}
			{showCreate && (
				<div className="rounded-lg border bg-card p-5 space-y-4">
					<strong className="text-sm">Create New Alliance</strong>
					<div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Alliance Name</label>
							<input
								value={newName}
								onChange={(e) => setNewName(e.target.value)}
								placeholder="Enter alliance name"
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
								maxLength={30}
							/>
						</div>
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Password</label>
							<input
								type="password"
								value={newPassword}
								onChange={(e) => setNewPassword(e.target.value)}
								placeholder="Alliance password"
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
							/>
						</div>
					</div>
					<div className="flex gap-2">
						<button
							onClick={() => {
								setError(null); setSuccess(null)
								createMutation.mutate({ allianceName: newName, password: newPassword })
							}}
							disabled={!newName.trim() || !newPassword.trim() || createMutation.isPending}
							className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
						>
							{createMutation.isPending ? 'Creating…' : 'Create'}
						</button>
						<button
							onClick={() => setShowCreate(false)}
							className="text-sm text-muted-foreground hover:text-foreground"
						>
							Cancel
						</button>
					</div>
				</div>
			)}

			{/* Alliance List */}
			<div className="rounded-lg border bg-card">
				<div className="border-b px-4 py-3">
					<strong className="text-sm">All Alliances</strong>
				</div>
				{!alliances || alliances.length === 0 ? (
					<div className="p-4 text-muted-foreground text-sm">
						No alliances yet. Be the first to create one!
					</div>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full text-sm">
							<thead className="border-b">
								<tr>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Name</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Members</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Status</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Founded</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
								</tr>
							</thead>
							<tbody>
								{alliances.map((a) => (
									<tr key={a.allianceId} className="border-b border-border hover:bg-secondary/30">
										<td className="py-2 px-3">
											<Link
												to={`/alliances/${a.allianceId}`}
												className="font-medium text-primary hover:underline flex items-center gap-1.5"
											>
												<ShieldIcon className="h-3.5 w-3.5" />
												{a.name}
											</Link>
										</td>
										<td className="py-2 px-3">
											<span className="flex items-center gap-1">
												<UsersIcon className="h-3.5 w-3.5 text-muted-foreground" />
												{a.memberCount}
											</span>
										</td>
										<td className="py-2 px-3">
											{a.isAtWar && (
												<span className="inline-flex items-center gap-1 rounded bg-red-700/30 px-2 py-0.5 text-xs text-red-300">
													<SwordsIcon className="h-3 w-3" />
													At War
												</span>
											)}
										</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">
											{relativeTime(a.created)}
										</td>
										<td className="py-2 px-3">
											{!inAlliance && (
												<button
													onClick={() => {
														setError(null); setSuccess(null)
														setJoinAllianceId(a.allianceId)
														setJoinPassword('')
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
			</div>

			{/* Join Alliance Modal */}
			{joinAllianceId && (
				<div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
					<div className="w-full max-w-sm rounded-lg border bg-card p-6 shadow-xl space-y-4">
						<div className="flex items-center justify-between">
							<strong className="text-sm">Join Alliance</strong>
							<button
								onClick={() => setJoinAllianceId(null)}
								className="text-muted-foreground hover:text-foreground text-lg"
							>
								×
							</button>
						</div>
						<p className="text-sm text-muted-foreground">
							Enter the alliance password to request membership.
						</p>
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Password</label>
							<input
								type="password"
								value={joinPassword}
								onChange={(e) => setJoinPassword(e.target.value)}
								placeholder="Alliance password"
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
								onKeyDown={(e) => {
									if (e.key === 'Enter' && joinPassword.trim()) {
										joinMutation.mutate({ allianceId: joinAllianceId, password: joinPassword })
									}
								}}
							/>
						</div>
						<div className="flex gap-2">
							<button
								onClick={() => joinMutation.mutate({ allianceId: joinAllianceId, password: joinPassword })}
								disabled={!joinPassword.trim() || joinMutation.isPending}
								className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
							>
								{joinMutation.isPending ? 'Joining…' : 'Join'}
							</button>
							<button
								onClick={() => setJoinAllianceId(null)}
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
