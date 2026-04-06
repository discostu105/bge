import {
	LineChart,
	Line,
	BarChart,
	Bar,
	XAxis,
	YAxis,
	Tooltip,
	ResponsiveContainer,
	ReferenceLine,
} from 'recharts'
import type { PlayerStatsGameEntry } from '@/api/types'

function buildScoreData(games: PlayerStatsGameEntry[]) {
	return games.map((g, i) => ({
		index: i + 1,
		gameName: g.gameName,
		finalScore: g.finalScore,
		finalRank: g.finalRank,
	}))
}

function buildWinRateTrendData(games: PlayerStatsGameEntry[]) {
	return games.map((_, i) => {
		const window = games.slice(Math.max(0, i - 4), i + 1)
		const wins = window.filter((g) => g.isWin).length
		return {
			index: i + 1,
			winRate: Math.round((wins / window.length) * 100),
		}
	})
}

function buildRankData(games: PlayerStatsGameEntry[]) {
	return games.map((g, i) => ({
		index: i + 1,
		gameName: g.gameName,
		// Invert rank so lower rank number = taller bar
		invertedRank: g.playersInGame - g.finalRank + 1,
		finalRank: g.finalRank,
		isWin: g.isWin,
	}))
}

function ScoreChart({ data }: { data: ReturnType<typeof buildScoreData> }) {
	return (
		<div className="rounded-lg border bg-card p-4">
			<h2 className="font-semibold mb-3 text-sm">Score Progression</h2>
			<ResponsiveContainer width="100%" height={220} aria-label="Score progression over games">
				<LineChart data={data}>
					<XAxis dataKey="index" tick={{ fontSize: 11 }} />
					<YAxis tick={{ fontSize: 11 }} tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}K`} />
					<Tooltip />
					<Line type="monotone" dataKey="finalScore" stroke="#3b82f6" dot={false} name="Score" />
				</LineChart>
			</ResponsiveContainer>
		</div>
	)
}

function WinRateChart({ data }: { data: ReturnType<typeof buildWinRateTrendData> }) {
	return (
		<div className="rounded-lg border bg-card p-4">
			<h2 className="font-semibold mb-3 text-sm">Win Rate Trend (rolling 5)</h2>
			<ResponsiveContainer width="100%" height={220} aria-label="Rolling 5-game win rate trend">
				<LineChart data={data}>
					<XAxis dataKey="index" tick={{ fontSize: 11 }} />
					<YAxis tick={{ fontSize: 11 }} tickFormatter={(v: number) => `${v}%`} domain={[0, 100]} />
					<Tooltip />
					<ReferenceLine y={50} stroke="currentColor" strokeDasharray="4 2" strokeOpacity={0.4} />
					<Line type="monotone" dataKey="winRate" stroke="#22c55e" dot={false} name="Win Rate %" />
				</LineChart>
			</ResponsiveContainer>
		</div>
	)
}

function RankChart({ data }: { data: ReturnType<typeof buildRankData> }) {
	return (
		<div className="rounded-lg border bg-card p-4 hidden sm:block">
			<h2 className="font-semibold mb-3 text-sm">Final Rank per Game</h2>
			<ResponsiveContainer width="100%" height={150} aria-label="Final rank per game">
				<BarChart data={data}>
					<XAxis dataKey="index" tick={{ fontSize: 11 }} />
					<YAxis hide />
					<Tooltip />
					<Bar dataKey="invertedRank" fill="#f59e0b" name="Rank" radius={[2, 2, 0, 0]} />
				</BarChart>
			</ResponsiveContainer>
		</div>
	)
}

interface PlayerStatsChartsProps {
	games: PlayerStatsGameEntry[]
}

export function PlayerStatsCharts({ games }: PlayerStatsChartsProps) {
	const scoreData = buildScoreData(games)
	const winRateTrend = buildWinRateTrendData(games)
	const rankData = buildRankData(games)

	return (
		<>
			<div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
				<ScoreChart data={scoreData} />
				<WinRateChart data={winRateTrend} />
			</div>
			{games.length > 1 && <RankChart data={rankData} />}
		</>
	)
}
