import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { ResourceHistoryViewModel } from '@/api/types'
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from 'recharts'
import { resourceHex } from '@/lib/resource-colors'

interface ResourceHistoryChartProps {
  gameId: string
}

export function ResourceHistoryChart({ gameId }: ResourceHistoryChartProps) {
  const { data, isLoading } = useQuery<ResourceHistoryViewModel>({
    queryKey: ['resource-history', gameId],
    queryFn: () => apiClient.get('/api/resource-history').then((r) => r.data),
    refetchInterval: 30_000,
  })

  if (isLoading) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <h3 className="font-semibold mb-3">Economy Trend</h3>
        <div className="text-sm text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (!data || data.snapshots.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <h3 className="font-semibold mb-3">Economy Trend</h3>
        <div className="text-sm text-muted-foreground">
          No history yet. Data will appear after a few game ticks.
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card p-4">
      <h3 className="font-semibold mb-3">Economy Trend</h3>
      <ResponsiveContainer width="100%" height={250}>
        <LineChart data={data.snapshots}>
          <XAxis
            dataKey="tick"
            tick={{ fontSize: 12 }}
            label={{ value: 'Tick', position: 'insideBottom', offset: -5, fontSize: 12 }}
          />
          <YAxis tick={{ fontSize: 12 }} />
          <Tooltip />
          <Legend />
          <Line type="monotone" dataKey="minerals" stroke={resourceHex('minerals')} name="Minerals" dot={false} />
          <Line type="monotone" dataKey="gas" stroke={resourceHex('gas')} name="Gas" dot={false} />
          <Line type="monotone" dataKey="land" stroke={resourceHex('land')} name="Land" dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
