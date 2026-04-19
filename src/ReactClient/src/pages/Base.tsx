import { useMemo, useState, lazy, Suspense, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import {
	AlertTriangleIcon, ZapIcon, InfoIcon, UsersIcon, MountainIcon, HammerIcon,
	ChevronDownIcon, ChevronRightIcon, CheckIcon, LockIcon, XIcon, PlusCircleIcon, SwordsIcon,
} from 'lucide-react'
import apiClient from '@/api/client'
import type {
	AssetsViewModel,
	AssetViewModel,
	BuildQueueViewModel,
	PlayerResourcesViewModel,
	PlayerNotificationViewModel,
	WorkerAssignmentViewModel,
	GameDetailViewModel,
} from '@/api/types'
import { BuildQueue } from '@/components/BuildQueue'
import { TrainUnitsDialog } from '@/components/TrainUnitsDialog'
import { WorkerAssignmentDialog } from '@/components/WorkerAssignmentDialog'
import { ColonizeDialog } from '@/components/ColonizeDialog'
import { AffordabilityChip } from '@/components/AffordabilityChip'
import { relativeTime } from '@/lib/utils'
import { formatNumber } from '@/lib/formatters'
import { PageHeader } from '@/components/ui/page-header'
import { Section } from '@/components/ui/section'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
const ResourceHistoryChart = lazy(() =>
	import('@/components/ResourceHistoryChart').then(m => ({ default: m.ResourceHistoryChart }))
)
import { ApiError } from '@/components/ApiError'
import { SkeletonCard, SkeletonLine } from '@/components/Skeleton'
import { cn } from '@/lib/utils'

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

type AssetState = 'built' | 'queued' | 'ready' | 'locked'

function assetState(asset: AssetViewModel): AssetState {
	if (asset.built) return 'built'
	if (asset.alreadyQueued) return 'queued'
	if (!asset.prerequisitesMet) return 'locked'
	return 'ready'
}

function AssetCard({
	asset,
	state,
	queueEntryId,
	onBuild,
	onQueue,
	onTrainUnits,
	onCancelQueue,
}: {
	asset: AssetViewModel
	state: AssetState
	queueEntryId?: string
	onBuild: (defId: string) => void
	onQueue: (defId: string) => void
	onTrainUnits: (asset: AssetViewModel) => void
	onCancelQueue: (entryId: string) => void
}) {
	const borderColor =
		state === 'built' ? 'border-success/60' :
		state === 'queued' ? 'border-warning/60' :
		state === 'locked' ? 'border-border/60 opacity-70' :
		'border-border'
	const bgTint =
		state === 'built' ? 'bg-success/5' :
		state === 'queued' ? 'bg-warning/5' :
		''

	const producesUnits = asset.availableUnits.length > 0

	return (
		<Card className={cn('py-3 gap-3 transition-colors', borderColor, bgTint)}>
			<CardContent className="px-4 space-y-2">
				<div className="flex items-start justify-between gap-2">
					<h3 className="font-semibold text-sm leading-tight">{asset.definition.name}</h3>
					{state === 'built' && <CheckIcon className="h-4 w-4 text-success shrink-0" aria-label="Built" />}
					{state === 'queued' && <HammerIcon className="h-4 w-4 text-warning shrink-0" aria-label="Building" />}
					{state === 'locked' && <LockIcon className="h-4 w-4 text-muted-foreground shrink-0" aria-label="Locked" />}
				</div>

				{state === 'built' && (
					producesUnits ? (
						<Button
							size="sm"
							className="w-full"
							onClick={() => onTrainUnits(asset)}
						>
							<SwordsIcon className="h-3.5 w-3.5" />
							Train units
						</Button>
					) : (
						<div className="text-xs text-muted-foreground">No units to train here.</div>
					)
				)}

				{state === 'queued' && (
					<div className="space-y-2">
						<div className="text-xs text-warning">
							Building · ready in <span className="mono">{asset.ticksLeftForBuild}t</span>
						</div>
						{queueEntryId && (
							<Button
								size="sm"
								variant="outline"
								className="w-full"
								onClick={() => onCancelQueue(queueEntryId)}
							>
								<XIcon className="h-3.5 w-3.5" />
								Cancel
							</Button>
						)}
					</div>
				)}

				{state === 'ready' && (
					<div className="space-y-2">
						<AffordabilityChip cost={asset.cost} affordable={asset.canAfford} block />
						<div className="flex gap-1.5">
							<Button
								size="sm"
								onClick={() => onBuild(asset.definition.id)}
								disabled={!asset.canAfford}
								className="flex-1"
								title={asset.canAfford ? 'Build now' : 'Not enough resources — try Queue instead'}
							>
								<HammerIcon className="h-3.5 w-3.5" />
								Build
							</Button>
							<Button
								size="sm"
								variant="outline"
								onClick={() => onQueue(asset.definition.id)}
								className="flex-1"
								title="Add to build queue; resources are reserved when the build starts"
							>
								<PlusCircleIcon className="h-3.5 w-3.5" />
								Queue
							</Button>
						</div>
					</div>
				)}

				{state === 'locked' && asset.prerequisites && (
					<div className="text-xs text-muted-foreground">
						Requires: {asset.prerequisites}
					</div>
				)}
			</CardContent>
		</Card>
	)
}

function EconomyStat({
	icon,
	label,
	value,
	detail,
	action,
}: {
	icon: ReactNode
	label: string
	value: ReactNode
	detail?: ReactNode
	action?: { label: string; onClick: () => void }
}) {
	return (
		<Card className="py-3 gap-2">
			<CardContent className="px-4 flex items-center gap-3">
				<div className="shrink-0 rounded-md bg-primary/10 p-2 text-primary">
					{icon}
				</div>
				<div className="min-w-0 flex-1">
					<div className="label">{label}</div>
					<div className="mono text-xl font-semibold leading-tight truncate">{value}</div>
					{detail && <div className="text-xs text-muted-foreground mt-0.5">{detail}</div>}
				</div>
				{action && (
					<Button size="sm" variant="outline" onClick={action.onClick} className="shrink-0">
						{action.label}
					</Button>
				)}
			</CardContent>
		</Card>
	)
}

export function Base({ gameId }: BaseProps) {
	const queryClient = useQueryClient()
	const [lastError, setLastError] = useState<string | null>(null)

	const [trainAsset, setTrainAsset] = useState<AssetViewModel | null>(null)
	const [trainOpen, setTrainOpen] = useState(false)
	const [workersOpen, setWorkersOpen] = useState(false)
	const [colonizeOpen, setColonizeOpen] = useState(false)

	const [activityOpen, setActivityOpen] = useState(false)

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

	const { data: workers } = useQuery<WorkerAssignmentViewModel>({
		queryKey: ['workers', gameId],
		queryFn: () => apiClient.get('/api/workers').then((r) => r.data),
		refetchInterval: 30_000,
	})

	const { data: queueData } = useQuery<BuildQueueViewModel>({
		queryKey: ['buildqueue', gameId],
		queryFn: () => apiClient.get('/api/buildqueue').then((r) => r.data),
		refetchInterval: 10_000,
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
			void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
			void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
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
			void queryClient.invalidateQueries({ queryKey: ['assets', gameId] })
		},
		onError: (err) => {
			if (axios.isAxiosError(err)) setLastError((err.response?.data as string) ?? 'Queue failed')
		},
	})

	const cancelQueueMutation = useMutation({
		mutationFn: (entryId: string) =>
			apiClient.delete(`/api/buildqueue/remove?entryId=${entryId}`),
		onSuccess: () => {
			void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
			void queryClient.invalidateQueries({ queryKey: ['assets', gameId] })
		},
	})

	const clearNotificationsMutation = useMutation({
		mutationFn: () => apiClient.delete('/api/notifications/recent'),
		onSuccess: () => void refetchNotifications(),
	})

	const isFinished = gameDetail?.status === 'Finished'

	const queueEntriesByDefId = useMemo(() => {
		const m = new Map<string, string>()
		for (const e of queueData?.entries ?? []) {
			if (e.type === 'asset') m.set(e.defId, e.id)
		}
		return m
	}, [queueData])

	const grouped = useMemo(() => {
		const g: Record<AssetState, AssetViewModel[]> = { built: [], queued: [], ready: [], locked: [] }
		for (const a of assetsData?.assets ?? []) {
			g[assetState(a)].push(a)
		}
		for (const k of Object.keys(g) as AssetState[]) {
			g[k].sort((a, b) => a.definition.name.localeCompare(b.definition.name))
		}
		return g
	}, [assetsData])

	const [searchParams] = useSearchParams()
	const tab = searchParams.get('tab')

	const economyRef = (el: HTMLDivElement | null) => {
		if (el && tab === 'economy') el.scrollIntoView({ behavior: 'smooth', block: 'start' })
	}
	const activityRef = (el: HTMLDivElement | null) => {
		if (el && tab === 'activity') {
			setActivityOpen(true)
			el.scrollIntoView({ behavior: 'smooth', block: 'start' })
		}
	}

	const queueCount = queueData?.entries.length ?? 0
	const idleWorkers = workers?.idleWorkers ?? 0
	const totalWorkers = workers?.totalWorkers ?? 0
	// Land can be either the primary or secondary resource depending on the game definition;
	// merge both to be robust across rulesets.
	const land =
		(resources?.primaryResource.cost.land ?? 0) +
		(resources?.secondaryResources.cost.land ?? 0)
	const buildingsBuilt = grouped.built.length
	const buildingsTotal = assetsData?.assets.length ?? 0

	const openTrain = (asset: AssetViewModel) => {
		setTrainAsset(asset)
		setTrainOpen(true)
	}

	return (
		<div className="space-y-6">
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

			{lastError && <div className="text-destructive text-sm">{lastError}</div>}

			<div ref={economyRef} className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3" data-testid="economy-strip">
				<EconomyStat
					icon={<UsersIcon className="h-5 w-5" />}
					label="Workers"
					value={formatNumber(totalWorkers)}
					detail={
						totalWorkers === 0
							? 'Train WBFs / drones / probes to gather resources.'
							: idleWorkers > 0
								? `${formatNumber(idleWorkers)} idle — click Assign`
								: 'All workers assigned'
					}
					action={{ label: 'Assign', onClick: () => setWorkersOpen(true) }}
				/>
				<EconomyStat
					icon={<MountainIcon className="h-5 w-5" />}
					label="Land"
					value={formatNumber(land)}
					detail={
						resources
							? `${formatNumber(resources.colonizationCostPerLand)} minerals per new tile`
							: undefined
					}
					action={{ label: 'Colonize', onClick: () => setColonizeOpen(true) }}
				/>
				<EconomyStat
					icon={<HammerIcon className="h-5 w-5" />}
					label="Build Queue"
					value={queueCount === 0 ? '—' : formatNumber(queueCount)}
					detail={
						queueCount === 0
							? `${buildingsBuilt} / ${buildingsTotal} buildings built`
							: `${queueCount} queued · ${buildingsBuilt} / ${buildingsTotal} built`
					}
				/>
			</div>

			<section aria-labelledby="buildings-heading">
				<div className="flex items-baseline justify-between mb-3">
					<h2 id="buildings-heading" className="text-lg font-semibold">Buildings</h2>
					{buildingsTotal > 0 && (
						<span className="label">
							{buildingsBuilt} / {buildingsTotal}
						</span>
					)}
				</div>

				{assetsLoading && (
					<div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-hidden="true">
						{Array.from({ length: 6 }).map((_, i) => (
							<SkeletonCard key={i} className="h-32" />
						))}
					</div>
				)}

				{!assetsLoading && assetsError && !assetsData && (
					<ApiError message="Failed to load buildings." onRetry={() => void refetchAssets()} />
				)}

				{assetsData && (
					<div className="space-y-5">
						<AssetGroup
							title="Built"
							assets={grouped.built}
							onBuild={buildMutation.mutate}
							onQueue={queueAssetMutation.mutate}
							onTrainUnits={openTrain}
							onCancelQueue={cancelQueueMutation.mutate}
							queueEntriesByDefId={queueEntriesByDefId}
							emptyNote="Build something from 'Ready to build' below to start."
						/>
						<AssetGroup
							title="Building"
							assets={grouped.queued}
							onBuild={buildMutation.mutate}
							onQueue={queueAssetMutation.mutate}
							onTrainUnits={openTrain}
							onCancelQueue={cancelQueueMutation.mutate}
							queueEntriesByDefId={queueEntriesByDefId}
							hideWhenEmpty
						/>
						<AssetGroup
							title="Ready to build"
							assets={grouped.ready}
							onBuild={buildMutation.mutate}
							onQueue={queueAssetMutation.mutate}
							onTrainUnits={openTrain}
							onCancelQueue={cancelQueueMutation.mutate}
							queueEntriesByDefId={queueEntriesByDefId}
							hideWhenEmpty
						/>
						<AssetGroup
							title="Locked"
							assets={grouped.locked}
							onBuild={buildMutation.mutate}
							onQueue={queueAssetMutation.mutate}
							onTrainUnits={openTrain}
							onCancelQueue={cancelQueueMutation.mutate}
							queueEntriesByDefId={queueEntriesByDefId}
							hideWhenEmpty
						/>
					</div>
				)}
			</section>

			<BuildQueue gameId={gameId} />

			<Section title="Resource history" boxed={false}>
				<Suspense fallback={<div className="rounded-lg border bg-card p-4 text-sm text-muted-foreground">Loading chart…</div>}>
					<ResourceHistoryChart gameId={gameId} />
				</Suspense>
			</Section>

			<section ref={activityRef}>
				<button
					type="button"
					onClick={() => setActivityOpen((v) => !v)}
					className="w-full flex items-center justify-between rounded-md border bg-card px-4 py-2.5 text-left hover:bg-muted/40"
					aria-expanded={activityOpen}
					aria-controls="activity-panel"
				>
					<span className="flex items-center gap-2">
						{activityOpen ? <ChevronDownIcon className="h-4 w-4" /> : <ChevronRightIcon className="h-4 w-4" />}
						<span className="font-semibold text-sm">Recent Activity</span>
						{(notifications?.length ?? 0) > 0 && (
							<Badge variant="secondary">{notifications!.length}</Badge>
						)}
					</span>
					{activityOpen && (notifications?.length ?? 0) > 0 && (
						<span
							role="button"
							tabIndex={0}
							onClick={(e) => { e.stopPropagation(); clearNotificationsMutation.mutate() }}
							onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.stopPropagation(); clearNotificationsMutation.mutate() } }}
							className="text-xs text-muted-foreground hover:text-foreground px-2 py-1 rounded hover:bg-muted"
						>
							Clear
						</span>
					)}
				</button>
				{activityOpen && (
					<div id="activity-panel" className="rounded-md border bg-card px-4 py-3 mt-2">
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
					</div>
				)}
			</section>

			<TrainUnitsDialog
				open={trainOpen}
				onOpenChange={(v) => { setTrainOpen(v); if (!v) setTrainAsset(null) }}
				gameId={gameId}
				asset={trainAsset}
			/>
			<WorkerAssignmentDialog
				open={workersOpen}
				onOpenChange={setWorkersOpen}
				gameId={gameId}
			/>
			<ColonizeDialog
				open={colonizeOpen}
				onOpenChange={setColonizeOpen}
				gameId={gameId}
			/>
		</div>
	)
}

function AssetGroup({
	title,
	assets,
	onBuild,
	onQueue,
	onTrainUnits,
	onCancelQueue,
	queueEntriesByDefId,
	emptyNote,
	hideWhenEmpty,
}: {
	title: string
	assets: AssetViewModel[]
	onBuild: (defId: string) => void
	onQueue: (defId: string) => void
	onTrainUnits: (asset: AssetViewModel) => void
	onCancelQueue: (entryId: string) => void
	queueEntriesByDefId: Map<string, string>
	emptyNote?: string
	hideWhenEmpty?: boolean
}) {
	if (assets.length === 0) {
		if (hideWhenEmpty) return null
		return (
			<div>
				<div className="label mb-2">{title} <span className="text-muted-foreground">(0)</span></div>
				{emptyNote && <p className="text-sm text-muted-foreground">{emptyNote}</p>}
			</div>
		)
	}
	return (
		<div>
			<div className="label mb-2">{title} <span className="text-muted-foreground">({assets.length})</span></div>
			<div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
				{assets.map((a) => (
					<AssetCard
						key={a.definition.id}
						asset={a}
						state={assetState(a)}
						queueEntryId={queueEntriesByDefId.get(a.definition.id)}
						onBuild={onBuild}
						onQueue={onQueue}
						onTrainUnits={onTrainUnits}
						onCancelQueue={onCancelQueue}
					/>
				))}
			</div>
		</div>
	)
}
