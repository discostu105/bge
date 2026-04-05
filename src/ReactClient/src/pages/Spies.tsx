import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  SpyPlayerEntryViewModel,
  SpyMissionViewModel,
  SendSpyMissionRequest,
  SendSpyMissionResponse,
} from '@/api/types'
import { PageLoader } from '@/components/PageLoader'

interface SpiesProps {
  gameId: string
}

const MISSION_TYPES = [
  {
    id: 'intelligence',
    label: 'Gather Intelligence',
    desc: 'Reveal approximate resources and unit counts',
  },
  {
    id: 'sabotage',
    label: 'Sabotage',
    desc: 'Destroy a portion of target resources',
  },
  {
    id: 'steal_resources',
    label: 'Steal Resources',
    desc: 'Transfer a portion of target minerals/gas to you',
  },
]

function missionTypeLabel(type: string): string {
  return MISSION_TYPES.find((m) => m.id === type)?.label ?? type
}

function statusLabel(status: string): string {
  switch (status) {
    case 'in_transit': return 'In Transit'
    case 'completed': return 'Completed'
    case 'intercepted': return 'Intercepted'
    default: return status
  }
}

function statusColor(status: string): string {
  switch (status) {
    case 'in_transit': return 'bg-warning/50 text-warning-foreground'
    case 'completed': return 'bg-success/50 text-success-foreground'
    case 'intercepted': return 'bg-danger/50 text-danger-foreground'
    default: return 'bg-secondary text-secondary-foreground'
  }
}

interface SpyMissionDetailModalProps {
  mission: SpyMissionViewModel
  onClose: () => void
}

function SpyMissionDetailModal({ mission, onClose }: SpyMissionDetailModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
      <div className="rounded-lg border bg-card p-6 w-full max-w-md shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-semibold">Mission Details</h3>
          <button
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground text-xl leading-none"
          >
            ×
          </button>
        </div>
        <div className="space-y-2 text-sm">
          <div>
            <span className="text-muted-foreground">Target:</span>{' '}
            <span className="font-medium">{mission.targetPlayerName}</span>
          </div>
          <div>
            <span className="text-muted-foreground">Mission:</span>{' '}
            <span>{missionTypeLabel(mission.missionType)}</span>
          </div>
          <div>
            <span className="text-muted-foreground">Status:</span>{' '}
            <span className={`rounded px-1.5 py-0.5 text-xs ${statusColor(mission.status)}`}>
              {statusLabel(mission.status)}
            </span>
          </div>
          {mission.resolvedAt && (
            <div>
              <span className="text-muted-foreground">Resolved:</span>{' '}
              <span>{new Date(mission.resolvedAt).toLocaleString()}</span>
            </div>
          )}
          {mission.result && (
            <div className="mt-3 rounded border bg-secondary p-3">
              <div className="text-muted-foreground text-xs mb-1 font-medium uppercase tracking-wide">
                Result
              </div>
              <pre className="text-xs whitespace-pre-wrap">{mission.result}</pre>
            </div>
          )}
        </div>
        <button
          onClick={onClose}
          className="mt-4 rounded bg-secondary px-4 py-1.5 text-sm text-secondary-foreground hover:opacity-90"
        >
          Close
        </button>
      </div>
    </div>
  )
}

export function Spies({ gameId }: SpiesProps) {
  const queryClient = useQueryClient()
  const [selectedPlayerId, setSelectedPlayerId] = useState('')
  const [selectedMissionType, setSelectedMissionType] = useState('intelligence')
  const [sendError, setSendError] = useState<string | null>(null)
  const [sendSuccess, setSendSuccess] = useState<string | null>(null)
  const [detailMission, setDetailMission] = useState<SpyMissionViewModel | null>(null)
  const [pageError, setPageError] = useState<string | null>(null)

  const { data: players } = useQuery<SpyPlayerEntryViewModel[]>({
    queryKey: ['spyplayers', gameId],
    queryFn: () =>
      apiClient
        .get<SpyPlayerEntryViewModel[]>('/api/spy/players')
        .then((r) => r.data)
        .catch((err: unknown) => {
          if (axios.isAxiosError(err)) setPageError(err.message)
          return []
        }),
    refetchInterval: 30_000,
  })

  const { data: missions, refetch: refetchMissions } = useQuery<SpyMissionViewModel[]>({
    queryKey: ['spymissions', gameId],
    queryFn: () => apiClient.get('/api/spy/missions').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const sendMutation = useMutation({
    mutationFn: () => {
      const request: SendSpyMissionRequest = {
        targetPlayerId: selectedPlayerId,
        missionType: selectedMissionType,
      }
      return apiClient.post<SendSpyMissionResponse>('/api/spy/send', request).then((r) => r.data)
    },
    onSuccess: (data) => {
      setSendError(null)
      const resolveTime = data.estimatedResolveAt
        ? new Date(data.estimatedResolveAt).toLocaleTimeString()
        : ''
      setSendSuccess(`Spy dispatched! Estimated resolution: ${resolveTime}`)
      setSelectedPlayerId('')
      void refetchMissions()
      void queryClient.invalidateQueries({ queryKey: ['spyplayers', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setSendError((err.response?.data as string) ?? 'Send failed')
    },
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Intelligence Operations</h1>

      {pageError && <div className="text-destructive text-sm">{pageError}</div>}

      {detailMission && (
        <SpyMissionDetailModal
          mission={detailMission}
          onClose={() => setDetailMission(null)}
        />
      )}

      {/* Send Spy card */}
      <div className="rounded-lg border bg-card p-5">
        <h2 className="font-semibold mb-4">Send Spy</h2>
        {!players ? (
          <PageLoader message="Loading players..." />
        ) : players.length === 0 ? (
          <div className="text-muted-foreground text-sm">No other players to target.</div>
        ) : (
          <>
            <div className="mb-4">
              <label className="block text-sm text-muted-foreground mb-1">Target Player</label>
              <select
                value={selectedPlayerId}
                onChange={(e) => setSelectedPlayerId(e.target.value)}
                className="w-full max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
              >
                <option value="">— select target —</option>
                {players.map((p) => (
                  <option key={p.playerId} value={p.playerId}>
                    {p.playerName} ({Math.round(p.score)} pts)
                    {p.cooldownExpiresAt && new Date(p.cooldownExpiresAt) > new Date()
                      ? ' [cooldown]'
                      : ''}
                  </option>
                ))}
              </select>
            </div>

            <div className="mb-4 space-y-2">
              <div className="text-sm text-muted-foreground mb-1">Mission Type</div>
              {MISSION_TYPES.map((mt) => (
                <label key={mt.id} className="flex items-start gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="missionType"
                    value={mt.id}
                    checked={selectedMissionType === mt.id}
                    onChange={() => setSelectedMissionType(mt.id)}
                    className="mt-0.5"
                  />
                  <div>
                    <span className="font-medium text-sm">{mt.label}</span>
                    <span className="text-muted-foreground text-sm"> — {mt.desc}</span>
                  </div>
                </label>
              ))}
            </div>

            <p className="text-xs text-muted-foreground mb-3">
              Mission cost: 50 minerals · Risk of interception increases with target counter-intelligence
            </p>

            {sendError && <div className="text-destructive text-sm mb-2">{sendError}</div>}
            {sendSuccess && <div className="text-success-foreground text-sm mb-2">{sendSuccess}</div>}

            <button
              onClick={() => sendMutation.mutate()}
              disabled={!selectedPlayerId || sendMutation.isPending}
              className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {sendMutation.isPending ? 'Sending…' : 'Send Spy'}
            </button>
          </>
        )}
      </div>

      {/* Active Missions */}
      <div className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <strong className="text-sm">Active Missions</strong>
          <button
            onClick={() => void refetchMissions()}
            className="text-xs text-muted-foreground hover:text-foreground"
          >
            Refresh
          </button>
        </div>
        {!missions ? (
          <div className="p-4 text-muted-foreground text-sm">Loading...</div>
        ) : missions.length === 0 ? (
          <div className="p-4 text-muted-foreground text-sm">No active missions.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Target</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Mission</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Status</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Launched</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground">Resolved</th>
                  <th className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
                </tr>
              </thead>
              <tbody>
                {missions.map((m) => (
                  <tr key={m.id} className="border-b border-border">
                    <td className="py-2 px-3">{m.targetPlayerName}</td>
                    <td className="py-2 px-3">{missionTypeLabel(m.missionType)}</td>
                    <td className="py-2 px-3">
                      <span className={`rounded px-1.5 py-0.5 text-xs ${statusColor(m.status)}`}>
                        {statusLabel(m.status)}
                      </span>
                    </td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {new Date(m.createdAt).toLocaleString('en', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}
                    </td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {m.resolvedAt
                        ? new Date(m.resolvedAt).toLocaleString('en', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
                        : '—'}
                    </td>
                    <td className="py-2 px-3">
                      {m.result && (
                        <button
                          onClick={() => setDetailMission(m)}
                          className="rounded bg-secondary px-2 py-0.5 text-xs text-secondary-foreground hover:opacity-90"
                        >
                          Details
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
