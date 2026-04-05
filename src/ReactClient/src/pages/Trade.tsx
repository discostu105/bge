import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowRightLeftIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
	TradeOfferViewModel,
	TradeHistoryItemViewModel,
	CreateTradeOfferRequest,
	MarketViewModel,
	PublicPlayerViewModel,
	PaginatedResponse,
} from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface TradeProps {
	gameId: string
}

export function Trade({ gameId }: TradeProps) {
	const queryClient = useQueryClient()
	const [error, setError] = useState<string | null>(null)
	const [success, setSuccess] = useState<string | null>(null)

	// Form state
	const [targetPlayerId, setTargetPlayerId] = useState('')
	const [offeredResourceId, setOfferedResourceId] = useState('')
	const [offeredAmount, setOfferedAmount] = useState(100)
	const [wantedResourceId, setWantedResourceId] = useState('')
	const [wantedAmount, setWantedAmount] = useState(100)
	const [note, setNote] = useState('')

	// Fetch resource options from market endpoint
	const { data: marketData, isLoading: marketLoading, error: marketError, refetch: refetchMarket } = useQuery<MarketViewModel>({
		queryKey: ['market', gameId],
		queryFn: () => apiClient.get('/api/market').then((r) => r.data),
	})

	// Fetch players for target dropdown
	const { data: playersResp } = useQuery<PaginatedResponse<PublicPlayerViewModel>>({
		queryKey: ['players-for-trade', gameId],
		queryFn: () => apiClient.get('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
	})
	const players = playersResp?.items

	// Incoming offers
	const { data: incoming, isLoading: incomingLoading } = useQuery<TradeOfferViewModel[]>({
		queryKey: ['trade-incoming', gameId],
		queryFn: () => apiClient.get('/api/trade/offers/incoming').then((r) => r.data),
		refetchInterval: 10_000,
	})

	// Sent offers
	const { data: sent, isLoading: sentLoading } = useQuery<TradeOfferViewModel[]>({
		queryKey: ['trade-sent', gameId],
		queryFn: () => apiClient.get('/api/trade/offers/sent').then((r) => r.data),
		refetchInterval: 10_000,
	})

	// Trade history
	const { data: history } = useQuery<TradeHistoryItemViewModel[]>({
		queryKey: ['trade-history', gameId],
		queryFn: () => apiClient.get('/api/trade/history?skip=0&take=20').then((r) => r.data),
		refetchInterval: 30_000,
	})

	function invalidateAll() {
		void queryClient.invalidateQueries({ queryKey: ['trade-incoming', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['trade-sent', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['trade-history', gameId] })
	}

	function handleError(err: unknown) {
		if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Request failed')
		else setError('Request failed')
	}

	// Create offer mutation
	const createMut = useMutation({
		mutationFn: () => {
			const request: CreateTradeOfferRequest = {
				targetPlayerId,
				offeredResourceId,
				offeredAmount,
				wantedResourceId,
				wantedAmount,
				note: note.trim() || null,
			}
			return apiClient.post('/api/trade/offer', request)
		},
		onSuccess: () => {
			setSuccess('Trade offer sent!')
			setError(null)
			setTargetPlayerId('')
			setOfferedAmount(100)
			setWantedAmount(100)
			setNote('')
			invalidateAll()
		},
		onError: handleError,
	})

	// Accept mutation
	const acceptMut = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.post(`/api/trade/offers/${offerId}/accept`),
		onSuccess: () => { setSuccess('Trade accepted!'); setError(null); invalidateAll() },
		onError: handleError,
	})

	// Decline mutation
	const declineMut = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.post(`/api/trade/offers/${offerId}/decline`),
		onSuccess: () => { setSuccess('Trade declined.'); setError(null); invalidateAll() },
		onError: handleError,
	})

	// Cancel mutation
	const cancelMut = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.delete(`/api/trade/offers/${offerId}`),
		onSuccess: () => { setSuccess('Trade cancelled.'); setError(null); invalidateAll() },
		onError: handleError,
	})

	if (marketLoading || incomingLoading || sentLoading) return <PageLoader message="Loading trade..." />
	if (marketError) return <ApiError message="Failed to load trade data." onRetry={() => void refetchMarket()} />

	const resourceOptions = marketData?.resourceOptions ?? []
	const currentPlayerId = marketData?.currentPlayerId ?? ''

	// Filter out current player from target list
	const otherPlayers = (players ?? []).filter((p) => p.playerId !== currentPlayerId)

	function resourceName(resId: string): string {
		return resourceOptions.find((r) => r.id === resId)?.name ?? resId
	}

	return (
		<div className="space-y-6">
			<h1 className="text-2xl font-bold flex items-center gap-2">
				<ArrowRightLeftIcon className="h-6 w-6 text-primary" />
				Trade
			</h1>

			{error && <div className="text-destructive text-sm">{error}</div>}
			{success && <div className="text-green-400 text-sm">{success}</div>}

			{/* Create Trade Offer */}
			<div className="rounded-lg border bg-card p-5 max-w-lg">
				<h2 className="font-semibold mb-4">Send Trade Offer</h2>
				<div className="space-y-3">
					<div>
						<label className="block text-sm text-muted-foreground mb-1">Target Player</label>
						<select
							value={targetPlayerId}
							onChange={(e) => setTargetPlayerId(e.target.value)}
							className="w-full rounded border bg-input px-2 py-1.5 text-sm"
						>
							<option value="">-- select player --</option>
							{otherPlayers.map((p) => (
								<option key={p.playerId ?? ''} value={p.playerId ?? ''}>
									{p.playerName}
								</option>
							))}
						</select>
					</div>
					<div className="grid grid-cols-2 gap-3">
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Offer resource</label>
							<select
								value={offeredResourceId}
								onChange={(e) => setOfferedResourceId(e.target.value)}
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
							>
								<option value="">-- select --</option>
								{resourceOptions.map((r) => (
									<option key={r.id} value={r.id}>{r.name}</option>
								))}
							</select>
						</div>
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Amount</label>
							<input
								type="number"
								min={1}
								value={offeredAmount}
								onChange={(e) => setOfferedAmount(Number(e.target.value))}
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
							/>
						</div>
					</div>
					<div className="grid grid-cols-2 gap-3">
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Want resource</label>
							<select
								value={wantedResourceId}
								onChange={(e) => setWantedResourceId(e.target.value)}
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
							>
								<option value="">-- select --</option>
								{resourceOptions.map((r) => (
									<option key={r.id} value={r.id}>{r.name}</option>
								))}
							</select>
						</div>
						<div>
							<label className="block text-sm text-muted-foreground mb-1">Amount</label>
							<input
								type="number"
								min={1}
								value={wantedAmount}
								onChange={(e) => setWantedAmount(Number(e.target.value))}
								className="w-full rounded border bg-input px-2 py-1.5 text-sm"
							/>
						</div>
					</div>
					<div>
						<label className="block text-sm text-muted-foreground mb-1">Note (optional)</label>
						<input
							type="text"
							value={note}
							onChange={(e) => setNote(e.target.value)}
							maxLength={200}
							placeholder="Add a note..."
							className="w-full rounded border bg-input px-2 py-1.5 text-sm"
						/>
					</div>
					<button
						onClick={() => createMut.mutate()}
						disabled={createMut.isPending || !targetPlayerId || !offeredResourceId || !wantedResourceId}
						className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
					>
						{createMut.isPending ? 'Sending...' : 'Send Offer'}
					</button>
				</div>
			</div>

			{/* Incoming Offers */}
			<div>
				<h2 className="font-semibold mb-3">Incoming Offers</h2>
				{!incoming || incoming.length === 0 ? (
					<p className="text-muted-foreground text-sm">No incoming trade offers.</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full text-sm">
							<thead className="border-b">
								<tr>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">From</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Offering</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Wants</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Note</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Sent</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
								</tr>
							</thead>
							<tbody>
								{incoming.map((offer) => (
									<tr key={offer.offerId} className="border-b border-border">
										<td className="py-2 px-3 font-medium">{offer.fromPlayerName}</td>
										<td className="py-2 px-3">{offer.offeredAmount} {resourceName(offer.offeredResourceId)}</td>
										<td className="py-2 px-3">{offer.wantedAmount} {resourceName(offer.wantedResourceId)}</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">{offer.note ?? '—'}</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">{relativeTime(offer.sentAt)}</td>
										<td className="py-2 px-3">
											<div className="flex gap-2">
												<button
													onClick={() => acceptMut.mutate(offer.offerId)}
													disabled={acceptMut.isPending}
													className="rounded bg-green-600 px-2 py-0.5 text-xs text-white hover:opacity-90 disabled:opacity-50"
												>
													Accept
												</button>
												<button
													onClick={() => declineMut.mutate(offer.offerId)}
													disabled={declineMut.isPending}
													className="rounded bg-destructive px-2 py-0.5 text-xs text-destructive-foreground hover:opacity-90 disabled:opacity-50"
												>
													Decline
												</button>
											</div>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</div>

			{/* Sent Offers */}
			<div>
				<h2 className="font-semibold mb-3">Sent Offers</h2>
				{!sent || sent.length === 0 ? (
					<p className="text-muted-foreground text-sm">No pending sent offers.</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full text-sm">
							<thead className="border-b">
								<tr>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">To</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Offering</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Wants</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Note</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Sent</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
								</tr>
							</thead>
							<tbody>
								{sent.map((offer) => (
									<tr key={offer.offerId} className="border-b border-border bg-blue-900/10">
										<td className="py-2 px-3 font-medium">{offer.toPlayerName}</td>
										<td className="py-2 px-3">{offer.offeredAmount} {resourceName(offer.offeredResourceId)}</td>
										<td className="py-2 px-3">{offer.wantedAmount} {resourceName(offer.wantedResourceId)}</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">{offer.note ?? '—'}</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">{relativeTime(offer.sentAt)}</td>
										<td className="py-2 px-3">
											<button
												onClick={() => cancelMut.mutate(offer.offerId)}
												disabled={cancelMut.isPending}
												className="rounded bg-destructive px-2 py-0.5 text-xs text-destructive-foreground hover:opacity-90 disabled:opacity-50"
											>
												Cancel
											</button>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</div>

			{/* Trade History */}
			{history && history.length > 0 && (
				<div>
					<h2 className="font-semibold mb-3">Trade History</h2>
					<div className="overflow-x-auto">
						<table className="w-full text-sm">
							<thead className="border-b">
								<tr>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">With</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Gave</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Received</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Status</th>
									<th className="py-2 px-3 text-left font-medium text-muted-foreground">Date</th>
								</tr>
							</thead>
							<tbody>
								{history.map((item) => (
									<tr key={item.offerId} className="border-b border-border">
										<td className="py-2 px-3 font-medium">{item.withPlayerName}</td>
										<td className="py-2 px-3">{item.gaveAmount} {resourceName(item.gaveResourceId)}</td>
										<td className="py-2 px-3">{item.receivedAmount} {resourceName(item.receivedResourceId)}</td>
										<td className="py-2 px-3">
											<span className={
												item.status === 'Accepted' ? 'text-green-400 text-xs' :
												item.status === 'Declined' ? 'text-red-400 text-xs' :
												'text-muted-foreground text-xs'
											}>
												{item.status}
											</span>
										</td>
										<td className="py-2 px-3 text-xs text-muted-foreground">{relativeTime(item.completedAt)}</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				</div>
			)}
		</div>
	)
}
