import { lazy, Suspense } from 'react'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerStatsViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

// Lazily import recharts-based charts so the library only loads when charts render.
const PlayerStatsCharts = lazy(() =>
	import('@/pages/PlayerStatsCharts').then(m => ({ default: m.PlayerStatsCharts }))
)

function formatDurationMs(ms: number | null): string {
	if (ms === null || ms <= 0) return '—'
	const totalMinutes = Math.floor(ms / 60000)
	const days = Math.floor(totalMinutes / 1440)
	const hours = Math.floor((totalMinutes % 1440) / 60)
	if (days > 0) return `${days}d ${hours}h`
	if (hours > 0) return `${hours}h`
	return `${totalMinutes}m`
}

export function PlayerStats() {
	const { data, isLoading, error, refetch } = useQuery<PlayerStatsViewModel>({
		queryKey: ['player-stats'],
		queryFn: () => apiClient.get('/api/stats').then((r) => r.data),
	})

	if (isLoading) return <PageLoader message="Loading statistics..." />
	if (error) return <ApiError message="Failed to load statistics." onRetry={() => void refetch()} />

	if (!data || data.totalGamesPlayed === 0) {
		return (
			<div className="p-6 max-w-4xl mx-auto">
				<h1 className="text-2xl font-bold mb-6">My Statistics</h1>
				<div className="flex flex-col items-center justify-center py-20 text-center">
					<div className="text-5xl mb-4 opacity-30">🕐</div>
					<p className="text-muted-foreground">No game history yet.</p>
					<p className="text-muted-foreground text-sm mt-1">
						Complete a game round to see your statistics.
					</p>
					<Link to="/games" className="mt-4 text-sm text-primary hover:underline">
						Browse games →
					</Link>
				</div>
			</div>
		)
	}

	const winRatePct = Math.round(data.winRate * 100)

	const heroStats = [
		{ label: 'Games Played', value: data.totalGamesPlayed.toString() },
		{ label: 'Wins', value: data.totalWins.toString() },
		{
			label: 'Win Rate',
			value: `${winRatePct}%`,
			highlight: winRatePct >= 50 ? 'text-green-400' : undefined,
		},
		{ label: 'Avg Rank', value: `#${data.avgFinalRank.toFixed(1)}` },
		{ label: 'Avg Score', value: data.avgScorePerGame.toLocaleString() },
		{ label: 'Avg Duration', value: formatDurationMs(data.avgGameDurationMs) },
	]

	return (
		<div className="p-6 max-w-4xl mx-auto">
			<h1 className="text-2xl font-bold mb-6">My Statistics</h1>

			<dl className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 mb-6">
				{heroStats.map(({ label, value, highlight }) => (
					<div key={label} className="rounded-lg border border-border bg-card p-3 text-center" role="group" aria-label={label}>
						<dd className={`text-2xl font-bold ${highlight ?? ''}`}>{value}</dd>
						<dt className="text-xs text-muted-foreground mt-1">{label}</dt>
					</div>
				))}
			</dl>

			<Suspense fallback={<div className="h-64 flex items-center justify-center text-muted-foreground text-sm">Loading charts…</div>}>
				<PlayerStatsCharts games={data.games} />
			</Suspense>
		</div>
	)
}
