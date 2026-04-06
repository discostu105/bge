import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
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
import apiClient from '@/api/client'
import type { PlayerStatsViewModel, PlayerStatsGameEntry } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

function formatDurationMs(ms: number | null): string {
  if (ms === null || ms <= 0) return '—'
  const totalMinutes = Math.floor(ms / 60000)
  const days = Math.floor(totalMinutes / 1440)
  const hours = Math.floor((totalMinutes % 1440) / 60)
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h`
  return `${totalMinutes}m`
}

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
  const scoreData = buildScoreData(data.games)
  const winRateTrend = buildWinRateTrendData(data.games)
  const rankData = buildRankData(data.games)

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

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
        <ScoreChart data={scoreData} />
        <WinRateChart data={winRateTrend} />
      </div>

      {data.games.length > 1 && <RankChart data={rankData} />}
    </div>
  )
}
