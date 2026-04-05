import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import type {
  DiplomacyStatusViewModel,
  DiplomacyProposalViewModel,
  PublicPlayerViewModel,
  ProposeNapRequest,
  ProposeResourceAgreementRequest,
  RespondToProposalRequest,
  PaginatedResponse,
} from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface DiplomacyProps {
  gameId: string
}

function proposalTypeLabel(type: string): string {
  switch (type) {
    case 'Nap': return 'Non-Aggression Pact'
    case 'ResourceAgreement': return 'Resource Agreement'
    default: return type
  }
}

function proposalTerms(p: DiplomacyProposalViewModel): string {
  if (p.type === 'ResourceAgreement') {
    return `${p.mineralsPerTick} minerals + ${p.gasPerTick} gas/tick`
  }
  return '—'
}

export function Diplomacy({ gameId }: DiplomacyProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [proposeMode, setProposeMode] = useState<'nap' | 'resource' | null>(null)
  const [selectedTargetId, setSelectedTargetId] = useState('')
  const [durationTicks, setDurationTicks] = useState(100)
  const [mineralsPerTick, setMineralsPerTick] = useState(0)
  const [gasPerTick, setGasPerTick] = useState(0)

  const { data: status, isLoading: statusLoading, error: statusError, refetch: refetchStatus } = useQuery<DiplomacyStatusViewModel>({
    queryKey: ['diplomacy', gameId],
    queryFn: () => apiClient.get('/api/diplomacy/status').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const { data: playersResp } = useQuery<PaginatedResponse<PublicPlayerViewModel>>({
    queryKey: ['playerranking', gameId],
    queryFn: () => apiClient.get('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
    refetchInterval: 30_000,
  })
  const players = playersResp?.items

  const respondMutation = useMutation({
    mutationFn: ({ proposalId, accept }: { proposalId: string; accept: boolean }) => {
      const body: RespondToProposalRequest = { accept }
      return apiClient.post(`/api/diplomacy/proposals/${proposalId}/respond`, body)
    },
    onSuccess: (_, { accept }) => {
      setSuccess(accept ? 'Proposal accepted.' : 'Proposal declined.')
      void refetchStatus()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Request failed')
    },
  })

  const proposeMutation = useMutation({
    mutationFn: () => {
      if (proposeMode === 'nap') {
        const body: ProposeNapRequest = { targetPlayerId: selectedTargetId, durationTicks }
        return apiClient.post('/api/diplomacy/propose/nap', body)
      } else {
        const body: ProposeResourceAgreementRequest = {
          targetPlayerId: selectedTargetId,
          durationTicks,
          mineralsPerTick,
          gasPerTick,
        }
        return apiClient.post('/api/diplomacy/propose/resource-agreement', body)
      }
    },
    onSuccess: () => {
      setSuccess('Proposal sent.')
      setProposeMode(null)
      setSelectedTargetId('')
      void refetchStatus()
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Proposal failed')
    },
  })

  const invalidateAll = () => void queryClient.invalidateQueries({ queryKey: ['diplomacy', gameId] })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Diplomacy</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}
      {success && <div className="text-success-foreground text-sm">{success}</div>}

      {statusLoading && <PageLoader message="Loading diplomacy..." />}
      {statusError && !status && <ApiError message="Failed to load diplomacy status." onRetry={() => void refetchStatus()} />}

      {/* Incoming proposals */}
      {(status?.pendingIncoming?.length ?? 0) > 0 && (
        <div className="rounded-lg border border-warning/50 bg-warning/10">
          <div className="border-b border-warning/50 bg-warning/20 px-4 py-3">
            <strong className="text-sm text-warning-foreground">
              Incoming Proposals ({status!.pendingIncoming.length})
            </strong>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-warning/30">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">From</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Type</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Terms</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Duration</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground"></th>
                </tr>
              </thead>
              <tbody>
                {status!.pendingIncoming.map((p) => (
                  <tr key={p.proposalId} className="border-b border-border">
                    <td className="py-2 px-3 font-medium">{p.proposerPlayerName}</td>
                    <td className="py-2 px-3">{proposalTypeLabel(p.type)}</td>
                    <td className="py-2 px-3 text-muted-foreground">{proposalTerms(p)}</td>
                    <td className="py-2 px-3">{p.durationTicks} ticks</td>
                    <td className="py-2 px-3">
                      <div className="flex gap-2">
                        <button
                          onClick={() => {
                            setError(null); setSuccess(null)
                            respondMutation.mutate({ proposalId: p.proposalId, accept: true })
                            invalidateAll()
                          }}
                          className="rounded bg-success px-2 py-0.5 text-xs text-white hover:opacity-90"
                        >
                          Accept
                        </button>
                        <button
                          onClick={() => {
                            setError(null); setSuccess(null)
                            respondMutation.mutate({ proposalId: p.proposalId, accept: false })
                            invalidateAll()
                          }}
                          className="rounded border border-destructive px-2 py-0.5 text-xs text-destructive hover:bg-destructive/10"
                        >
                          Decline
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Active NAPs */}
      <div className="rounded-lg border bg-card">
        <div className="border-b px-4 py-3">
          <strong className="text-sm">Active Non-Aggression Pacts</strong>
        </div>
        {!status || status.activeNaps.length === 0 ? (
          <div className="p-4 text-muted-foreground text-sm">No active NAPs.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Partner</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Ticks Remaining</th>
                </tr>
              </thead>
              <tbody>
                {status.activeNaps.map((nap) => (
                  <tr key={nap.napId} className="border-b border-border">
                    <td className="py-2 px-3">{nap.partnerPlayerName}</td>
                    <td className="py-2 px-3">{nap.ticksRemaining}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Active Resource Agreements */}
      <div className="rounded-lg border bg-card">
        <div className="border-b px-4 py-3">
          <strong className="text-sm">Active Resource Agreements</strong>
        </div>
        {!status || status.activeResourceAgreements.length === 0 ? (
          <div className="p-4 text-muted-foreground text-sm">No active resource agreements.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Partner</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Minerals/tick</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Gas/tick</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Ticks Remaining</th>
                </tr>
              </thead>
              <tbody>
                {status.activeResourceAgreements.map((a) => (
                  <tr key={a.agreementId} className="border-b border-border">
                    <td className="py-2 px-3">{a.partnerPlayerName}</td>
                    <td className="py-2 px-3">{a.mineralsPerTick}</td>
                    <td className="py-2 px-3">{a.gasPerTick}</td>
                    <td className="py-2 px-3">{a.ticksRemaining}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Sent proposals */}
      {(status?.pendingSent?.length ?? 0) > 0 && (
        <div className="rounded-lg border bg-card">
          <div className="border-b px-4 py-3">
            <strong className="text-sm">Sent Proposals (awaiting response)</strong>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b">
                <tr>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">To</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Type</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Terms</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Duration</th>
                  <th scope="col" className="py-2 px-3 text-left font-medium text-muted-foreground">Proposed</th>
                </tr>
              </thead>
              <tbody>
                {status!.pendingSent.map((p) => (
                  <tr key={p.proposalId} className="border-b border-border">
                    <td className="py-2 px-3">{p.targetPlayerName}</td>
                    <td className="py-2 px-3">{proposalTypeLabel(p.type)}</td>
                    <td className="py-2 px-3 text-muted-foreground">{proposalTerms(p)}</td>
                    <td className="py-2 px-3">{p.durationTicks} ticks</td>
                    <td className="py-2 px-3 text-xs text-muted-foreground">
                      {new Date(p.proposedAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Propose Agreement */}
      <div className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <strong className="text-sm">Propose Agreement</strong>
          <div className="flex gap-2">
            <button
              onClick={() => { setProposeMode('nap'); setError(null); setSuccess(null) }}
              aria-pressed={proposeMode === 'nap'}
              className={`rounded px-3 py-1 text-xs ${proposeMode === 'nap' ? 'bg-primary text-primary-foreground' : 'border border-primary text-primary'}`}
            >
              Non-Aggression Pact
            </button>
            <button
              onClick={() => { setProposeMode('resource'); setError(null); setSuccess(null) }}
              aria-pressed={proposeMode === 'resource'}
              className={`rounded px-3 py-1 text-xs ${proposeMode === 'resource' ? 'bg-primary text-primary-foreground' : 'border border-primary text-primary'}`}
            >
              Resource Agreement
            </button>
          </div>
        </div>

        {proposeMode && (
          <div className="p-5 space-y-4">
            <div>
              <label className="block text-sm text-muted-foreground mb-1">Target Player</label>
              {players ? (
                <select
                  value={selectedTargetId}
                  onChange={(e) => setSelectedTargetId(e.target.value)}
                  className="w-full max-w-xs rounded border bg-input px-2 py-1.5 text-sm"
                >
                  <option value="">— Select a player —</option>
                  {players.map((p) => (
                    <option key={p.playerId ?? ""} value={p.playerId ?? ""}>
                      {p.playerName}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  value={selectedTargetId}
                  onChange={(e) => setSelectedTargetId(e.target.value)}
                  placeholder="Player ID"
                  className="rounded border bg-input px-2 py-1.5 text-sm"
                />
              )}
            </div>

            <div>
              <label className="block text-sm text-muted-foreground mb-1">Duration (ticks)</label>
              <select
                value={durationTicks}
                onChange={(e) => setDurationTicks(Number(e.target.value))}
                className="rounded border bg-input px-2 py-1.5 text-sm"
              >
                <option value={50}>50 ticks (short)</option>
                <option value={100}>100 ticks (medium)</option>
                <option value={200}>200 ticks (long)</option>
              </select>
            </div>

            {proposeMode === 'resource' && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm text-muted-foreground mb-1">Minerals / tick</label>
                  <input
                    type="number"
                    min={0}
                    value={mineralsPerTick}
                    onChange={(e) => setMineralsPerTick(Number(e.target.value))}
                    className="w-full rounded border bg-input px-2 py-1.5 text-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm text-muted-foreground mb-1">Gas / tick</label>
                  <input
                    type="number"
                    min={0}
                    value={gasPerTick}
                    onChange={(e) => setGasPerTick(Number(e.target.value))}
                    className="w-full rounded border bg-input px-2 py-1.5 text-sm"
                  />
                </div>
                <p className="col-span-2 text-xs text-muted-foreground">
                  Resources are transferred each tick for the agreement duration.
                </p>
              </div>
            )}

            <div className="flex items-center gap-3">
              <button
                onClick={() => proposeMutation.mutate()}
                disabled={!selectedTargetId || proposeMutation.isPending}
                className="rounded bg-primary px-4 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
              >
                {proposeMutation.isPending ? 'Sending…' : 'Send Proposal'}
              </button>
              <button
                onClick={() => setProposeMode(null)}
                className="text-sm text-muted-foreground hover:text-foreground"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
