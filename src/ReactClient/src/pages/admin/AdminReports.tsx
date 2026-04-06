import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { AdminNav } from './AdminNav'

interface Report {
  id: string
  createdAt: string
  reporterUserId: string
  targetUserId: string
  reason: string
  details: string | null
  status: 'Pending' | 'Resolved' | 'Dismissed'
  resolvedByUserId: string | null
  resolutionNote: string | null
  resolvedAt: string | null
}

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-warning/20 text-warning-foreground',
  Resolved: 'bg-success/20 text-success-foreground',
  Dismissed: 'bg-muted text-muted-foreground',
}

export function AdminReports() {
  const queryClient = useQueryClient()
  const [actionError, setActionError] = useState<string | null>(null)
  const [noteInputs, setNoteInputs] = useState<Record<string, string>>({})

  const { data: reports, isLoading, error, refetch } = useQuery<Report[]>({
    queryKey: ['admin-reports'],
    queryFn: () => apiClient.get('/api/admin/reports').then((r) => r.data),
  })

  const actionMutation = useMutation({
    mutationFn: ({ id, action, note }: { id: string; action: string; note: string }) =>
      apiClient.post(`/api/admin/reports/${id}/action`, { action, note }),
    onSuccess: () => {
      setActionError(null)
      void queryClient.invalidateQueries({ queryKey: ['admin-reports'] })
    },
    onError: (err) => {
      setActionError(axios.isAxiosError(err) ? `Action failed: ${err.response?.data ?? err.message}` : 'Unknown error.')
    },
  })

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <h1 className="text-2xl font-bold mb-4">Reports</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold mb-4">Reports</h1>
        <ApiError message="Failed to load reports." onRetry={() => void refetch()} />
      </div>
    )
  }

  const pendingCount = reports?.filter((r) => r.status === 'Pending').length ?? 0

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <AdminNav />
      <div className="flex items-center gap-3 mb-6">
        <h1 className="text-2xl font-bold">Reports</h1>
        {pendingCount > 0 && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-destructive text-destructive-foreground font-bold">
            {pendingCount} pending
          </span>
        )}
      </div>

      {actionError && (
        <div className="rounded border border-destructive/50 bg-destructive/10 p-3 text-destructive text-sm mb-4">
          {actionError}
        </div>
      )}

      {isLoading ? (
        <PageLoader message="Loading reports..." />
      ) : !reports || reports.length === 0 ? (
        <p className="text-muted-foreground">No reports submitted yet.</p>
      ) : (
        <div className="space-y-3">
          {reports.map((report) => (
            <div key={report.id} className="rounded-lg border border-border bg-card p-4">
              <div className="flex items-start justify-between gap-4 mb-2">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className={`text-xs px-2 py-0.5 rounded font-medium ${STATUS_COLORS[report.status] ?? ''}`}>
                      {report.status}
                    </span>
                    <span className="text-xs text-muted-foreground font-mono">{report.id}</span>
                    <span className="text-xs text-muted-foreground">
                      {new Date(report.createdAt).toLocaleString()}
                    </span>
                  </div>
                  <div className="text-sm">
                    <span className="text-muted-foreground">Reporter:</span>{' '}
                    <span className="font-mono text-xs">{report.reporterUserId}</span>
                    {' → '}
                    <span className="text-muted-foreground">Target:</span>{' '}
                    <span className="font-mono text-xs">{report.targetUserId}</span>
                  </div>
                  <div className="text-sm font-medium">{report.reason}</div>
                  {report.details && (
                    <div className="text-sm text-muted-foreground">{report.details}</div>
                  )}
                  {report.resolutionNote && (
                    <div className="text-xs text-muted-foreground mt-1">
                      Resolution: {report.resolutionNote}
                    </div>
                  )}
                </div>
              </div>
              {report.status === 'Pending' && (
                <div className="flex items-center gap-2 mt-3 pt-3 border-t border-border/50">
                  <input
                    type="text"
                    placeholder="Resolution note (optional)"
                    className="flex-1 border border-border rounded px-2 py-1 text-xs bg-background"
                    value={noteInputs[report.id] ?? ''}
                    onChange={(e) => setNoteInputs((prev) => ({ ...prev, [report.id]: e.target.value }))}
                  />
                  <button
                    onClick={() => actionMutation.mutate({ id: report.id, action: 'Resolved', note: noteInputs[report.id] ?? '' })}
                    disabled={actionMutation.isPending}
                    className="px-3 py-1 rounded bg-success text-success-foreground text-xs hover:bg-success/90 disabled:opacity-50"
                  >
                    Resolve
                  </button>
                  <button
                    onClick={() => actionMutation.mutate({ id: report.id, action: 'Dismissed', note: noteInputs[report.id] ?? '' })}
                    disabled={actionMutation.isPending}
                    className="px-3 py-1 rounded border border-border text-xs hover:bg-muted disabled:opacity-50"
                  >
                    Dismiss
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
