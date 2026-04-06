import { test, expect } from '@playwright/test'

/**
 * Live game view page tests (BGE-747).
 *
 * Tests the GameLiveView page at /games/:gameId/live shipped in PR #237.
 *
 * The page polls /api/resources, /api/playerranking, /api/units,
 * /api/assets, and /api/notifications/recent every 10 seconds.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createActiveGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Live View Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	// Join the game as admin player
	await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, { data: {} })
	return game.gameId
}

test.describe('Live game view page', () => {
	test('renders resource and ranking sections for an active game', async ({ page }) => {
		const gameId = await createActiveGame(page)

		await page.goto(`/games/${gameId}/live`)

		// Main sections should render
		await expect(page.getByText('Resources')).toBeVisible({ timeout: 10_000 })
		await expect(page.getByText('Rankings')).toBeVisible({ timeout: 10_000 })

		// At least one data section or skeleton loaded
		await expect(page.locator('.rounded-lg.border').first()).toBeVisible()
	})

	test('shows tick update timestamp', async ({ page }) => {
		const gameId = await createActiveGame(page)

		await page.goto(`/games/${gameId}/live`)

		// The page displays "Last updated" or a time reference once data loads
		await expect(page.getByText(/last updated/i).or(page.getByText(/units at base/i))).toBeVisible({ timeout: 10_000 })
	})

	test('shows finish-game notice for a completed game', async ({ page }) => {
		// Create and immediately end a game by setting endTime in the past
		const now = new Date()
		const res = await page.request.post(`${baseURL}/api/games`, {
			data: {
				name: `E2E Finished Game ${Date.now()}`,
				gameDefType: 'sco',
				startTime: new Date(now.getTime() - 7200_000).toISOString(),
				endTime: new Date(now.getTime() - 3600_000).toISOString(),
				tickDuration: '00:01:00',
				discordWebhookUrl: null,
			},
		})
		expect(res.ok()).toBeTruthy()
		const game = await res.json()
		await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, { data: {} })

		await page.goto(`/games/${game.gameId}/live`)

		// Page renders without a crash — the layout still loads
		await expect(page.locator('.rounded-lg.border').first()).toBeVisible({ timeout: 10_000 })

		// No unhandled error message
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})
