import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router'
import type { ReactNode } from 'react'
import { usePlayerRace } from '@/hooks/usePlayerRace'
import apiClient from '@/api/client'

vi.mock('@/api/client', () => ({ default: { get: vi.fn() } }))

function makeWrapper(initialPath = '/games/g1/units') {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/games/:gameId/*" element={<>{children}</>} />
            <Route path="*" element={<>{children}</>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    )
  }
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
    renderHook(() => usePlayerRace(), { wrapper: makeWrapper() })
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-race')).toBe('terran')
    })
  })

  it('does not set data-race for unknown races', async () => {
    ;(apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { playerType: 'alien', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
    })
    renderHook(() => usePlayerRace(), { wrapper: makeWrapper() })
    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalled()
    })
    expect(document.documentElement.getAttribute('data-race')).toBeNull()
  })

  // Regression: usePlayerRace previously cached under ['playerprofile'] with no gameId,
  // so navigating from a Zerg game to a Terran game showed stale Zerg in the header.
  it('refetches the profile when gameId in the URL changes', async () => {
    const get = apiClient.get as ReturnType<typeof vi.fn>
    get.mockImplementation(() =>
      Promise.resolve({
        data: { playerType: 'zerg', playerId: '1', playerName: 'x', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
      }),
    )
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    function makeWrapperFor(path: string) {
      return function Wrapper({ children }: { children: ReactNode }) {
        return (
          <QueryClientProvider client={qc}>
            <MemoryRouter initialEntries={[path]}>
              <Routes>
                <Route path="/games/:gameId/*" element={<>{children}</>} />
              </Routes>
            </MemoryRouter>
          </QueryClientProvider>
        )
      }
    }

    const r1 = renderHook(() => usePlayerRace(), { wrapper: makeWrapperFor('/games/zergGame/units') })
    await waitFor(() => expect(get).toHaveBeenCalledTimes(1))
    r1.unmount()

    get.mockImplementation(() =>
      Promise.resolve({
        data: { playerType: 'terran', playerId: '2', playerName: 'y', land: 0, protectionTicksRemaining: 0, isOnline: true, lastOnline: null, tutorialCompleted: true },
      }),
    )
    renderHook(() => usePlayerRace(), { wrapper: makeWrapperFor('/games/terranGame/units') })
    await waitFor(() => expect(get).toHaveBeenCalledTimes(2))
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-race')).toBe('terran')
    })
  })
})
