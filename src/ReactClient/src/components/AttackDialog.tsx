import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as Dialog from '@radix-ui/react-dialog'
import { SwordsIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type { SelectEnemyViewModel, UnitViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

interface AttackDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
  unit: UnitViewModel | null
}

export function AttackDialog({ open, onOpenChange, gameId, unit }: AttackDialogProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [selectedPlayerId, setSelectedPlayerId] = useState<string>('')
  const [error, setError] = useState<string | null>(null)

  const { data, isLoading, error: queryError } = useQuery<SelectEnemyViewModel>({
    queryKey: ['attackableplayers', gameId],
    queryFn: () => apiClient.get<SelectEnemyViewModel>('/api/battle/attackableplayers').then((r) => r.data),
    enabled: open,
    staleTime: 30_000,
  })

  useEffect(() => {
    if (!open) {
      setSelectedPlayerId('')
      setError(null)
    }
  }, [open])

  useEffect(() => {
    if (open && data?.attackablePlayers?.length && !selectedPlayerId) {
      setSelectedPlayerId(data.attackablePlayers[0].playerId ?? '')
    }
  }, [open, data, selectedPlayerId])

  const sendMutation = useMutation({
    mutationFn: () => {
      if (!unit) throw new Error('No unit selected')
      return apiClient.post(
        `/api/battle/sendunits?unitId=${unit.unitId}&enemyPlayerId=${encodeURIComponent(selectedPlayerId)}`,
        '',
      )
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
      onOpenChange(false)
      void navigate(`/games/${gameId}/enemybase/${encodeURIComponent(selectedPlayerId)}`)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) {
        setError((err.response?.data as string) ?? 'Send failed')
      } else {
        setError('Send failed')
      }
    },
  })

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay
          className={cn(
            'fixed inset-0 z-[90] bg-black/60',
            'data-[state=open]:animate-in data-[state=open]:fade-in',
            'data-[state=closed]:animate-out data-[state=closed]:fade-out',
          )}
        />
        <Dialog.Content
          className={cn(
            'fixed left-1/2 top-1/2 z-[91] w-[calc(100vw-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2',
            'rounded-lg border bg-card p-5 shadow-lg',
            'data-[state=open]:animate-in data-[state=open]:fade-in data-[state=open]:zoom-in-95',
            'data-[state=closed]:animate-out data-[state=closed]:fade-out data-[state=closed]:zoom-out-95',
            'duration-150',
          )}
        >
          <Dialog.Title className="text-lg font-semibold flex items-center gap-2">
            <SwordsIcon className="h-5 w-5 text-destructive" aria-hidden="true" />
            Attack
          </Dialog.Title>
          <Dialog.Description className="mt-1 text-sm text-muted-foreground">
            {unit
              ? `Send ${unit.count.toLocaleString()} ${unit.definition?.name} against a rival.`
              : 'Select a target.'}
          </Dialog.Description>

          <div className="mt-4 space-y-3">
            {isLoading && (
              <div className="text-sm text-muted-foreground">Loading targets…</div>
            )}
            {queryError && !data && (
              <div className="text-sm text-destructive">Failed to load attackable players.</div>
            )}
            {data && data.attackablePlayers.length === 0 && (
              <div className="rounded border border-warning/50 bg-warning/10 p-3 text-sm">
                No rivals can be attacked right now.
              </div>
            )}
            {data && data.attackablePlayers.length > 0 && (
              <div>
                <label htmlFor="attack-target" className="block text-xs text-muted-foreground mb-1">
                  Target player
                </label>
                <select
                  id="attack-target"
                  value={selectedPlayerId}
                  onChange={(e) => setSelectedPlayerId(e.target.value)}
                  className="w-full rounded border bg-input px-2 py-1.5 text-sm"
                >
                  {data.attackablePlayers.map((p) => (
                    <option key={p.playerId ?? ''} value={p.playerId ?? ''}>
                      {p.playerName} ({Math.round(p.land).toLocaleString()})
                    </option>
                  ))}
                </select>
              </div>
            )}
            {error && <div className="text-sm text-destructive">{error}</div>}
          </div>

          <div className="mt-5 flex justify-end gap-2">
            <Dialog.Close asChild>
              <button
                type="button"
                className="min-h-[36px] rounded border px-3 py-1.5 text-sm hover:bg-muted"
              >
                Cancel
              </button>
            </Dialog.Close>
            <button
              type="button"
              disabled={!selectedPlayerId || sendMutation.isPending || !unit}
              onClick={() => {
                setError(null)
                sendMutation.mutate()
              }}
              className="min-h-[36px] rounded bg-destructive px-3 py-1.5 text-sm text-destructive-foreground hover:opacity-90 disabled:opacity-50"
            >
              {sendMutation.isPending ? 'Sending…' : 'Send Troops'}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
