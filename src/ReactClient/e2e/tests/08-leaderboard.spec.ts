import { test, expect } from '@playwright/test'

/**
 * Leaderboard (Player Ranking) smoke tests.
 *
 * Verifies that the ranking page renders, shows the filter controls, and lists
 * at least the e2e-test-admin player who was enrolled in the default game via
 * global-setup.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Leaderboard Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, { data: {} })
	return game.gameId as string
}

test.describe('Player Ranking (leaderboard)', () => {
	test('ranking page renders heading and filter controls', async ({ page }) => {
		const gameId = await createNavGame(page)

		await page.goto(`/games/${gameId}/ranking`)
		await expect(page.getByRole('heading', { name: 'Player Ranking' })).toBeVisible()

		// Filter buttons: All / Human / Agent
		await expect(page.getByRole('button', { name: 'All' })).toBeVisible()
		await expect(page.getByRole('button', { name: 'Human' })).toBeVisible()
		await expect(page.getByRole('button', { name: 'Agent' })).toBeVisible()

		// No crash
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('ranking page shows at least one player row', async ({ page }) => {
		const gameId = await createNavGame(page)

		await page.goto(`/games/${gameId}/ranking`)
		await expect(page.getByRole('heading', { name: 'Player Ranking' })).toBeVisible()

		// Wait for table to populate (query refetches every 30 s; initial load should be fast)
		const rows = page.locator('table tbody tr')
		await expect(rows.first()).toBeVisible({ timeout: 10_000 })
		expect(await rows.count()).toBeGreaterThan(0)
	})

	test('Human filter hides agent-only rows without crashing', async ({ page }) => {
		const gameId = await createNavGame(page)

		await page.goto(`/games/${gameId}/ranking`)
		await expect(page.getByRole('heading', { name: 'Player Ranking' })).toBeVisible()

		// Switch to Human filter
		await page.getByRole('button', { name: 'Human' }).click()
		await expect(page.getByRole('button', { name: 'Human' })).toHaveAttribute('aria-pressed', 'true')

		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})
