import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { AdminNav } from './AdminNav'

interface TickStatus {
  currentTick: number
  isPaused: boolean
  effectiveTickDurationSeconds: number
  defaultTickDurationSeconds: number
}

export function AdminTickControl() {
  const queryClient = useQueryClient()
  const [tickCount, setTickCount] = useState(1)
  const [tickDuration, setTickDuration] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  const { data: status, isLoading, error, refetch } = useQuery<TickStatus>({
    queryKey: ['admin-tick-status'],
    queryFn: () => apiClient.get('/api/admin/tick-status').then((r) => r.data),
    refetchInterval: 5000,
  })

  const pauseMutation = useMutation({
    mutationFn: () => apiClient.post('/api/admin/pause-ticks'),
    onSuccess: () => {
      setActionError(null)
      setActionSuccess('Ticks paused.')
      queryClient.invalidateQueries({ queryKey: ['admin-tick-status'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Pause failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  const resumeMutation = useMutation({
    mutationFn: () => apiClient.post('/api/admin/resume-ticks'),
    onSuccess: () => {
      setActionError(null)
      setActionSuccess('Ticks resumed.')
      queryClient.invalidateQueries({ queryKey: ['admin-tick-status'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Resume failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  const forceTickMutation = useMutation({
    mutationFn: (count: number) => apiClient.post(`/api/admin/force-tick?count=${count}`),
    onSuccess: (_, count) => {
      setActionError(null)
      setActionSuccess(`Forced ${count} tick(s).`)
      queryClient.invalidateQueries({ queryKey: ['admin-tick-status'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Force tick failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  const setDurationMutation = useMutation({
    mutationFn: (seconds: number) => apiClient.post(`/api/admin/set-tick-duration?seconds=${seconds}`),
    onSuccess: (_, seconds) => {
      setActionError(null)
      setActionSuccess(`Tick duration set to ${seconds}s.`)
      setTickDuration('')
      queryClient.invalidateQueries({ queryKey: ['admin-tick-status'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Set duration failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <h1 className="text-2xl font-bold mb-4">Game Tick Control</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold mb-4">Game Tick Control</h1>
        <ApiError message="Failed to load tick status." onRetry={() => void refetch()} />
      </div>
    )
  }

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <AdminNav />
      <h1 className="text-2xl font-bold mb-6">Game Tick Control</h1>

      {actionError && (
        <div className="rounded border border-destructive/50 bg-destructive/10 p-3 text-destructive text-sm mb-4">
          {actionError}
        </div>
      )}
      {actionSuccess && (
        <div className="rounded border border-success/50 bg-success/10 p-3 text-success-foreground text-sm mb-4">
          {actionSuccess}
        </div>
      )}

      {isLoading || !status ? (
        <PageLoader message="Loading tick status..." />
      ) : (
        <div className="space-y-6">
          {/* Status */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Current Status</h2>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <div className="text-xs text-muted-foreground">Current Tick</div>
                <div className="text-lg font-bold">{status.currentTick}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Status</div>
                <div className="text-lg font-bold">
                  {status.isPaused ? (
                    <span className="text-warning">Paused</span>
                  ) : (
                    <span className="text-success-foreground">Running</span>
                  )}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Tick Duration</div>
                <div>{status.effectiveTickDurationSeconds}s</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Default Duration</div>
                <div>{status.defaultTickDurationSeconds}s</div>
              </div>
            </div>
          </div>

          {/* Pause / Resume */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Tick Engine</h2>
            <div className="flex gap-2">
              <button
                onClick={() => pauseMutation.mutate()}
                disabled={pauseMutation.isPending || status.isPaused}
                className="px-4 py-2 rounded bg-warning text-warning-foreground text-sm hover:bg-warning/90 disabled:opacity-50"
              >
                Pause
              </button>
              <button
                onClick={() => resumeMutation.mutate()}
                disabled={resumeMutation.isPending || !status.isPaused}
                className="px-4 py-2 rounded bg-success text-success-foreground text-sm hover:bg-success/90 disabled:opacity-50"
              >
                Resume
              </button>
            </div>
          </div>

          {/* Force Tick */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Force Tick</h2>
            <div className="flex items-end gap-2">
              <div>
                <label className="block text-xs font-medium mb-1">Tick Count</label>
                <input
                  type="number"
                  min={1}
                  max={1000}
                  value={tickCount}
                  onChange={(e) => setTickCount(Math.max(1, Math.min(1000, parseInt(e.target.value) || 1)))}
                  className="w-24 rounded border border-input bg-background px-3 py-2 text-sm"
                />
              </div>
              <button
                onClick={() => forceTickMutation.mutate(tickCount)}
                disabled={forceTickMutation.isPending}
                className="px-4 py-2 rounded bg-primary text-primary-foreground text-sm hover:bg-primary/90 disabled:opacity-50"
              >
                {forceTickMutation.isPending ? 'Running...' : 'Force Tick'}
              </button>
            </div>
          </div>

          {/* Set Tick Duration */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h2 className="text-lg font-semibold mb-3">Override Tick Duration</h2>
            <div className="flex items-end gap-2">
              <div>
                <label className="block text-xs font-medium mb-1">Seconds (1-86400)</label>
                <input
                  type="number"
                  min={1}
                  max={86400}
                  placeholder={String(status.effectiveTickDurationSeconds)}
                  value={tickDuration}
                  onChange={(e) => setTickDuration(e.target.value)}
                  className="w-32 rounded border border-input bg-background px-3 py-2 text-sm"
                />
              </div>
              <button
                onClick={() => {
                  const secs = parseInt(tickDuration)
                  if (secs >= 1 && secs <= 86400) setDurationMutation.mutate(secs)
                }}
                disabled={setDurationMutation.isPending || !tickDuration}
                className="px-4 py-2 rounded bg-primary text-primary-foreground text-sm hover:bg-primary/90 disabled:opacity-50"
              >
                Set Duration
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
