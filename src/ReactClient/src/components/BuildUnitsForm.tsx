import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import axios from 'axios'
import apiClient from '@/api/client'
import type { UnitDefinitionViewModel, AddToQueueRequest } from '@/api/types'

interface BuildUnitsFormProps {
  availableUnits: UnitDefinitionViewModel[]
  gameId: string
  onQueueChanged?: () => void
}

export function BuildUnitsForm({ availableUnits, gameId, onQueueChanged }: BuildUnitsFormProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [unitDefId, setUnitDefId] = useState(availableUnits[0]?.id ?? '')
  const [count, setCount] = useState(1)
  const [error, setError] = useState<string | null>(null)

  const buildMutation = useMutation({
    mutationFn: () =>
      apiClient.post(`/api/units/build?unitDefId=${unitDefId}&count=${count}`, ''),
    onSuccess: () => {
      void navigate(`/games/${gameId}/units`)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        setError((err.response?.data as string) ?? 'Build failed')
      }
    },
  })

  const queueMutation = useMutation({
    mutationFn: () => {
      const request: AddToQueueRequest = { type: 'unit', defId: unitDefId, count }
      return apiClient.post('/api/buildqueue/add', request)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['buildqueue', gameId] })
      onQueueChanged?.()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        setError((err.response?.data as string) ?? 'Queue failed')
      }
    },
  })

  if (availableUnits.length === 0) return null

  return (
    <div className="mt-3 space-y-2">
      {error && <div className="text-destructive text-sm">{error}</div>}
      <div className="flex gap-2 flex-wrap items-center">
        <input
          type="number"
          min={1}
          value={count}
          onChange={(e) => setCount(Number(e.target.value))}
          className="w-20 rounded border bg-input px-2 py-1 text-sm"
        />
        <select
          value={unitDefId}
          onChange={(e) => setUnitDefId(e.target.value)}
          className="w-full sm:w-auto rounded border bg-input px-2 py-1 text-sm"
        >
          {availableUnits.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name}
            </option>
          ))}
        </select>
        <button
          onClick={() => buildMutation.mutate()}
          disabled={buildMutation.isPending}
          className="rounded bg-primary px-3 py-1 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
        >
          Build
        </button>
        <button
          onClick={() => queueMutation.mutate()}
          disabled={queueMutation.isPending}
          className="rounded bg-secondary px-3 py-1 text-sm text-secondary-foreground hover:opacity-90 disabled:opacity-50"
        >
          Queue
        </button>
      </div>
    </div>
  )
}
