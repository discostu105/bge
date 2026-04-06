import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { TrophyIcon, SwordsIcon, CoinsIcon, HandshakeIcon, CompassIcon, LockIcon, ZapIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type {
	PlayerAchievementsViewModel,
	MilestoneAchievementsViewModel,
	MilestoneAchievementViewModel,
	MilestoneCategory,
	MilestoneTier,
	ProfileViewModel,
} from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { cn } from '@/lib/utils'
import { formatDate, rankBadgeClass } from '@/lib/formatters'

// ── Config ───────────────────────────────────────────────────────────────────

const CATEGORIES: { key: MilestoneCategory; label: string; icon: React.ComponentType<{ className?: string }> }[] = [
	{ key: 'combat', label: 'Combat', icon: SwordsIcon },
	{ key: 'economy', label: 'Economy', icon: CoinsIcon },
	{ key: 'diplomacy', label: 'Diplomacy', icon: HandshakeIcon },
	{ key: 'exploration', label: 'Exploration', icon: CompassIcon },
	{ key: 'progression', label: 'Progression', icon: ZapIcon },
]

const TIER_STYLES: Record<MilestoneTier, { ring: string; bg: string; label: string; glow: string }> = {
	bronze: { ring: 'ring-amber-700/60', bg: 'bg-amber-900/20', label: 'text-amber-600', glow: '' },
	silver: { ring: 'ring-gray-400/60', bg: 'bg-gray-500/10', label: 'text-gray-400', glow: '' },
	gold: { ring: 'ring-yellow-500/70', bg: 'bg-yellow-500/10', label: 'text-yellow-500', glow: 'shadow-[0_0_12px_rgba(234,179,8,0.15)]' },
	legendary: { ring: 'ring-purple-500/70', bg: 'bg-purple-500/10', label: 'text-purple-400', glow: 'shadow-[0_0_16px_rgba(168,85,247,0.2)]' },
}

const TIER_ORDER: MilestoneTier[] = ['bronze', 'silver', 'gold', 'legendary']

// ── Helpers ──────────────────────────────────────────────────────────────────

function progressPercent(current: number, target: number) {
	if (target === 0) return 100
	return Math.min(100, Math.round((current / target) * 100))
}

function formatProgress(current: number, target: number) {
	const fmt = (n: number) => n >= 10000 ? `${(n / 1000).toFixed(n >= 100000 ? 0 : 1)}k` : n.toLocaleString()
	return `${fmt(current)} / ${fmt(target)}`
}

// ── Sub-components ───────────────────────────────────────────────────────────

function MilestoneCard({ m }: { m: MilestoneAchievementViewModel }) {
	const tier = TIER_STYLES[m.tier]
	const pct = progressPercent(m.currentProgress, m.targetProgress)

	return (
		<div
			className={cn(
				'relative rounded-lg border p-4 flex flex-col gap-2.5 transition-all duration-200',
				m.isUnlocked
					? `ring-1 ${tier.ring} ${tier.bg} ${tier.glow} border-transparent`
					: 'border-border/50 bg-card/50 opacity-65 hover:opacity-80'
			)}
		>
			{/* Icon + tier badge */}
			<div className="flex items-start justify-between">
				<div className={cn('text-3xl leading-none', !m.isUnlocked && 'grayscale')}>
					{m.isUnlocked ? m.icon : <LockIcon className="h-7 w-7 text-muted-foreground/50" />}
				</div>
				<span className={cn('text-[10px] font-bold uppercase tracking-wider', tier.label)}>
					{m.tier}
				</span>
			</div>

			{/* Name + description */}
			<div>
				<div className={cn('font-semibold text-sm', m.isUnlocked ? 'text-foreground' : 'text-muted-foreground')}>
					{m.name}
				</div>
				<div className="text-xs text-muted-foreground mt-0.5">{m.description}</div>
			</div>

			{/* Progress bar */}
			<div className="mt-auto">
				<div className="flex justify-between items-center text-[11px] text-muted-foreground mb-1">
					<span>{formatProgress(m.currentProgress, m.targetProgress)}</span>
					<span>{pct}%</span>
				</div>
				<div className="h-1.5 rounded-full bg-secondary overflow-hidden">
					<div
						className={cn(
							'h-full rounded-full transition-all duration-500',
							m.isUnlocked
								? m.tier === 'legendary' ? 'bg-purple-500' : m.tier === 'gold' ? 'bg-yellow-500' : 'bg-primary'
								: 'bg-muted-foreground/40'
						)}
						style={{ width: `${pct}%` }}
					/>
				</div>
			</div>

			{/* Unlocked date */}
			{m.isUnlocked && m.unlockedAt && (
				<div className="text-[10px] text-muted-foreground">
					Unlocked {formatDate(m.unlockedAt)}
				</div>
			)}
		</div>
	)
}

function TrophyCard({ a }: { a: { achievementIcon: string; achievementLabel: string; gameName: string; finalRank: number; score: number; earnedAt: string } }) {
	return (
		<div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-2 hover:border-border/80 transition-colors">
			<div className="text-3xl leading-none">{a.achievementIcon}</div>
			<div className="font-semibold text-sm">{a.achievementLabel}</div>
			<div className="text-sm text-muted-foreground">{a.gameName}</div>
			<div className="flex justify-between items-center mt-auto text-xs text-muted-foreground">
				<span className={`px-2 py-0.5 rounded font-mono font-bold ${rankBadgeClass(a.finalRank)}`}>
					#{a.finalRank}
				</span>
				<span>{Number(a.score).toLocaleString()} pts</span>
				<span>{formatDate(a.earnedAt)}</span>
			</div>
		</div>
	)
}

function SummaryStats({ milestones, trophyCount }: { milestones: MilestoneAchievementViewModel[]; trophyCount: number }) {
	const unlocked = milestones.filter(m => m.isUnlocked).length
	const total = milestones.length
	const overallPct = total > 0 ? Math.round((unlocked / total) * 100) : 0

	return (
		<div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
			<div className="rounded-lg border bg-secondary/20 p-3 text-center">
				<div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Trophies</div>
				<div className="text-2xl font-bold text-yellow-500">{trophyCount}</div>
			</div>
			<div className="rounded-lg border bg-secondary/20 p-3 text-center">
				<div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Milestones</div>
				<div className="text-2xl font-bold">{unlocked}<span className="text-sm font-normal text-muted-foreground"> / {total}</span></div>
			</div>
			<div className="rounded-lg border bg-secondary/20 p-3 text-center">
				<div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Completion</div>
				<div className="text-2xl font-bold">{overallPct}%</div>
			</div>
			<div className="rounded-lg border bg-secondary/20 p-3 text-center">
				<div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">Best Tier</div>
				<div className="text-2xl font-bold">
					{unlocked > 0
						? (() => {
								const unlockTiers = TIER_ORDER.filter((t: MilestoneTier) => milestones.some(m => m.isUnlocked && m.tier === t))
								const best = unlockTiers.length > 0 ? unlockTiers[unlockTiers.length - 1] : undefined
								return best ? <span className={TIER_STYLES[best].label}>{best.charAt(0).toUpperCase() + best.slice(1)}</span> : '—'
							})()
						: '—'}
				</div>
			</div>
		</div>
	)
}

// ── Main component ───────────────────────────────────────────────────────────

type Tab = 'milestones' | 'trophies'

export function Achievements() {
	const [tab, setTab] = useState<Tab>('milestones')
	const [selectedCategory, setSelectedCategory] = useState<MilestoneCategory | 'all'>('all')

	const { data, isLoading, error, refetch } = useQuery<PlayerAchievementsViewModel>({
		queryKey: ['achievements'],
		queryFn: () => apiClient.get('/api/player-management/me/achievements').then(r => r.data),
	})

	const { data: milestoneData } = useQuery<MilestoneAchievementsViewModel>({
		queryKey: ['milestone-achievements'],
		queryFn: () => apiClient.get('/api/player-management/me/milestone-achievements').then(r => r.data),
	})
	const milestones = milestoneData?.achievements ?? []

	const { data: profileData } = useQuery<ProfileViewModel>({
		queryKey: ['profile'],
		queryFn: () => apiClient.get('/api/profile').then(r => r.data),
	})

	if (isLoading) return <PageLoader message="Loading achievements..." />
	if (error) return <ApiError message="Failed to load achievements." onRetry={() => void refetch()} />

	const trophies = data?.achievements ?? []

	const filteredMilestones = selectedCategory === 'all'
		? milestones
		: milestones.filter(m => m.category === selectedCategory)

	// Sort: unlocked first (by tier desc), then locked (by progress desc)
	const sortedMilestones = [...filteredMilestones].sort((a, b) => {
		if (a.isUnlocked !== b.isUnlocked) return a.isUnlocked ? -1 : 1
		if (a.isUnlocked && b.isUnlocked) {
			return TIER_ORDER.indexOf(b.tier) - TIER_ORDER.indexOf(a.tier)
		}
		return progressPercent(b.currentProgress, b.targetProgress) - progressPercent(a.currentProgress, a.targetProgress)
	})

	return (
		<div className="p-4 sm:p-6 max-w-5xl mx-auto">
			{/* Header */}
			<div className="flex items-center gap-3 mb-1">
				<TrophyIcon className="h-6 w-6 text-yellow-500" />
				<h1 className="text-2xl font-bold">Achievements</h1>
			</div>
			<p className="text-muted-foreground mb-5">Track your progress and earn rewards across all games.</p>

			{/* Summary stats */}
			<SummaryStats milestones={milestones} trophyCount={trophies.length} />

			{/* XP / Level progress */}
			{profileData && (
				<div className="rounded-lg border bg-card p-4 mb-5">
					<div className="flex items-center gap-2 mb-3">
						<ZapIcon className="h-4 w-4 text-primary" />
						<span className="font-semibold text-sm">XP &amp; Level</span>
						<span className="ml-auto inline-flex items-center rounded-full bg-primary/20 px-3 py-0.5 text-sm font-bold text-primary border border-primary/30">
							Lv {profileData.level}
						</span>
					</div>
					<div className="space-y-1.5">
						<div className="flex justify-between text-xs text-muted-foreground">
							<span>Level {profileData.level}{profileData.level < 50 ? ` → ${profileData.level + 1}` : ' (Max)'}</span>
							<span>{profileData.totalXp.toLocaleString()} XP total
								{profileData.level < 50 && <> · {profileData.xpToNextLevel.toLocaleString()} to next</>}
							</span>
						</div>
						<div className="h-3 rounded-full bg-secondary overflow-hidden" role="progressbar" aria-valuenow={profileData.levelProgress} aria-valuemin={0} aria-valuemax={100}>
							<div
								className="h-full rounded-full bg-primary transition-all"
								style={{ width: `${profileData.level >= 50 ? 100 : profileData.levelProgress}%` }}
							/>
						</div>
					</div>
				</div>
			)}

			{/* Tab bar */}
			<div className="flex gap-1 mb-5 border-b border-border">
				<button
					onClick={() => setTab('milestones')}
					className={cn(
						'px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px',
						tab === 'milestones'
							? 'border-primary text-primary'
							: 'border-transparent text-muted-foreground hover:text-foreground'
					)}
				>
					Milestones
				</button>
				<button
					onClick={() => setTab('trophies')}
					className={cn(
						'px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px',
						tab === 'trophies'
							? 'border-primary text-primary'
							: 'border-transparent text-muted-foreground hover:text-foreground'
					)}
				>
					Game Trophies
					{trophies.length > 0 && (
						<span className="ml-1.5 px-1.5 py-0.5 rounded-full bg-secondary text-xs">{trophies.length}</span>
					)}
				</button>
			</div>

			{/* ── Milestones tab ── */}
			{tab === 'milestones' && (
				<>
					{/* Category filter pills */}
					<div className="flex flex-wrap gap-2 mb-5">
						<button
							onClick={() => setSelectedCategory('all')}
							className={cn(
								'px-3 py-1.5 rounded-full text-xs font-medium transition-colors',
								selectedCategory === 'all'
									? 'bg-primary text-primary-foreground'
									: 'bg-secondary text-muted-foreground hover:text-foreground'
							)}
						>
							All
						</button>
						{CATEGORIES.map(cat => {
							const Icon = cat.icon
							const count = milestones.filter(m => m.category === cat.key && m.isUnlocked).length
							const total = milestones.filter(m => m.category === cat.key).length
							return (
								<button
									key={cat.key}
									onClick={() => setSelectedCategory(cat.key)}
									className={cn(
										'flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium transition-colors',
										selectedCategory === cat.key
											? 'bg-primary text-primary-foreground'
											: 'bg-secondary text-muted-foreground hover:text-foreground'
									)}
								>
									<Icon className="h-3 w-3" />
									{cat.label}
									<span className="opacity-70">{count}/{total}</span>
								</button>
							)
						})}
					</div>

					{/* Milestone grid */}
					<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
						{sortedMilestones.map(m => (
							<MilestoneCard key={m.id} m={m} />
						))}
					</div>
				</>
			)}

			{/* ── Trophies tab ── */}
			{tab === 'trophies' && (
				<>
					{trophies.length === 0 ? (
						<div className="flex flex-col items-center justify-center py-20 text-center">
							<div className="text-6xl mb-4 opacity-30">🎯</div>
							<h3 className="text-lg font-semibold mb-1">No trophies yet</h3>
							<p className="text-muted-foreground mb-4">Complete your first game to earn your first trophy!</p>
							<Link
								to="/games"
								className="px-4 py-2 rounded bg-primary text-primary-foreground hover:bg-primary/90 text-sm"
							>
								Browse Games
							</Link>
						</div>
					) : (
						<>
							<p className="mb-4 text-sm text-muted-foreground">
								<strong className="text-foreground">{trophies.length}</strong>{' '}
								troph{trophies.length !== 1 ? 'ies' : 'y'} earned
							</p>
							<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
								{trophies.map((a, idx) => (
									<TrophyCard key={idx} a={a} />
								))}
							</div>
						</>
					)}
				</>
			)}
		</div>
	)
}
