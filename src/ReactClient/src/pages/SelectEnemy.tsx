import { useState, useEffect } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useNavigate, useParams, Link } from 'react-router'
import axios from 'axios'
import apiClient from '@/api/client'
import type { SelectEnemyViewModel, SpyReportViewModel } from '@/api/types'

interface SelectEnemyProps {
  gameId: string
}

export function SelectEnemy({ gameId }: SelectEnemyProps) {
  const { unitId } = useParams<{ unitId: string }>()
  const navigate = useNavigate()
  const [selectedPlayerId, setSelectedPlayerId] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const [spyReport, setSpyReport] = useState<SpyReportViewModel | null>(null)

  const { data: model } = useQuery<SelectEnemyViewModel>({
    queryKey: ['attackableplayers', gameId],
    queryFn: () => apiClient.get<SelectEnemyViewModel>('/api/battle/attackableplayers').then((r) => r.data),
  })

  useEffect(() => {
    if (model?.attackablePlayers && model.attackablePlayers.length > 0 && !selectedPlayerId) {
      setSelectedPlayerId(model.attackablePlayers[0].playerId ?? "")
    }
  }, [model, selectedPlayerId])

  const sendTroopsMutation = useMutation({
    mutationFn: () =>
      apiClient.post(
        `/api/battle/sendunits?unitId=${unitId}&enemyPlayerId=${encodeURIComponent(selectedPlayerId)}`,
        ''
      ),
    onSuccess: () => {
      void navigate(`/games/${gameId}/enemybase/${encodeURIComponent(selectedPlayerId)}`)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Send failed')
    },
  })

  const spyMutation = useMutation({
    mutationFn: () =>
      apiClient
        .post(`/api/spy/execute?targetPlayerId=${encodeURIComponent(selectedPlayerId)}`, null)
        .then((r) => r.data as SpyReportViewModel),
    onSuccess: (report) => {
      setError(null)
      setSpyReport(report)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Spy failed')
    },
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Select Enemy</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}

      {!model ? (
        <div className="text-muted-foreground">Loading...</div>
      ) : (
        <>
          <div className="space-y-4 max-w-sm">
            <div>
              <label className="block text-sm text-muted-foreground mb-1">Target player</label>
              <select
                value={selectedPlayerId}
                onChange={(e) => { setSelectedPlayerId(e.target.value); setSpyReport(null) }}
                className="w-full rounded border bg-input px-2 py-1.5 text-sm"
              >
                {model.attackablePlayers.map((p) => (
                  <option key={p.playerId ?? ""} value={p.playerId ?? ""}>
                    {p.playerName} ({Math.round(p.score)})
                  </option>
                ))}
              </select>
            </div>

            <div className="flex gap-3">
              <button
                onClick={() => sendTroopsMutation.mutate()}
                disabled={!selectedPlayerId || sendTroopsMutation.isPending}
                className="rounded bg-destructive px-4 py-1.5 text-sm text-destructive-foreground hover:opacity-90 disabled:opacity-50"
              >
                Send Troops
              </button>
              <button
                onClick={() => spyMutation.mutate()}
                disabled={!selectedPlayerId || spyMutation.isPending}
                className="rounded bg-secondary px-4 py-1.5 text-sm text-secondary-foreground hover:opacity-90 disabled:opacity-50"
              >
                {spyMutation.isPending ? 'Spying…' : 'Send Spy (50 minerals)'}
              </button>
            </div>
          </div>

          {/* Spy Report inline */}
          {spyReport && (
            <details open className="rounded-lg border bg-card p-4 max-w-md">
              <summary className="cursor-pointer font-semibold text-sm">
                Spy Report — {spyReport.targetPlayerName}{' '}
                <span className="text-muted-foreground font-normal">
                  ({new Date(spyReport.reportTime).toLocaleTimeString()})
                </span>
              </summary>
              <div className="mt-3 space-y-2 text-sm">
                <p>
                  <strong>Approximate Resources:</strong> ~{Math.round(spyReport.approximateMinerals)}{' '}
                  minerals, ~{Math.round(spyReport.approximateGas)} gas
                </p>
                {spyReport.unitEstimates.length > 0 ? (
                  <table className="w-full text-xs">
                    <thead className="border-b">
                      <tr>
                        <th className="py-1 text-left font-medium text-muted-foreground">Unit</th>
                        <th className="py-1 text-left font-medium text-muted-foreground">~Count</th>
                      </tr>
                    </thead>
                    <tbody>
                      {spyReport.unitEstimates
                        .slice()
                        .sort((a, b) => b.approximateCount - a.approximateCount)
                        .map((u) => (
                          <tr key={u.unitDefId} className="border-b border-border">
                            <td className="py-1">{u.unitTypeName}</td>
                            <td className="py-1">~{u.approximateCount}</td>
                          </tr>
                        ))}
                    </tbody>
                  </table>
                ) : (
                  <p className="text-muted-foreground">No units detected at base.</p>
                )}
              </div>
            </details>
          )}

          <Link
            to={`/games/${gameId}/units`}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Cancel Attack
          </Link>
        </>
      )}
    </div>
  )
}
