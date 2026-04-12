import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { GameDetailViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

function barColor(percent: number): string {
	if (percent >= 90) return 'bg-red-500'
	if (percent >= 75) return 'bg-orange-500'
	return 'bg-yellow-500'
}

function formatDuration(ms: number): string {
	if (ms <= 0) return '0s'
	const totalSeconds = Math.floor(ms / 1000)
	const days = Math.floor(totalSeconds / 86400)
	const hours = Math.floor((totalSeconds % 86400) / 3600)
	const minutes = Math.floor((totalSeconds % 3600) / 60)
	const seconds = totalSeconds % 60
	if (days > 0) return `${days}d ${hours}h ${minutes}m`
	if (hours > 0) return `${hours}h ${minutes}m`
	if (minutes > 0) return `${minutes}m ${seconds}s`
	return `${seconds}s`
}

interface VictoryProgressBarProps {
	gameId: string
}

export function VictoryProgressBar({ gameId }: VictoryProgressBarProps) {
	const { data: game } = useQuery<GameDetailViewModel>({
		queryKey: ['game-detail', gameId],
		queryFn: () => apiClient.get(`/api/games/${gameId}`).then((r) => r.data),
		refetchInterval: 30_000,
	})

	// Tick the clock every second so the countdown looks live.
	const [now, setNow] = useState<number>(() => Date.now())
	useEffect(() => {
		const id = window.setInterval(() => setNow(Date.now()), 1000)
		return () => window.clearInterval(id)
	}, [])

	if (!game) {
		return (
			<div className="rounded-lg border bg-card p-4 text-xs text-muted-foreground">
				Loading game clock…
			</div>
		)
	}

	const startMs = new Date(game.startTime).getTime()
	const endMs = new Date(game.endTime).getTime()
	const totalMs = Math.max(1, endMs - startMs)
	const elapsedMs = Math.min(totalMs, Math.max(0, now - startMs))
	const remainingMs = Math.max(0, endMs - now)
	const percent = Math.min(100, Math.round((elapsedMs / totalMs) * 100))

	return (
		<div className="rounded-lg border bg-card p-4 space-y-3">
			<div className="flex items-baseline justify-between">
				<span className="text-sm font-semibold">Time Remaining</span>
				<span className="text-xs text-muted-foreground font-mono">
					{formatDuration(remainingMs)}
				</span>
			</div>

			<div className="h-3 w-full rounded-full bg-secondary overflow-hidden">
				<div
					className={cn('h-full rounded-full transition-all', barColor(percent))}
					style={{ width: `${percent}%` }}
					role="progressbar"
					aria-valuenow={percent}
					aria-valuemin={0}
					aria-valuemax={100}
					aria-label="Game time elapsed"
				/>
			</div>

			<div className="flex justify-between text-[10px] text-muted-foreground px-0.5">
				<span>Start</span>
				<span>{percent}% elapsed</span>
				<span>End</span>
			</div>
		</div>
	)
}
