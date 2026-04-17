import { describe, it, expect } from 'vitest'
import { bucketBadges } from './useNavBadges'

describe('bucketBadges', () => {
  it('returns zero counts on empty input', () => {
    expect(bucketBadges([])).toEqual({ messages: 0, alliances: 0, diplomacy: 0 })
  })

  it('groups notification kinds by nav section', () => {
    const got = bucketBadges([
      { kind: 'MessageReceived', isRead: false },
      { kind: 'MessageReceived', isRead: false },
      { kind: 'AllianceRequest', isRead: false },
      { kind: 'AttackReceived', isRead: false },
      { kind: 'MessageReceived', isRead: true }, // read — excluded
      { kind: 'Info', isRead: false }, // unrelated — excluded
    ])
    expect(got).toEqual({ messages: 2, alliances: 1, diplomacy: 1 })
  })
})
