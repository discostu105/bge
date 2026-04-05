import { useQuery } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { SpyAttemptViewModel } from '@/api/types'

interface SpyReportsProps {
  gameId: string
}

export function SpyReports({ gameId }: SpyReportsProps) {
  const { data: attempts, isLoading } = useQuery<SpyAttemptViewModel[]>({
    queryKey: ['spyattempts', gameId],
    queryFn: () => apiClient.get('/api/spy/attempts').then((r) => r.data),
    refetchInterval: 30_000,
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Incoming Intel</h1>
      <p className="text-sm text-muted-foreground">
        Detected spy attempts against you. Requires Counter-Intelligence tech to detect spies.
      </p>

      {isLoading ? (
        <div className="text-muted-foreground">Loading...</div>
      ) : !attempts || attempts.length === 0 ? (
        <div className="rounded-lg border bg-card p-6 text-center text-muted-foreground text-sm">
          No detected spy attempts. Upgrade Counter-Intelligence in the Tech Tree to improve detection.
        </div>
      ) : (
        <div className="rounded-lg border bg-card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Attacker</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Operation Type</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Detected At</th>
                </tr>
              </thead>
              <tbody>
                {attempts.map((a) => (
                  <tr key={a.id} className="border-b border-border">
                    <td className="py-2 px-3 font-medium">{a.attackerName}</td>
                    <td className="py-2 px-3">
                      <span className="rounded bg-yellow-700/50 px-1.5 py-0.5 text-xs text-yellow-200">
                        {a.actionType}
                      </span>
                    </td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {new Date(a.timestamp ?? a.id).toLocaleString('en', {
                        month: 'short',
                        day: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
