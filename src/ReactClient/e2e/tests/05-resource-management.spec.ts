import { test, expect } from '@playwright/test'

/**
 * Resource management tests: worker assignment on the base page.
 *
 * The server architecture uses a single default game world state for all in-game
 * API calls (/api/workers, /api/assets, etc.). The global-setup already places the
 * e2e-test-admin user into the default game. We create a game record here purely
 * so we have a valid gameId for React router navigation — the actual state mutations
 * always hit the default game.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Resource Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	// Enroll in the new game record so the game page loads properly
	await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, { data: {} })
	return game.gameId
}

test.describe('Resource management — worker assignment', () => {
	test('base page shows Workers stat card with Assign action', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Ensure we have at least some WBF workers so the card is meaningful
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=3`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()

		// Workers stat card is in the economy strip, with an Assign button.
		await expect(page.getByText('Workers', { exact: true })).toBeVisible()
		await expect(page.getByRole('button', { name: 'Assign' })).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('worker assignment dialog opens with count inputs', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build some workers so assignment is possible
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Assign' }).click()

		// Dialog title and both count inputs should appear.
		await expect(page.getByRole('dialog')).toBeVisible()
		await expect(page.getByRole('heading', { name: /assign workers/i })).toBeVisible()
		const dialog = page.getByRole('dialog')
		await expect(dialog.getByRole('spinbutton')).toHaveCount(2)
	})

	test('assigning workers via API is reflected in the worker dialog', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build workers so we have something to assign
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		// Assign workers via API
		const assignRes = await page.request.post(
			`${baseURL}/api/workers/assign?mineralWorkers=2&gasWorkers=2`,
			{ data: {} }
		)
		expect([200, 204]).toContain(assignRes.status())

		// Open the dialog and verify the two count inputs reflect the server state.
		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Assign' }).click()
		await expect(page.getByRole('dialog')).toBeVisible()
		const spinners = page.getByRole('dialog').getByRole('spinbutton')
		await expect(spinners.nth(0)).toHaveValue('2')
		await expect(spinners.nth(1)).toHaveValue('2')
	})

	test('saving worker assignment from the dialog sends an update', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build workers and reset assignment to a known state
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })
		await page.request.post(`${baseURL}/api/workers/assign?mineralWorkers=1&gasWorkers=1`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Assign' }).click()
		await expect(page.getByRole('dialog')).toBeVisible()

		// Set minerals to 3 via the first spinbutton, then save.
		const spinners = page.getByRole('dialog').getByRole('spinbutton')
		await spinners.nth(0).fill('3')

		const assignPromise = page.waitForResponse(
			(r) => r.url().includes('/api/workers/assign') && r.request().method() === 'POST'
		)
		await page.getByRole('button', { name: /save assignment/i }).click()
		await assignPromise

		await expect(page.getByText('Something went wrong')).not.toBeVisible()

		// Verify the API reflects the change
		const workersRes = await page.request.get(`${baseURL}/api/workers`)
		expect(workersRes.ok()).toBeTruthy()
		const workers = await workersRes.json() as { mineralWorkers: number }
		expect(workers.mineralWorkers).toBe(3)
	})
})
