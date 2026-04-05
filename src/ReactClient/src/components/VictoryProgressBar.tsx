import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel, LeaderboardEntryViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

const ECONOMIC_THRESHOLD = 500_000

function barColor(percent: number): string {
	if (percent >= 90) return 'bg-red-500'
	if (percent >= 75) return 'bg-orange-500'
	return 'bg-yellow-500'
}

interface VictoryProgressBarProps {
	gameId: string
}

export function VictoryProgressBar({ gameId }: VictoryProgressBarProps) {
	const { data: profile } = useQuery<PlayerProfileViewModel>({
		queryKey: ['playerprofile', gameId],
		queryFn: () => apiClient.get('/api/playerprofile').then((r) => r.data),
		refetchInterval: 30_000,
	})

	const { data: leaderboard } = useQuery<LeaderboardEntryViewModel[]>({
		queryKey: ['leaderboard', gameId],
		queryFn: () => apiClient.get(`/api/games/${gameId}/leaderboard`).then((r) => r.data),
		refetchInterval: 30_000,
	})

	const playerScore = profile?.score ?? 0
	const progressPercent = Math.min(100, Math.round((playerScore / ECONOMIC_THRESHOLD) * 100))

	// Top 3 competitors (excluding current player)
	const top3 = (leaderboard ?? [])
		.filter((e) => !e.isCurrentPlayer)
		.slice(0, 3)

	return (
		<div className="rounded-lg border bg-card p-4 space-y-3">
			<div className="flex items-baseline justify-between">
				<span className="text-sm font-semibold">Victory Progress</span>
				<span className="text-xs text-muted-foreground">
					Target: {ECONOMIC_THRESHOLD.toLocaleString()} Land
				</span>
			</div>

			{/* Player's own progress */}
			<div>
				<div className="flex items-center justify-between mb-1">
					<span className="text-xs font-medium text-primary">You</span>
					<span className="text-xs text-muted-foreground font-mono">
						{Math.floor(playerScore).toLocaleString()} ({progressPercent}%)
					</span>
				</div>
				<div className="h-3 w-full rounded-full bg-secondary overflow-hidden">
					<div
						className={cn('h-full rounded-full transition-all', barColor(progressPercent))}
						style={{ width: `${progressPercent}%` }}
						role="progressbar"
						aria-valuenow={progressPercent}
						aria-valuemin={0}
						aria-valuemax={100}
						aria-label="Your victory progress"
					/>
				</div>
			</div>

			{/* Top competitors */}
			{top3.length > 0 && (
				<div className="space-y-1.5">
					<span className="text-[10px] text-muted-foreground uppercase tracking-wide">Top Competitors</span>
					{top3.map((entry) => {
						const pct = Math.min(100, Math.round((entry.score / ECONOMIC_THRESHOLD) * 100))
						return (
							<div key={entry.playerId} className="flex items-center gap-2">
								<span className="text-xs text-muted-foreground truncate w-20">{entry.playerName}</span>
								<div className="h-1.5 flex-1 rounded-full bg-secondary overflow-hidden">
									<div
										className={cn('h-full rounded-full transition-all opacity-70', barColor(pct))}
										style={{ width: `${pct}%` }}
									/>
								</div>
								<span className="text-[10px] text-muted-foreground font-mono w-7 text-right">{pct}%</span>
							</div>
						)
					})}
				</div>
			)}

			{/* Milestone markers */}
			<div className="flex justify-between text-[10px] text-muted-foreground px-0.5">
				<span>0%</span>
				<span>50%</span>
				<span>75%</span>
				<span>90%</span>
				<span>100%</span>
			</div>
		</div>
	)
}
