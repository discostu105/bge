import { test, expect } from '@playwright/test'

/**
 * Spectator mode E2E tests (BGE-849).
 *
 * Tests the anonymous spectator page at /games/:gameId/spectate and the backing
 * REST endpoint GET /api/games/:gameId/spectate. Both are AllowAnonymous.
 *
 * The page shows a live leaderboard via an initial REST fetch plus a SignalR
 * connection to /hubs/spectator?gameId=… for real-time updates.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createActiveGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Spectator Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	// Join as admin player so the snapshot has at least one player
	await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, { data: {} })
	return game.gameId as string
}

test.describe('Spectator mode', () => {
	test('REST endpoint returns snapshot with expected fields', async ({ page }) => {
		const gameId = await createActiveGame(page)

		const res = await page.request.get(`${baseURL}/api/games/${gameId}/spectate`)
		expect(res.ok()).toBeTruthy()

		const snapshot = await res.json() as {
			gameId: string
			gameName: string
			gameStatus: string
			topPlayers: unknown[]
			tick: number
		}

		expect(snapshot.gameId).toBe(gameId)
		expect(typeof snapshot.gameName).toBe('string')
		expect(typeof snapshot.gameStatus).toBe('string')
		expect(Array.isArray(snapshot.topPlayers)).toBe(true)
		expect(typeof snapshot.tick).toBe('number')
	})

	test('spectate page renders heading and player table', async ({ page }) => {
		const gameId = await createActiveGame(page)

		await page.goto(`/games/${gameId}/spectate`)

		// Heading is either the game name (once data loads) or the fallback
		await expect(
			page.getByRole('heading', { level: 1 })
		).toBeVisible({ timeout: 10_000 })

		// Player table should render
		await expect(page.locator('table')).toBeVisible({ timeout: 10_000 })

		// Column headers from Spectator.tsx
		await expect(page.getByRole('columnheader', { name: 'Player' })).toBeVisible()
		await expect(page.getByRole('columnheader', { name: 'Land' })).toBeVisible()

		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('spectate page shows connection status indicator', async ({ page }) => {
		const gameId = await createActiveGame(page)

		await page.goto(`/games/${gameId}/spectate`)

		// The page renders a SignalR connection status: "● Live" or "○ Connecting…"
		await expect(
			page.getByText(/live/i).or(page.getByText(/connecting/i))
		).toBeVisible({ timeout: 10_000 })
	})

	test('spectate page shows "Game Lobby →" navigation link', async ({ page }) => {
		const gameId = await createActiveGame(page)

		await page.goto(`/games/${gameId}/spectate`)

		// Spectator.tsx always renders the "Game Lobby →" link
		await expect(page.getByRole('link', { name: /game lobby/i })).toBeVisible({ timeout: 10_000 })
	})

	test('REST endpoint returns 404 for unknown game', async ({ page }) => {
		const res = await page.request.get(`${baseURL}/api/games/no-such-game-id/spectate`)
		expect(res.status()).toBe(404)
	})
})
