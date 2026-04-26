import { describe, it, expect } from 'vitest'
import { gameIdFromPath } from '@/lib/gameIdFromPath'

describe('gameIdFromPath', () => {
	it('extracts gameId from a game-scoped path', () => {
		expect(gameIdFromPath('/games/abc123/base')).toBe('abc123')
		expect(gameIdFromPath('/games/abc123/units')).toBe('abc123')
		expect(gameIdFromPath('/games/97ad41885175/enemybase/p1')).toBe('97ad41885175')
	})

	it('returns null when path is not game-scoped', () => {
		expect(gameIdFromPath('/')).toBeNull()
		expect(gameIdFromPath('/games')).toBeNull()
		expect(gameIdFromPath('/games/')).toBeNull()
		expect(gameIdFromPath('/profile')).toBeNull()
		expect(gameIdFromPath('/admin/games')).toBeNull()
	})
})
