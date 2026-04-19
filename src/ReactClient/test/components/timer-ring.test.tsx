import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { TimerRing } from '@/components/ui/timer-ring'

describe('TimerRing', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('renders mm:ss format when time remaining is above 60s', () => {
    const to = new Date(Date.now() + 3 * 60_000 + 42_000).toISOString()
    render(<TimerRing to={to} size={34} />)
    expect(screen.getByText(/^3:42$/)).toBeDefined()
  })

  it('renders 0:ss format when time remaining is under 60s', () => {
    const to = new Date(Date.now() + 15_000).toISOString()
    render(<TimerRing to={to} size={34} />)
    expect(screen.getByText(/^0:15$/)).toBeDefined()
  })

  it('renders the stroke arc proportional to remaining fraction', () => {
    const to = new Date(Date.now() + 50_000).toISOString()
    const { container } = render(<TimerRing to={to} size={34} periodMs={100_000} />)
    const arc = container.querySelectorAll('circle')[1]
    expect(arc.getAttribute('stroke-dashoffset')).toBeDefined()
  })
})
