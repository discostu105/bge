import { describe, it, expect } from 'vitest'
import { normalizeRace, RACES, type Race } from '@/lib/race'

describe('race helpers', () => {
  it('exposes the three canonical races', () => {
    expect(RACES).toEqual(['terran', 'zerg', 'protoss'])
  })

  it('normalizes known ids case-insensitively', () => {
    expect(normalizeRace('Terran')).toBe('terran')
    expect(normalizeRace('ZERG')).toBe('zerg')
    expect(normalizeRace('protoss')).toBe('protoss')
  })

  it('returns null for unknown or empty input', () => {
    expect(normalizeRace(null)).toBeNull()
    expect(normalizeRace('')).toBeNull()
    expect(normalizeRace('undead')).toBeNull()
  })
})
