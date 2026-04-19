import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { GameCountdown } from '@/components/games/GameCountdown'

describe('GameCountdown', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('shows days + hours + minutes when over 1 day away', () => {
    const to = new Date(Date.now() + (3 * 86400_000 + 12 * 3600_000 + 4 * 60_000)).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^3d 12h 04m$/)).toBeDefined()
  })

  it('shows hours + minutes when under 1 day', () => {
    const to = new Date(Date.now() + (5 * 3600_000 + 30 * 60_000)).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^5h 30m$/)).toBeDefined()
  })

  it('shows "0m" when the target is in the past', () => {
    const to = new Date(Date.now() - 60_000).toISOString()
    render(<GameCountdown to={to} />)
    expect(screen.getByText(/^0m$/)).toBeDefined()
  })
})
