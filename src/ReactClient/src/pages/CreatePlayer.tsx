import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import apiClient from '@/api/client'
import type { CreatePlayerRequest } from '@/api/types'

export function CreatePlayer() {
  const navigate = useNavigate()
  const [playerName, setPlayerName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (req: CreatePlayerRequest) =>
      apiClient.post('/api/playerprofile/create', req),
    onSuccess: () => {
      void navigate('/games?welcome=1')
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to create player.'
      setError(String(msg))
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!playerName.trim()) {
      setError('Please enter a commander name.')
      return
    }
    setError(null)
    createMutation.mutate({ playerName: playerName.trim(), playerType: '' })
  }

  return (
    <div className="flex items-center justify-center min-h-[80vh] px-4">
      <div className="w-full max-w-sm rounded-lg border border-border bg-card p-8 space-y-6">
        <div className="text-center space-y-2">
          <div className="flex justify-center">
            <svg className="w-11 h-11 fill-primary" aria-hidden="true" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M7.97 16L5 19.03A2 2 0 0 1 2 17.56V17l2.5-9.5A5 5 0 0 1 9.37 4h5.26a5 5 0 0 1 4.87 3.5L22 17v.56a2 2 0 0 1-3 1.47L16.03 16H7.97zM6 10h2v2h2v-2h2V8h-2V6h-2v2H6v2zm10 0a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm2-3a1 1 0 1 0 0-2 1 1 0 0 0 0 2z" />
            </svg>
          </div>
          <h1 className="text-xl font-bold">Welcome to BGE</h1>
          <p className="text-muted-foreground text-sm">Choose your commander name to begin</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div role="alert" className="rounded border border-danger/50 bg-danger/10 p-2 text-sm text-danger-foreground">{error}</div>
          )}

          <div className="space-y-1">
            <label htmlFor="playerName" className="text-sm font-medium">
              Commander name
            </label>
            <input
              id="playerName"
              type="text"
              className="w-full rounded border border-border bg-background px-3 py-2 text-sm placeholder-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              value={playerName}
              onChange={(e) => setPlayerName(e.target.value)}
              placeholder="Enter your name"
              maxLength={32}
              autoFocus
            />
          </div>

          <button
            type="submit"
            className="w-full py-2.5 rounded bg-primary text-primary-foreground text-sm font-semibold hover:opacity-90 disabled:opacity-50 transition-colors"
            disabled={createMutation.isPending}
          >
            {createMutation.isPending ? 'Entering game...' : 'Enter the game'}
          </button>
        </form>
      </div>
    </div>
  )
}
