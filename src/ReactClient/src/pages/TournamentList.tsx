import { useState } from 'react'
import { Link } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/api/client'
import type { TournamentSummaryViewModel, CreateTournamentRequest } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { useCurrentUser } from '@/contexts/CurrentUserContext'

export function TournamentList() {
  const [showCreate, setShowCreate] = useState(false)
  const { user } = useCurrentUser()
  const queryClient = useQueryClient()

  const { data: tournaments, error, isLoading, refetch } = useQuery<TournamentSummaryViewModel[]>({
    queryKey: ['tournaments'],
    queryFn: () => apiClient.get('/api/tournaments').then((r) => r.data),
  })

  const createMutation = useMutation({
    mutationFn: (req: CreateTournamentRequest) =>
      apiClient.post('/api/tournaments', req).then((r) => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['tournaments'] })
      setShowCreate(false)
    },
  })

  if (isLoading) return <PageLoader message="Loading tournaments..." />
  if (error) return <ApiError message="Failed to load tournaments." onRetry={() => void refetch()} />

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Tournaments</h1>
          <p className="text-muted-foreground text-sm mt-1">Competitive brackets and round-robin events</p>
        </div>
        {user && (
          <button
            className="px-4 py-2 bg-primary text-primary-foreground rounded text-sm font-medium hover:bg-primary/90"
            onClick={() => setShowCreate(true)}
          >
            Create Tournament
          </button>
        )}
      </div>

      {showCreate && (
        <CreateTournamentForm
          onSubmit={(req) => createMutation.mutate(req)}
          onCancel={() => setShowCreate(false)}
          error={createMutation.error?.message}
          isSubmitting={createMutation.isPending}
        />
      )}

      {!tournaments || tournaments.length === 0 ? (
        <p className="text-muted-foreground">No tournaments yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-muted-foreground">
                <th className="pb-2 pr-4">Name</th>
                <th className="pb-2 pr-4">Format</th>
                <th className="pb-2 pr-4">Status</th>
                <th className="pb-2 pr-4">Players</th>
                <th className="pb-2">Deadline</th>
              </tr>
            </thead>
            <tbody>
              {tournaments.map((t) => (
                <tr key={t.tournamentId} className="border-b border-border hover:bg-muted/30">
                  <td className="py-2 pr-4">
                    <Link
                      to={`/tournaments/${t.tournamentId}`}
                      className="text-primary hover:underline font-medium"
                    >
                      {t.name}
                    </Link>
                  </td>
                  <td className="py-2 pr-4 text-muted-foreground">{t.format}</td>
                  <td className="py-2 pr-4">
                    <StatusBadge status={t.status} />
                  </td>
                  <td className="py-2 pr-4">
                    {t.registrationCount}
                    {t.maxPlayers > 0 ? ` / ${t.maxPlayers}` : ''}
                  </td>
                  <td className="py-2 text-muted-foreground">
                    {new Date(t.registrationDeadline).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Registration: 'bg-blue-100 text-blue-800',
    InProgress: 'bg-green-100 text-green-800',
    Finished: 'bg-gray-100 text-gray-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${colors[status] ?? 'bg-muted'}`}>
      {status}
    </span>
  )
}

interface CreateTournamentFormProps {
  onSubmit: (req: CreateTournamentRequest) => void
  onCancel: () => void
  error?: string
  isSubmitting: boolean
}

function CreateTournamentForm({ onSubmit, onCancel, error, isSubmitting }: CreateTournamentFormProps) {
  const [name, setName] = useState('')
  const [format, setFormat] = useState('SingleElimination')
  const [deadline, setDeadline] = useState('')
  const [maxPlayers, setMaxPlayers] = useState(0)
  const [gameDefType, setGameDefType] = useState('sco')
  const [tickDuration, setTickDuration] = useState('00:20:00')
  const [matchDurationHours, setMatchDurationHours] = useState(24)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    onSubmit({
      name,
      format,
      registrationDeadline: new Date(deadline).toISOString(),
      maxPlayers,
      gameDefType,
      tickDuration,
      matchDurationHours,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="border border-border rounded-lg p-4 space-y-4 bg-card">
      <h2 className="font-semibold text-lg">Create Tournament</h2>

      {error && <p className="text-destructive text-sm">{error}</p>}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-sm font-medium mb-1">Name</label>
          <input
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Format</label>
          <select
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={format}
            onChange={(e) => setFormat(e.target.value)}
          >
            <option value="SingleElimination">Single Elimination</option>
            <option value="RoundRobin">Round Robin</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Registration Deadline</label>
          <input
            type="datetime-local"
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={deadline}
            onChange={(e) => setDeadline(e.target.value)}
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Max Players (0 = unlimited)</label>
          <input
            type="number"
            min={0}
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={maxPlayers}
            onChange={(e) => setMaxPlayers(Number(e.target.value))}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Game Type</label>
          <input
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={gameDefType}
            onChange={(e) => setGameDefType(e.target.value)}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Tick Duration (HH:MM:SS)</label>
          <input
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={tickDuration}
            onChange={(e) => setTickDuration(e.target.value)}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Match Duration (hours)</label>
          <input
            type="number"
            min={1}
            className="w-full border border-border rounded px-3 py-2 text-sm bg-background"
            value={matchDurationHours}
            onChange={(e) => setMatchDurationHours(Number(e.target.value))}
          />
        </div>
      </div>

      <div className="flex gap-2 justify-end">
        <button
          type="button"
          className="px-4 py-2 text-sm rounded border border-border hover:bg-muted"
          onClick={onCancel}
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="px-4 py-2 text-sm rounded bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          {isSubmitting ? 'Creating...' : 'Create'}
        </button>
      </div>
    </form>
  )
}
