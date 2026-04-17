import { describe, it, expect } from 'vitest'
import { formatTicks } from './formatters'

describe('formatTicks', () => {
  it('renders a short run in seconds', () => {
    expect(formatTicks(3, 30_000)).toBe('3 ticks (~1m 30s)')
  })
  it('rounds to minutes past 90 seconds', () => {
    expect(formatTicks(4, 30_000)).toBe('4 ticks (~2m)')
  })
  it('falls back to hours when very long', () => {
    expect(formatTicks(240, 30_000)).toBe('240 ticks (~2h)')
  })
  it('handles zero', () => {
    expect(formatTicks(0, 30_000)).toBe('0 ticks')
  })
  it('handles singular', () => {
    expect(formatTicks(1, 30_000)).toBe('1 tick (~30s)')
  })
})
