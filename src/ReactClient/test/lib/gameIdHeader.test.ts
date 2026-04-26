import { describe, it, expect, afterEach } from 'vitest'
import { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { gameIdFromPath } from '@/lib/gameIdFromPath'
import apiClient from '@/api/client'

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
		expect(gameIdFromPath('/admin/players')).toBeNull()
		expect(gameIdFromPath('/admin/audit')).toBeNull()
	})

	it('extracts gameId from a per-game admin path', () => {
		expect(gameIdFromPath('/admin/players/abc123')).toBe('abc123')
		expect(gameIdFromPath('/admin/ticks/abc123')).toBe('abc123')
		expect(gameIdFromPath('/admin/stats/abc123')).toBe('abc123')
		expect(gameIdFromPath('/admin/metrics/abc123')).toBe('abc123')
	})
})

describe('apiClient X-Game-Id interceptor', () => {
	const originalLocation = window.location

	function setPath(pathname: string) {
		Object.defineProperty(window, 'location', {
			configurable: true,
			writable: true,
			value: { ...originalLocation, pathname },
		})
	}

	afterEach(() => {
		Object.defineProperty(window, 'location', {
			configurable: true,
			writable: true,
			value: originalLocation,
		})
	})

	function makeConfig(): InternalAxiosRequestConfig {
		return { headers: new AxiosHeaders() } as InternalAxiosRequestConfig
	}

	function runInterceptor(config: InternalAxiosRequestConfig): InternalAxiosRequestConfig {
		// Access the registered request interceptor — runs only the X-Game-Id one.
		const handlers = (apiClient.interceptors.request as unknown as {
			handlers: Array<{ fulfilled?: (c: InternalAxiosRequestConfig) => InternalAxiosRequestConfig }>
		}).handlers
		const fulfilled = handlers[0]?.fulfilled
		if (!fulfilled) throw new Error('expected request interceptor to be registered')
		return fulfilled(config)
	}

	it('adds X-Game-Id header when on a game-scoped page', () => {
		setPath('/games/abc123/base')
		const result = runInterceptor(makeConfig())
		expect(result.headers.get('X-Game-Id')).toBe('abc123')
	})

	it('does not add X-Game-Id header on non-game pages', () => {
		setPath('/profile')
		const result = runInterceptor(makeConfig())
		expect(result.headers.get('X-Game-Id')).toBeUndefined()
	})
})
