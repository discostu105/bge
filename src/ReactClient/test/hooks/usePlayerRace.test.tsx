import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { usePlayerRace } from '@/hooks/usePlayerRace'
import apiClient from '@/api/client'

vi.mock('@/api/client', () => ({ default: { get: vi.fn() } }))

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>
}

describe('usePlayerRace', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-race')
    vi.clearAllMocks()
  })

  it('sets data-race on <html> when API returns terran', async () => {
    ;(apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { playerType: 'terran', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
    })
    renderHook(() => usePlayerRace(), { wrapper })
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-race')).toBe('terran')
    })
  })

  it('does not set data-race for unknown races', async () => {
    ;(apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { playerType: 'alien', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
    })
    renderHook(() => usePlayerRace(), { wrapper })
    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalled()
    })
    expect(document.documentElement.getAttribute('data-race')).toBeNull()
  })
})
