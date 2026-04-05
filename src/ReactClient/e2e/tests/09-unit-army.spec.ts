import { test, expect } from '@playwright/test'

/**
 * Unit build → army tests.
 *
 * Verifies that building a unit via the API causes it to appear on the Units
 * page. The in-game state lives in the default game world; we create a game
 * record only for React router navigation.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Army Game ${Date.now()}`,
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

test.describe('Unit build — army visibility', () => {
	test('units page renders heading and shows unit count after building', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build WBF units via API
		const buildRes = await page.request.post(
			`${baseURL}/api/units/build?unitDefId=wbf&count=3`,
			{ data: {} }
		)
		expect(buildRes.ok()).toBeTruthy()

		await page.goto(`/games/${gameId}/units`)
		await expect(page.getByRole('heading', { name: 'Units' })).toBeVisible()

		// The units page should report at least 3 total units
		const countLine = page.getByText(/units in \d+ stack/)
		await expect(countLine).toBeVisible({ timeout: 10_000 })

		const text = await countLine.textContent()
		const match = text?.match(/^(\d[\d,]*)/)
		const total = parseInt((match?.[1] ?? '0').replace(',', ''), 10)
		expect(total).toBeGreaterThanOrEqual(3)
	})

	test('units page renders without errors when army is empty', async ({ page }) => {
		// Fresh user — no units in army yet
		const freshUserId = `e2e-army-${Date.now()}`
		await page.request.post(`${baseURL}/signindev`, {
			form: { playerid: freshUserId, returnUrl: '/' },
		})

		const gameId = await createNavGame(page)

		await page.goto(`/games/${gameId}/units`)
		await expect(page.getByRole('heading', { name: 'Units' })).toBeVisible()

		// Empty state message should be shown
		await expect(page.getByText('Build units from your base buildings')).toBeVisible({
			timeout: 10_000,
		})
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})
