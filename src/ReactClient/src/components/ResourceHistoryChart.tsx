import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { ResourceHistoryViewModel, ResourceSnapshotViewModel } from '@/api/types'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Legend,
  CartesianGrid,
  Area,
  AreaChart,
} from 'recharts'
import type { TooltipContentProps } from 'recharts'
import type { NameType, ValueType } from 'recharts/types/component/DefaultTooltipContent'
import { resourceHex } from '@/lib/resource-colors'

interface ResourceHistoryChartProps {
  gameId: string
}

function formatCompact(value: number): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  const abs = Math.abs(value)
  if (abs >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
  if (abs >= 1_000) return `${(value / 1_000).toFixed(1)}K`
  return Math.round(value).toString()
}

function ChartTooltip({ active, payload, label }: TooltipContentProps<ValueType, NameType>) {
  if (!active || !payload || payload.length === 0) return null
  return (
    <div className="rounded-md border bg-popover/95 backdrop-blur-sm px-3 py-2 text-xs shadow-md">
      <div className="font-medium text-muted-foreground mb-1">Tick {label}</div>
      <div className="space-y-0.5">
        {payload.map((entry) => (
          <div key={entry.dataKey as string} className="flex items-center gap-2">
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ backgroundColor: entry.color }}
              aria-hidden
            />
            <span className="text-foreground">{entry.name}</span>
            <span className="ml-auto font-mono tabular-nums text-foreground">
              {formatCompact(entry.value as number)}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

function ChartShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <h3 className="font-semibold mb-3">Economy Trend</h3>
      {children}
    </div>
  )
}

export function ResourceHistoryChart({ gameId }: ResourceHistoryChartProps) {
  const { data, isLoading } = useQuery<ResourceHistoryViewModel>({
    queryKey: ['resource-history', gameId],
    queryFn: () => apiClient.get('/api/resource-history').then((r) => r.data),
    refetchInterval: 30_000,
  })

  if (isLoading) {
    return (
      <ChartShell>
        <div className="text-sm text-muted-foreground">Loading…</div>
      </ChartShell>
    )
  }

  if (!data || data.snapshots.length === 0) {
    return (
      <ChartShell>
        <div className="text-sm text-muted-foreground">
          No history yet. Data will appear after a few game ticks.
        </div>
      </ChartShell>
    )
  }

  const snapshots: ResourceSnapshotViewModel[] = data.snapshots
  const mineralsColor = resourceHex('minerals')
  const gasColor = resourceHex('gas')
  const landColor = resourceHex('land')

  const axisTick = { fontSize: 11, fill: 'currentColor' } as const
  const axisLine = { stroke: 'currentColor', opacity: 0.2 } as const
  const gridStroke = 'currentColor'

  return (
    <ChartShell>
      <div className="space-y-4 text-muted-foreground">
        <div>
          <div className="flex items-baseline justify-between mb-1">
            <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Resources
            </h4>
            <div className="flex items-center gap-3 text-xs">
              <span className="flex items-center gap-1.5">
                <span
                  className="inline-block h-2 w-2 rounded-full"
                  style={{ backgroundColor: mineralsColor }}
                />
                <span className="text-foreground">Minerals</span>
              </span>
              <span className="flex items-center gap-1.5">
                <span
                  className="inline-block h-2 w-2 rounded-full"
                  style={{ backgroundColor: gasColor }}
                />
                <span className="text-foreground">Gas</span>
              </span>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={snapshots} margin={{ top: 8, right: 8, left: 0, bottom: 4 }}>
              <defs>
                <linearGradient id="fill-minerals" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={mineralsColor} stopOpacity={0.35} />
                  <stop offset="100%" stopColor={mineralsColor} stopOpacity={0} />
                </linearGradient>
                <linearGradient id="fill-gas" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={gasColor} stopOpacity={0.35} />
                  <stop offset="100%" stopColor={gasColor} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke={gridStroke} strokeOpacity={0.1} vertical={false} />
              <XAxis
                dataKey="tick"
                tick={axisTick}
                axisLine={axisLine}
                tickLine={axisLine}
                minTickGap={24}
              />
              <YAxis
                tick={axisTick}
                axisLine={axisLine}
                tickLine={axisLine}
                width={48}
                tickFormatter={formatCompact}
              />
              <Tooltip content={ChartTooltip} cursor={{ stroke: 'currentColor', strokeOpacity: 0.2 }} />
              <Area
                type="monotone"
                dataKey="minerals"
                name="Minerals"
                stroke={mineralsColor}
                strokeWidth={2}
                fill="url(#fill-minerals)"
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
                isAnimationActive={false}
              />
              <Area
                type="monotone"
                dataKey="gas"
                name="Gas"
                stroke={gasColor}
                strokeWidth={2}
                fill="url(#fill-gas)"
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
                isAnimationActive={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        <div>
          <div className="flex items-baseline justify-between mb-1">
            <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Land
            </h4>
            <span className="flex items-center gap-1.5 text-xs">
              <span
                className="inline-block h-2 w-2 rounded-full"
                style={{ backgroundColor: landColor }}
              />
              <span className="text-foreground">Land</span>
            </span>
          </div>
          <ResponsiveContainer width="100%" height={120}>
            <LineChart data={snapshots} margin={{ top: 8, right: 8, left: 0, bottom: 4 }}>
              <CartesianGrid stroke={gridStroke} strokeOpacity={0.1} vertical={false} />
              <XAxis
                dataKey="tick"
                tick={axisTick}
                axisLine={axisLine}
                tickLine={axisLine}
                minTickGap={24}
                label={{ value: 'Tick', position: 'insideBottom', offset: -2, fontSize: 11, fill: 'currentColor' }}
                height={32}
              />
              <YAxis
                tick={axisTick}
                axisLine={axisLine}
                tickLine={axisLine}
                width={48}
                tickFormatter={formatCompact}
                allowDecimals={false}
              />
              <Tooltip content={ChartTooltip} cursor={{ stroke: 'currentColor', strokeOpacity: 0.2 }} />
              <Legend wrapperStyle={{ display: 'none' }} />
              <Line
                type="monotone"
                dataKey="land"
                name="Land"
                stroke={landColor}
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
                isAnimationActive={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </ChartShell>
  )
}
