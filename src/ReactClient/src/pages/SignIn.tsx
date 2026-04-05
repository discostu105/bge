import { useState } from 'react'
import { GithubIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

export function SignIn() {
  const [devPlayerId, setDevPlayerId] = useState('')
  const raw = new URLSearchParams(window.location.search).get('returnUrl') ?? '/'
  const returnUrl = raw.startsWith('/') && !raw.startsWith('//') ? raw : '/'

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="w-full max-w-sm space-y-6">
        {/* Header */}
        <div className="text-center">
          <div className="text-4xl mb-2">🚀</div>
          <h1 className="text-2xl font-bold">Age of Agents</h1>
          <p className="text-muted-foreground text-sm mt-1">Sign in to continue</p>
        </div>

        {/* GitHub OAuth */}
        <div className="rounded-lg border bg-card p-6 space-y-4">
          <form method="post" action="/signin">
            <input type="hidden" name="provider" value="GitHub" />
            <input type="hidden" name="returnUrl" value={returnUrl} />
            <button
              type="submit"
              className={cn(
                'w-full flex items-center justify-center gap-2',
                'bg-[#24292e] hover:bg-[#1a1e22] text-white',
                'rounded-md px-4 py-2.5 text-sm font-medium transition-colors'
              )}
            >
              <GithubIcon className="h-4 w-4" />
              Sign in with GitHub
            </button>
          </form>
        </div>

        {/* DevAuth — rendered only in dev builds (VITE_DEV_AUTH=true) */}
        {import.meta.env.VITE_DEV_AUTH === 'true' && (
          <div className="rounded-lg border border-dashed border-yellow-700 bg-yellow-900/10 p-4 space-y-3">
            <p className="text-xs font-semibold text-yellow-400 uppercase tracking-wide">
              Dev Login
            </p>
            <form
              method="post"
              action="/signindev"
              className="flex gap-2"
              onSubmit={(e) => {
                if (!devPlayerId.trim()) e.preventDefault()
              }}
            >
              <input type="hidden" name="returnUrl" value={returnUrl} />
              <input
                name="playerid"
                value={devPlayerId}
                onChange={(e) => setDevPlayerId(e.target.value)}
                placeholder="Player ID"
                className={cn(
                  'flex-1 rounded-md border bg-background px-3 py-1.5 text-sm',
                  'placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring'
                )}
              />
              <button
                type="submit"
                className="rounded-md bg-yellow-700 hover:bg-yellow-600 text-white px-3 py-1.5 text-sm font-medium transition-colors"
              >
                Login
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  )
}
