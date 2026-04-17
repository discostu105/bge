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
	test('base page renders WorkerAssignment component with correct structure', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Ensure we have at least some WBF workers so the component is meaningful
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=3`, { data: {} })

		await page.goto(`/games/${gameId}/base?tab=economy`)
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()

		await expect(page.getByRole('heading', { name: 'Worker Assignment' })).toBeVisible()
		await expect(page.getByText('Total')).toBeVisible()
		await expect(page.getByText(/Mining/)).toBeVisible()
		// Use a label-specific locator to avoid matching unrelated 'Gas' occurrences on the page
		await expect(page.getByText('Gas Workers')).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('worker assignment inputs are present and accept numeric input', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build some workers so assignment is possible
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		await page.goto(`/games/${gameId}/base?tab=economy`)
		await expect(page.getByRole('heading', { name: 'Worker Assignment' })).toBeVisible()

		await expect(page.getByLabel('Mineral Workers')).toBeVisible()
		await expect(page.getByLabel('Gas Workers')).toBeVisible()
	})

	test('assigning workers via API is reflected in the base page UI', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build workers so we have something to assign
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		// Assign workers via API
		const assignRes = await page.request.post(
			`${baseURL}/api/workers/assign?mineralWorkers=2&gasWorkers=2`,
			{ data: {} }
		)
		expect([200, 204]).toContain(assignRes.status())

		// Navigate to base and confirm the assignment is reflected
		await page.goto(`/games/${gameId}/base?tab=economy`)
		await expect(page.getByRole('heading', { name: 'Worker Assignment' })).toBeVisible()

		await expect(page.getByLabel('Mineral Workers')).toHaveValue('2')
		await expect(page.getByLabel('Gas Workers')).toHaveValue('2')
	})

	test('changing worker assignment via the UI input sends an update', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Build workers and reset assignment to a known state
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })
		await page.request.post(`${baseURL}/api/workers/assign?mineralWorkers=1&gasWorkers=1`, { data: {} })

		await page.goto(`/games/${gameId}/base?tab=economy`)
		await expect(page.getByRole('heading', { name: 'Worker Assignment' })).toBeVisible()

		// Set a new value for mineral workers; wait for the assign API call to complete
		// before reading back the state (onChange fires immediately on fill).
		const mineralInput = page.getByLabel('Mineral Workers')
		const assignPromise = page.waitForResponse(
			(r) => r.url().includes('/api/workers/assign') && r.request().method() === 'POST'
		)
		await mineralInput.fill('3')
		await assignPromise

		// Verify no error boundary was hit
		await expect(page.getByText('Something went wrong')).not.toBeVisible()

		// Verify the API reflects the change
		const workersRes = await page.request.get(`${baseURL}/api/workers`)
		expect(workersRes.ok()).toBeTruthy()
		const workers = await workersRes.json() as { mineralWorkers: number }
		expect(workers.mineralWorkers).toBe(3)
	})
})
