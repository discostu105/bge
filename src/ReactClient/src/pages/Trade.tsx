import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
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
import { ApiError } from '@/components/ApiError'
import { useConfirm } from '@/contexts/ConfirmContext'
import { PageHeader } from '@/components/ui/page-header'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { DataTable, type ColumnDef } from '@/components/ui/data-table'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Field } from '@/components/ui/field'
import { Badge } from '@/components/ui/badge'
import {
	Dialog,
	DialogContent,
	DialogHeader,
	DialogTitle,
	DialogFooter,
} from '@/components/ui/dialog'
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select'

interface TradeProps {
	gameId: string
}

const proposeSchema = z.object({
	targetPlayerId: z.string().min(1, 'Select a player'),
	offeredResourceId: z.string().min(1, 'Select a resource to offer'),
	offeredAmount: z.number().int().min(1, 'Must be at least 1'),
	wantedResourceId: z.string().min(1, 'Select a resource to want'),
	wantedAmount: z.number().int().min(1, 'Must be at least 1'),
	note: z.string().max(200, 'Max 200 characters').optional(),
})
type ProposeValues = z.infer<typeof proposeSchema>

function statusBadgeVariant(status: string): 'default' | 'destructive' | 'secondary' | 'outline' {
	if (status === 'Accepted') return 'default'
	if (status === 'Declined') return 'destructive'
	if (status === 'Cancelled') return 'secondary'
	return 'outline'
}

export function Trade({ gameId }: TradeProps) {
	const queryClient = useQueryClient()
	const confirm = useConfirm()
	const [dialogOpen, setDialogOpen] = useState(false)

	// Fetch resource options from market endpoint
	const { data: marketData, error: marketError, refetch: refetchMarket } = useQuery<MarketViewModel>({
		queryKey: ['market', gameId],
		queryFn: () => apiClient.get('/api/market').then((r) => r.data),
	})

	// Fetch players for target dropdown
	const { data: playersResp } = useQuery<PaginatedResponse<PublicPlayerViewModel>>({
		queryKey: ['players-for-trade', gameId],
		queryFn: () => apiClient.get('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
	})

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
	const { data: history, isLoading: historyLoading } = useQuery<TradeHistoryItemViewModel[]>({
		queryKey: ['trade-history', gameId],
		queryFn: () => apiClient.get('/api/trade/history?skip=0&take=20').then((r) => r.data),
		refetchInterval: 30_000,
	})

	function invalidateAll() {
		void queryClient.invalidateQueries({ queryKey: ['trade-incoming', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['trade-sent', gameId] })
		void queryClient.invalidateQueries({ queryKey: ['trade-history', gameId] })
	}

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const form = useForm<ProposeValues>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		resolver: zodResolver(proposeSchema) as any,
		defaultValues: {
			targetPlayerId: '',
			offeredResourceId: '',
			offeredAmount: 100,
			wantedResourceId: '',
			wantedAmount: 100,
			note: '',
		},
	})

	// Send offer mutation
	const sendMutation = useMutation({
		mutationFn: (values: ProposeValues) => {
			const request: CreateTradeOfferRequest = {
				targetPlayerId: values.targetPlayerId,
				offeredResourceId: values.offeredResourceId,
				offeredAmount: values.offeredAmount,
				wantedResourceId: values.wantedResourceId,
				wantedAmount: values.wantedAmount,
				note: values.note?.trim() || null,
			}
			return apiClient.post('/api/trade/offer', request)
		},
		onSuccess: () => {
			toast.success('Trade offer sent')
			setDialogOpen(false)
			form.reset()
			invalidateAll()
		},
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Request failed')
				: 'Request failed'
			toast.error(msg)
		},
	})

	// Accept mutation
	const acceptMutation = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.post(`/api/trade/offers/${offerId}/accept`),
		onSuccess: () => { toast.success('Trade accepted'); invalidateAll() },
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Request failed')
				: 'Request failed'
			toast.error(msg)
		},
	})

	// Decline mutation
	const declineMutation = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.post(`/api/trade/offers/${offerId}/decline`),
		onSuccess: () => { toast.success('Trade declined'); invalidateAll() },
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Request failed')
				: 'Request failed'
			toast.error(msg)
		},
	})

	// Cancel mutation
	const cancelMutation = useMutation({
		mutationFn: (offerId: string) =>
			apiClient.delete(`/api/trade/offers/${offerId}`),
		onSuccess: () => { toast.success('Trade cancelled'); invalidateAll() },
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Request failed')
				: 'Request failed'
			toast.error(msg)
		},
	})

	if (marketError) return <ApiError message="Failed to load trade data." onRetry={() => void refetchMarket()} />

	const resourceOptions = marketData?.resourceOptions ?? []
	const currentPlayerId = marketData?.currentPlayerId ?? ''
	const players = playersResp?.items ?? []
	const otherPlayers = players.filter((p) => p.playerId !== currentPlayerId)

	function resourceName(resId: string): string {
		return resourceOptions.find((r) => r.id === resId)?.name ?? resId
	}

	const incomingColumns: ColumnDef<TradeOfferViewModel, unknown>[] = [
		{
			id: 'from',
			header: 'From',
			accessorFn: (o) => o.fromPlayerName,
		},
		{
			id: 'offering',
			header: 'Offering',
			cell: ({ row }) => `${row.original.offeredAmount} ${resourceName(row.original.offeredResourceId)}`,
		},
		{
			id: 'wants',
			header: 'Wants',
			cell: ({ row }) => `${row.original.wantedAmount} ${resourceName(row.original.wantedResourceId)}`,
		},
		{
			id: 'sent',
			header: 'Sent',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{relativeTime(row.original.sentAt)}</span>
			),
		},
		{
			id: 'note',
			header: 'Note',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{row.original.note ?? '—'}</span>
			),
		},
		{
			id: 'actions',
			header: '',
			cell: ({ row }) => {
				const offer = row.original
				return (
					<div className="flex gap-2">
						<Button
							variant="default"
							size="sm"
							disabled={acceptMutation.isPending}
							onClick={() => acceptMutation.mutate(offer.offerId)}
						>
							Accept
						</Button>
						<Button
							variant="destructive"
							size="sm"
							disabled={declineMutation.isPending}
							onClick={() => declineMutation.mutate(offer.offerId)}
						>
							Decline
						</Button>
					</div>
				)
			},
		},
	]

	const sentColumns: ColumnDef<TradeOfferViewModel, unknown>[] = [
		{
			id: 'to',
			header: 'To',
			accessorFn: (o) => o.toPlayerName,
		},
		{
			id: 'offering',
			header: 'Offering',
			cell: ({ row }) => `${row.original.offeredAmount} ${resourceName(row.original.offeredResourceId)}`,
		},
		{
			id: 'wants',
			header: 'Wants',
			cell: ({ row }) => `${row.original.wantedAmount} ${resourceName(row.original.wantedResourceId)}`,
		},
		{
			id: 'sent',
			header: 'Sent',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{relativeTime(row.original.sentAt)}</span>
			),
		},
		{
			id: 'note',
			header: 'Note',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{row.original.note ?? '—'}</span>
			),
		},
		{
			id: 'actions',
			header: '',
			cell: ({ row }) => {
				const offer = row.original
				return (
					<Button
						variant="destructive"
						size="sm"
						disabled={cancelMutation.isPending}
						onClick={async () => {
							const ok = await confirm({
								title: 'Cancel trade offer?',
								description: 'The offer will be withdrawn and the recipient can no longer accept it.',
								destructive: true,
								confirmLabel: 'Cancel offer',
								cancelLabel: 'Keep offer',
							})
							if (ok) cancelMutation.mutate(offer.offerId)
						}}
					>
						Cancel
					</Button>
				)
			},
		},
	]

	const historyColumns: ColumnDef<TradeHistoryItemViewModel, unknown>[] = [
		{
			id: 'counterparty',
			header: 'Counterparty',
			accessorFn: (o) => o.withPlayerName,
		},
		{
			id: 'offering',
			header: 'Offering',
			cell: ({ row }) => `${row.original.gaveAmount} ${resourceName(row.original.gaveResourceId)}`,
		},
		{
			id: 'wants',
			header: 'Wants',
			cell: ({ row }) => `${row.original.receivedAmount} ${resourceName(row.original.receivedResourceId)}`,
		},
		{
			id: 'status',
			header: 'Status',
			cell: ({ row }) => (
				<Badge variant={statusBadgeVariant(row.original.status)}>
					{row.original.status}
				</Badge>
			),
		},
		{
			id: 'settled',
			header: 'Settled',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{relativeTime(row.original.completedAt)}</span>
			),
		},
	]

	const onSubmit = (values: ProposeValues) => {
		sendMutation.mutate(values)
	}

	return (
		<div className="space-y-6">
			<PageHeader
				title="Trade"
				description="Propose direct trades with other players."
				actions={
					<Button onClick={() => setDialogOpen(true)}>Propose trade</Button>
				}
			/>

			<Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
				<DialogContent className="sm:max-w-lg">
					<DialogHeader>
						<DialogTitle>Propose Trade</DialogTitle>
					</DialogHeader>

					<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
						<Field
							label="Target Player"
							htmlFor="targetPlayerId"
							error={form.formState.errors.targetPlayerId?.message}
						>
							<Select
								value={form.watch('targetPlayerId')}
								onValueChange={(v) => form.setValue('targetPlayerId', v, { shouldValidate: true })}
							>
								<SelectTrigger id="targetPlayerId" className="w-full">
									<SelectValue placeholder="Select player" />
								</SelectTrigger>
								<SelectContent>
									{otherPlayers.map((p) => (
										<SelectItem key={p.playerId ?? ''} value={p.playerId ?? ''}>
											{p.playerName}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</Field>

						<div className="grid grid-cols-2 gap-4">
							<Field
								label="Offer resource"
								htmlFor="offeredResourceId"
								error={form.formState.errors.offeredResourceId?.message}
							>
								<Select
									value={form.watch('offeredResourceId')}
									onValueChange={(v) => form.setValue('offeredResourceId', v, { shouldValidate: true })}
								>
									<SelectTrigger id="offeredResourceId" className="w-full">
										<SelectValue placeholder="Select resource" />
									</SelectTrigger>
									<SelectContent>
										{resourceOptions.map((r) => (
											<SelectItem key={r.id} value={r.id}>{r.name}</SelectItem>
										))}
									</SelectContent>
								</Select>
							</Field>

							<Field
								label="Amount"
								htmlFor="offeredAmount"
								error={form.formState.errors.offeredAmount?.message}
							>
								<Input
									id="offeredAmount"
									type="number"
									min={1}
									{...form.register('offeredAmount', { valueAsNumber: true })}
								/>
							</Field>

							<Field
								label="Want resource"
								htmlFor="wantedResourceId"
								error={form.formState.errors.wantedResourceId?.message}
							>
								<Select
									value={form.watch('wantedResourceId')}
									onValueChange={(v) => form.setValue('wantedResourceId', v, { shouldValidate: true })}
								>
									<SelectTrigger id="wantedResourceId" className="w-full">
										<SelectValue placeholder="Select resource" />
									</SelectTrigger>
									<SelectContent>
										{resourceOptions.map((r) => (
											<SelectItem key={r.id} value={r.id}>{r.name}</SelectItem>
										))}
									</SelectContent>
								</Select>
							</Field>

							<Field
								label="Amount"
								htmlFor="wantedAmount"
								error={form.formState.errors.wantedAmount?.message}
							>
								<Input
									id="wantedAmount"
									type="number"
									min={1}
									{...form.register('wantedAmount', { valueAsNumber: true })}
								/>
							</Field>
						</div>

						<Field
							label="Note (optional)"
							htmlFor="note"
							error={form.formState.errors.note?.message}
						>
							<Input
								id="note"
								type="text"
								maxLength={200}
								placeholder="Add a note..."
								{...form.register('note')}
							/>
						</Field>

						<DialogFooter>
							<Button type="submit" disabled={sendMutation.isPending}>
								{sendMutation.isPending ? 'Sending…' : 'Send offer'}
							</Button>
						</DialogFooter>
					</form>
				</DialogContent>
			</Dialog>

			<Tabs defaultValue="incoming">
				<TabsList>
					<TabsTrigger value="incoming">Incoming</TabsTrigger>
					<TabsTrigger value="sent">Sent</TabsTrigger>
					<TabsTrigger value="history">History</TabsTrigger>
				</TabsList>

				<TabsContent value="incoming" className="mt-4">
					<DataTable
						columns={incomingColumns}
						rows={incomingLoading ? undefined : (incoming ?? [])}
						loading={incomingLoading}
						empty="No incoming trade offers."
						getRowId={(o) => o.offerId}
					/>
				</TabsContent>

				<TabsContent value="sent" className="mt-4">
					<DataTable
						columns={sentColumns}
						rows={sentLoading ? undefined : (sent ?? [])}
						loading={sentLoading}
						empty="No pending sent offers."
						getRowId={(o) => o.offerId}
					/>
				</TabsContent>

				<TabsContent value="history" className="mt-4">
					<DataTable
						columns={historyColumns}
						rows={historyLoading ? undefined : (history ?? [])}
						loading={historyLoading}
						empty="No trade history."
						getRowId={(o) => o.offerId}
					/>
				</TabsContent>
			</Tabs>
		</div>
	)
}
