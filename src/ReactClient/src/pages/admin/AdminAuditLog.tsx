import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import axios from 'axios'
import apiClient from '@/api/client'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { AdminNav } from './AdminNav'

interface AuditLogEntry {
  id: string
  timestamp: string
  actionType: string
  actorUserId: string | null
  description: string
  targetPlayerId: string | null
}

interface AuditLogPage {
  entries: AuditLogEntry[]
  total: number
  page: number
  pageSize: number
}

const ACTION_TYPES = ['all', 'BanPlayer', 'UnbanPlayer', 'SetResources', 'ReportAction']

export function AdminAuditLog() {
  const [page, setPage] = useState(0)
  const [actionType, setActionType] = useState('all')

  const { data, isLoading, error, refetch } = useQuery<AuditLogPage>({
    queryKey: ['admin-audit-log', page, actionType],
    queryFn: () =>
      apiClient
        .get('/api/admin/audit-log', { params: { page, pageSize: 20, actionType } })
        .then((r) => r.data),
  })

  const totalPages = data ? Math.ceil(data.total / 20) : 0

  if (error) {
    if (axios.isAxiosError(error) && error.response?.status === 403) {
      return (
        <div className="p-6 max-w-2xl mx-auto">
          <h1 className="text-2xl font-bold mb-4">Audit Log</h1>
          <div className="rounded border border-warning/50 bg-warning/10 p-4 text-warning-foreground">
            You are not authorized to view the admin panel.
          </div>
        </div>
      )
    }
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold mb-4">Audit Log</h1>
        <ApiError message="Failed to load audit log." onRetry={() => void refetch()} />
      </div>
    )
  }

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <AdminNav />
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Audit Log</h1>
        <select
          value={actionType}
          onChange={(e) => { setActionType(e.target.value); setPage(0) }}
          className="border border-border rounded px-2 py-1 text-sm bg-background"
        >
          {ACTION_TYPES.map((t) => (
            <option key={t} value={t}>{t === 'all' ? 'All actions' : t}</option>
          ))}
        </select>
      </div>

      {isLoading ? (
        <PageLoader message="Loading audit log..." />
      ) : !data || data.entries.length === 0 ? (
        <p className="text-muted-foreground">No audit entries found.</p>
      ) : (
        <>
          <div className="rounded-lg border border-border overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  <th scope="col" className="px-3 py-2 text-left font-medium">Time</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">Action</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">Description</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium hidden md:table-cell">Actor</th>
                </tr>
              </thead>
              <tbody>
                {data.entries.map((entry) => (
                  <tr key={entry.id} className="border-b border-border/50">
                    <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap">
                      {new Date(entry.timestamp).toLocaleString()}
                    </td>
                    <td className="px-3 py-2">
                      <span className="text-xs px-2 py-0.5 rounded bg-secondary font-mono">
                        {entry.actionType}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{entry.description}</td>
                    <td className="px-3 py-2 text-xs text-muted-foreground font-mono hidden md:table-cell">
                      {entry.actorUserId ?? '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="mt-4 flex items-center justify-end gap-2">
              <button
                onClick={() => setPage((p) => Math.max(0, p - 1))}
                disabled={page === 0}
                className="px-3 py-1 rounded border border-border text-sm hover:bg-muted disabled:opacity-40"
              >
                Previous
              </button>
              <span className="text-sm text-muted-foreground">
                Page {page + 1} of {totalPages} ({data.total} entries)
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
                disabled={page >= totalPages - 1}
                className="px-3 py-1 rounded border border-border text-sm hover:bg-muted disabled:opacity-40"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
