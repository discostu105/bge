import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import { Countdown } from './countdown'

describe('Countdown', () => {
  beforeEach(() => { vi.useFakeTimers() })
  afterEach(() => { vi.useRealTimers() })

  it('renders time remaining to the target', () => {
    const target = new Date(Date.now() + 65_000).toISOString()
    render(<Countdown to={target} />)
    expect(screen.getByText(/1m/)).toBeInTheDocument()
  })

  it('updates as time advances', () => {
    const target = new Date(Date.now() + 120_000).toISOString()
    render(<Countdown to={target} />)
    expect(screen.getByText(/2m/)).toBeInTheDocument()
    act(() => { vi.advanceTimersByTime(61_000) })
    expect(screen.getByText(/59s/)).toBeInTheDocument()
  })

  it('renders "ended" when target is past', () => {
    const target = new Date(Date.now() - 5000).toISOString()
    render(<Countdown to={target} />)
    expect(screen.getByText('ended')).toBeInTheDocument()
  })
})
