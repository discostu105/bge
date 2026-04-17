import { useState, lazy, Suspense, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { AlertTriangleIcon, ZapIcon, InfoIcon } from 'lucide-react'
import { toast } from 'sonner'
import apiClient from '@/api/client'
import type {
	AssetsViewModel,
	AssetViewModel,
	PlayerResourcesViewModel,
	PlayerNotificationViewModel,
	GameDetailViewModel,
} from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { BuildUnitsForm } from '@/components/BuildUnitsForm'
import { WorkerAssignment } from '@/components/WorkerAssignment'
import { CostBadge } from '@/components/CostBadge'
import { relativeTime } from '@/lib/utils'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
const ResourceHistoryChart = lazy(() =>
	import('@/components/ResourceHistoryChart').then(m => ({ default: m.ResourceHistoryChart }))
)
import { ApiError } from '@/components/ApiError'
import { SkeletonCard, SkeletonLine } from '@/components/Skeleton'

interface BaseProps {
  gameId: string
}

function notificationIcon(kind: string): ReactNode {
	switch (kind) {
		case 'Warning':
			return <AlertTriangleIcon className="h-4 w-4 text-warning shrink-0" aria-hidden="true" />
		case 'GameEvent':
			return <ZapIcon className="h-4 w-4 text-primary shrink-0" aria-hidden="true" />
		default:
			return <InfoIcon className="h-4 w-4 text-muted-foreground shrink-0" aria-hidden="true" />
	}
}

function AssetCard({
	asset,
	gameId,
	onBuild,
	onQueue,
}: {
	asset: AssetViewModel
	gameId: string
	onBuild: (defId: string) => void
	onQueue: (defId: string) => void
}) {
	return (
		<Card
			className={asset.built ? 'border-green-700 bg-green-900/20' : undefined}
		>
			<CardContent className="p-4">
				<h3 className="font-semibold text-sm mb-2">{asset.definition.name}</h3>

				{asset.built ? (
					<>
						<Badge variant="secondary" className="mb-2 text-green-400">Built</Badge>
						<BuildUnitsForm
							availableUnits={asset.availableUnits}
							gameId={gameId}
						/>
					</>
				) : asset.alreadyQueued ? (
					<div className="text-xs text-muted-foreground">
						Queued — ready in {asset.ticksLeftForBuild} ticks
					</div>
				) : (
					<>
						<div className="text-xs mb-1">
							Cost: <CostBadge cost={asset.cost} />
						</div>
						{asset.prerequisites && (
							<div className="text-xs text-muted-foreground mb-2">
								Requires: {asset.prerequisites}
							</div>
						)}
						{asset.prerequisitesMet && (
							<div className="flex gap-2 mt-2">
								{asset.canAfford && (
									<Button
										variant="default"
										size="sm"
										onClick={() => onBuild(asset.definition.id)}
									>
										Build
									</Button>
								)}
								<Button
									variant="secondary"
									size="sm"
									onClick={() => onQueue(asset.definition.id)}
								>
									Queue
								</Button>
							</div>
						)}
					</>
				)}
			</CardContent>
		</Card>
	)
}

export function Base({ gameId }: BaseProps) {
	const queryClient = useQueryClient()
	const [lastError, setLastError] = useState<string | null>(null)
	const [colonizeAmount, setColonizeAmount] = useState(1)

	const { data: assetsData, isLoading: assetsLoading, error: assetsError, refetch: refetchAssets } = useQuery<AssetsViewModel>({
		queryKey: ['assets', gameId],
		queryFn: () => apiClient.get('/api/assets').then((r) => r.data),
		refetchInterval: 30_000,
	})

	const { data: resources } = useQuery<PlayerResourcesViewModel>({
		queryKey: ['resources', gameId],
		queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
		refetchInterval: 30_000,
	})

	const { data: gameDetail } = useQuery<GameDetailViewModel>({
		queryKey: ['gamedetail', gameId],
		queryFn: () => apiClient.get(`/api/games/${gameId}`).then((r) => r.data),
		refetchInterval: 30_000,
		enabled: !!gameId,
	})

	const { data: notifications, refetch: refetchNotifications } = useQuery<
		PlayerNotificationViewModel[]
	>({
		queryKey: ['notifications', gameId],
		queryFn: () => apiClient.get('/api/notifications/recent').then((r) => r.data),
		refetchInterval: 30_000,
	})

	const buildMutation = useMutation({
		mutationFn: (assetDefId: string) =>
			apiClient.post(`/api/assets/build?assetDefId=${assetDefId}`, ''),
		onSuccess: () => {
			setLastError(null)
			void queryClient.invalidateQueries({ queryKey: ['assets', gameId] })
		},
		onError: (err) => {
			if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Build failed')
		},
	})

	const queueAssetMutation = useMutation({
		mutationFn: (defId: string) =>
			apiClient.post('/api/buildqueue/add', { type: 'asset', defId, count: 1 }),
		onSuccess: () => {
			void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
		},
		onError: (err) => {
			if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Queue failed')
		},
	})

	const colonizeMutation = useMutation({
		mutationFn: () =>
			apiClient.post(`/api/colonize/colonize?amount=${colonizeAmount}`, ''),
		onSuccess: () => {
			void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
		},
		onError: (err) => {
			if (axios.isAxiosError(err)) {
				toast.error((err.response?.data as string) ?? 'Colonize failed')
			}
		},
	})

	const clearNotificationsMutation = useMutation({
		mutationFn: () => apiClient.delete('/api/notifications/recent'),
		onSuccess: () => void refetchNotifications(),
	})

	const isFinished = gameDetail?.status === 'Finished'

	const [searchParams, setSearchParams] = useSearchParams()
	const tab = searchParams.get('tab') ?? 'build'
	const setTab = (t: string) => {
		const next = new URLSearchParams(searchParams)
		if (t === 'build') next.delete('tab')
		else next.set('tab', t)
		setSearchParams(next, { replace: true })
	}

	return (
		<div className="space-y-5">
			<PageHeader title="Base" />

			{isFinished && (
				<Section boxed>
					<div className="flex items-center gap-3">
						<Badge variant="secondary" className="text-yellow-400 border-yellow-600 bg-yellow-900/20">
							Game Over
						</Badge>
						<span className="text-sm">
							The game has ended.{' '}
							<a href={`/games/${gameId}/results`} className="underline">
								View final results
							</a>
							.
						</span>
					</div>
				</Section>
			)}

			<Tabs value={tab} onValueChange={setTab}>
				<TabsList>
					<TabsTrigger value="build">Build</TabsTrigger>
					<TabsTrigger value="economy">Economy</TabsTrigger>
					<TabsTrigger value="activity">
						Activity
						{(notifications?.length ?? 0) > 0 && (
							<span className="ml-1.5 rounded-full bg-primary/20 px-1.5 py-0.5 text-[10px] font-mono text-primary">
								{notifications!.length}
							</span>
						)}
					</TabsTrigger>
				</TabsList>

				<TabsContent value="build" className="space-y-5 mt-4">
					{lastError && <div className="text-destructive text-sm">{lastError}</div>}
					{assetsLoading ? (
						<div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-hidden="true">
							{Array.from({ length: 6 }).map((_, i) => (
								<SkeletonCard key={i} className="h-32" />
							))}
						</div>
					) : assetsError && !assetsData ? (
						<ApiError message="Failed to load assets." onRetry={() => void refetchAssets()} />
					) : assetsData ? (
						<div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
							{assetsData.assets.map((asset) => (
								<AssetCard
									key={asset.definition.id}
									asset={asset}
									gameId={gameId}
									onBuild={(defId) => buildMutation.mutate(defId)}
									onQueue={(defId) => queueAssetMutation.mutate(defId)}
								/>
							))}
						</div>
					) : null}

					<BuildQueue gameId={gameId} />
				</TabsContent>

				<TabsContent value="economy" className="space-y-5 mt-4">
					<WorkerAssignment gameId={gameId} />

					{resources && (
						<Section title="Colonize">
							<div className="text-sm text-muted-foreground mb-2">
								Cost: {resources.colonizationCostPerLand} minerals per land
							</div>
							<div className="flex flex-wrap items-center gap-3">
								<Input
									type="number"
									min={1}
									max={24}
									value={colonizeAmount}
									onChange={(e) => setColonizeAmount(Number(e.target.value))}
									className="w-24"
								/>
								<span className="text-sm text-muted-foreground">
									= {colonizeAmount * resources.colonizationCostPerLand} minerals
								</span>
								<Button
									onClick={() => colonizeMutation.mutate()}
									disabled={colonizeMutation.isPending}
								>
									Colonize
								</Button>
							</div>
						</Section>
					)}

					<Section title="Resource history" boxed={false}>
						<Suspense fallback={<div className="rounded-lg border bg-card p-4 text-sm text-muted-foreground">Loading chart…</div>}>
							<ResourceHistoryChart gameId={gameId} />
						</Suspense>
					</Section>
				</TabsContent>

				<TabsContent value="activity" className="mt-4">
					<Section
						title="Recent Activity"
						actions={
							(notifications?.length ?? 0) > 0 ? (
								<Button
									variant="ghost"
									size="sm"
									onClick={() => clearNotificationsMutation.mutate()}
								>
									Clear
								</Button>
							) : undefined
						}
					>
						{!notifications ? (
							<div className="space-y-2" aria-hidden="true">
								<SkeletonLine className="w-3/4" />
								<SkeletonLine className="w-2/3" />
								<SkeletonLine className="w-1/2" />
							</div>
						) : notifications.length === 0 ? (
							<div className="text-muted-foreground text-sm">No recent activity.</div>
						) : (
							<ul className="divide-y">
								{notifications.slice(0, 10).map((n) => (
									<li key={n.id} className="flex items-start gap-2 py-2">
										<span>{notificationIcon(n.kind)}</span>
										<div className="flex-1 min-w-0">
											<span className="text-sm">{n.message}</span>
											<div className="text-xs text-muted-foreground">{relativeTime(n.createdAt)}</div>
										</div>
									</li>
								))}
							</ul>
						)}
					</Section>
				</TabsContent>
			</Tabs>
		</div>
	)
}
