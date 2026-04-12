import { useState, useEffect } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useNavigate, useParams, Link } from 'react-router'
import axios from 'axios'
import apiClient from '@/api/client'
import type { SelectEnemyViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface SelectEnemyProps {
  gameId: string
}

export function SelectEnemy({ gameId }: SelectEnemyProps) {
  const { unitId } = useParams<{ unitId: string }>()
  const navigate = useNavigate()
  const [selectedPlayerId, setSelectedPlayerId] = useState<string>('')
  const [error, setError] = useState<string | null>(null)

  const { data: model, isLoading: queryLoading, error: queryError, refetch } = useQuery<SelectEnemyViewModel>({
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

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Select Enemy</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}

      {queryLoading ? (
        <PageLoader message="Loading players..." />
      ) : queryError ? (
        <ApiError message="Failed to load attackable players." onRetry={() => void refetch()} />
      ) : !model ? null : (
        <>
          <div className="space-y-4 max-w-sm">
            <div>
              <label className="block text-sm text-muted-foreground mb-1">Target player</label>
              <select
                value={selectedPlayerId}
                onChange={(e) => { setSelectedPlayerId(e.target.value) }}
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
            </div>
          </div>

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
