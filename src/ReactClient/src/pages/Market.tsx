import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import axios from 'axios'
import apiClient from '@/api/client'
import type { MarketViewModel, MarketOrderViewModel, CreateMarketOrderRequest } from '@/api/types'
import { useConfirm } from '@/contexts/ConfirmContext'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { DataTable, type ColumnDef } from '@/components/ui/data-table'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Field } from '@/components/ui/field'
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select'
import {
	Tooltip,
	TooltipTrigger,
	TooltipContent,
	TooltipProvider,
} from '@/components/ui/tooltip'
import { ConvertResourceForm } from '@/components/ConvertResourceForm'
import { ApiError } from '@/components/ApiError'

interface MarketProps {
	gameId: string
}

function exchangeRate(order: MarketOrderViewModel): string {
	if (order.wantedAmount === 0) return '—'
	const ratio = order.offeredAmount / order.wantedAmount
	return `${ratio.toFixed(4)} ${order.offeredResourceName}/${order.wantedResourceName}`
}

const postOfferSchema = z.object({
	offeredResourceId: z.string().min(1, 'Select a resource to offer'),
	offeredAmount: z.number().int().min(1, 'Must be at least 1'),
	wantedResourceId: z.string().min(1, 'Select a resource to want'),
	wantedAmount: z.number().int().min(1, 'Must be at least 1'),
})
type PostOfferValues = z.infer<typeof postOfferSchema>

export function Market({ gameId }: MarketProps) {
	const queryClient = useQueryClient()
	const confirm = useConfirm()

	const { data, isLoading, error: queryError, refetch } = useQuery<MarketViewModel>({
		queryKey: ['market', gameId],
		queryFn: () => apiClient.get('/api/market').then((r) => r.data),
		refetchInterval: 30_000,
	})

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const form = useForm<PostOfferValues>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		resolver: zodResolver(postOfferSchema) as any,
		defaultValues: {
			offeredResourceId: '',
			offeredAmount: 100,
			wantedResourceId: '',
			wantedAmount: 100,
		},
	})

	const postOfferMutation = useMutation({
		mutationFn: (values: PostOfferValues) => {
			const request: CreateMarketOrderRequest = {
				offeredResourceId: values.offeredResourceId,
				offeredAmount: values.offeredAmount,
				wantedResourceId: values.wantedResourceId,
				wantedAmount: values.wantedAmount,
			}
			return apiClient.post('/api/market/post', request)
		},
		onSuccess: () => {
			toast.success('Offer posted')
			form.reset()
			void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
		},
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Post failed')
				: 'Post failed'
			toast.error(msg)
		},
	})

	const acceptMutation = useMutation({
		mutationFn: (orderId: string) =>
			apiClient.post(`/api/market/accept?orderId=${orderId}`, ''),
		onSuccess: () => {
			void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
		},
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Accept failed')
				: 'Accept failed'
			toast.error(msg)
		},
	})

	const cancelMutation = useMutation({
		mutationFn: (orderId: string) =>
			apiClient.delete(`/api/market/cancel?orderId=${orderId}`),
		onSuccess: () => {
			void queryClient.invalidateQueries({ queryKey: ['market', gameId] })
		},
		onError: (err) => {
			const msg = axios.isAxiosError(err)
				? ((err.response?.data as string) ?? 'Cancel failed')
				: 'Cancel failed'
			toast.error(msg)
		},
	})

	if (queryError) return <ApiError message="Failed to load market." onRetry={() => void refetch()} />

	const myOrders = data?.openOrders.filter((o) => o.sellerPlayerId === data.currentPlayerId) ?? []
	const otherOrders = data?.openOrders.filter((o) => o.sellerPlayerId !== data.currentPlayerId) ?? []
	const resourceOptions = data?.resourceOptions ?? []

	const buyColumns: ColumnDef<MarketOrderViewModel, unknown>[] = [
		{
			id: 'player',
			header: 'Player',
			accessorFn: (o) => o.sellerPlayerName,
		},
		{
			id: 'offering',
			header: 'Offering',
			cell: ({ row }) => `${row.original.offeredAmount} ${row.original.offeredResourceName}`,
		},
		{
			id: 'wants',
			header: 'Wants',
			cell: ({ row }) => `${row.original.wantedAmount} ${row.original.wantedResourceName}`,
		},
		{
			id: 'rate',
			header: 'Rate',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{exchangeRate(row.original)}</span>
			),
		},
		{
			id: 'posted',
			header: 'Posted',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">
					{new Date(row.original.createdAt).toLocaleString()}
				</span>
			),
		},
		{
			id: 'actions',
			header: '',
			cell: ({ row }) => {
				const order = row.original
				const isPending = acceptMutation.isPending
				return (
					<TooltipProvider>
						<Tooltip>
							<TooltipTrigger asChild>
								<span tabIndex={isPending ? 0 : undefined}>
									<Button
										variant="default"
										size="sm"
										disabled={isPending}
										onClick={() => acceptMutation.mutate(order.orderId)}
									>
										Accept
									</Button>
								</span>
							</TooltipTrigger>
							{isPending && (
								<TooltipContent>Accepting order…</TooltipContent>
							)}
						</Tooltip>
					</TooltipProvider>
				)
			},
		},
	]

	const myOrderColumns: ColumnDef<MarketOrderViewModel, unknown>[] = [
		{
			id: 'offering',
			header: 'Offering',
			cell: ({ row }) => `${row.original.offeredAmount} ${row.original.offeredResourceName}`,
		},
		{
			id: 'wants',
			header: 'Wants',
			cell: ({ row }) => `${row.original.wantedAmount} ${row.original.wantedResourceName}`,
		},
		{
			id: 'rate',
			header: 'Rate',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">{exchangeRate(row.original)}</span>
			),
		},
		{
			id: 'posted',
			header: 'Posted',
			cell: ({ row }) => (
				<span className="text-xs text-muted-foreground">
					{new Date(row.original.createdAt).toLocaleString()}
				</span>
			),
		},
		{
			id: 'actions',
			header: '',
			cell: ({ row }) => {
				const order = row.original
				return (
					<Button
						variant="destructive"
						size="sm"
						disabled={cancelMutation.isPending}
						onClick={async () => {
							const ok = await confirm({
								title: 'Cancel market order?',
								description: `Your ${order.offeredAmount} ${order.offeredResourceName} will be returned to your base.`,
								destructive: true,
								confirmLabel: 'Cancel order',
								cancelLabel: 'Keep order',
							})
							if (ok) cancelMutation.mutate(order.orderId)
						}}
					>
						Cancel
					</Button>
				)
			},
		},
	]

	const onSubmit = (values: PostOfferValues) => {
		postOfferMutation.mutate(values)
	}

	return (
		<div className="space-y-6">
			<PageHeader
				title="Market"
				description="Post or accept resource trades with other players."
			/>

			<Tabs defaultValue="buy">
				<TabsList>
					<TabsTrigger value="buy">Buy</TabsTrigger>
					<TabsTrigger value="sell">Sell</TabsTrigger>
					<TabsTrigger value="convert">Convert</TabsTrigger>
				</TabsList>

				<TabsContent value="buy" className="mt-4">
					<DataTable
						columns={buyColumns}
						rows={isLoading ? undefined : otherOrders}
						loading={isLoading}
						empty="No open orders from other players."
						getRowId={(o) => o.orderId}
					/>
				</TabsContent>

				<TabsContent value="sell" className="mt-4 space-y-6">
					<Section title="My offers">
						<DataTable
							columns={myOrderColumns}
							rows={isLoading ? undefined : myOrders}
							loading={isLoading}
							empty="You have no open offers."
							getRowId={(o) => o.orderId}
						/>
					</Section>

					<Section title="Post a new offer">
						<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 max-w-lg">
							<div className="grid grid-cols-2 gap-4">
								<Field
									label="Offering resource"
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
									label="Amount to offer"
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
									label="Wanting resource"
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
									label="Amount wanted"
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

							<Button type="submit" disabled={postOfferMutation.isPending}>
								{postOfferMutation.isPending ? 'Posting…' : 'Post offer'}
							</Button>
						</form>
					</Section>
				</TabsContent>

				<TabsContent value="convert" className="mt-4">
					<ConvertResourceForm gameId={gameId} />
				</TabsContent>
			</Tabs>
		</div>
	)
}
