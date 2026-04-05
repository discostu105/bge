import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel, LeaderboardEntryViewModel } from '@/api/types'

const ECONOMIC_THRESHOLD = 500_000

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
	const leaderScore = leaderboard?.[0]?.score ?? 0
	const progressPercent = Math.min(100, Math.round((playerScore / ECONOMIC_THRESHOLD) * 100))

	return (
		<div className="rounded-lg border bg-card p-4">
			<div className="flex items-baseline justify-between mb-1">
				<span className="text-sm font-semibold">Economic Victory Progress</span>
				<span className="text-xs text-muted-foreground">
					Target: {ECONOMIC_THRESHOLD.toLocaleString()}
				</span>
			</div>
			<div className="h-3 w-full rounded-full bg-secondary overflow-hidden">
				<div
					className="h-full rounded-full bg-yellow-500 transition-all"
					style={{ width: `${progressPercent}%` }}
					role="progressbar"
					aria-valuenow={progressPercent}
					aria-valuemin={0}
					aria-valuemax={100}
				/>
			</div>
			<div className="flex justify-between mt-1">
				<span className="text-xs text-muted-foreground">
					You: {Math.floor(playerScore).toLocaleString()}
				</span>
				{leaderScore > playerScore && (
					<span className="text-xs text-muted-foreground">
						Leader: {Math.floor(leaderScore).toLocaleString()}
					</span>
				)}
			</div>
		</div>
	)
}
