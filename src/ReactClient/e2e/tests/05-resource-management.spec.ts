import { test, expect } from '@playwright/test'

/**
 * Resource management tests: worker auto-assignment on the base page.
 *
 * The server architecture uses a single default game world state for all in-game
 * API calls (/api/workers, /api/assets, etc.). The global-setup already places the
 * e2e-test-admin user into the default game. We create a game record here purely
 * so we have a valid gameId for React router navigation — the actual state mutations
 * always hit the default game.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

test.describe('Resource management — worker auto-assignment', () => {
	test('base page shows Workers stat card with Adjust action', async ({ page }) => {
		const gameId = 'default'

		// Ensure we have at least some WBF workers so the card is meaningful
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=3`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()

		// Workers stat card is in the economy strip, with an Adjust button.
		await expect(page.getByText('Workers', { exact: true })).toBeVisible()
		await expect(page.getByRole('button', { name: 'Adjust' })).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('worker assignment dialog opens with a gas-percent slider', async ({ page }) => {
		const gameId = 'default'

		// Build some workers so assignment is possible
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Adjust' }).click()

		// Dialog title and the gas-percent slider should appear.
		await expect(page.getByRole('dialog')).toBeVisible()
		await expect(page.getByRole('heading', { name: /worker auto-assignment/i })).toBeVisible()
		const dialog = page.getByRole('dialog')
		await expect(dialog.getByRole('slider')).toHaveCount(1)
	})

	test('setting gas percent via API is reflected in the worker dialog', async ({ page }) => {
		const gameId = 'default'

		// Build workers so we have something to assign
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })

		// Set gas percent via API
		const assignRes = await page.request.post(
			`${baseURL}/api/workers/assign?gasPercent=40`,
			{ data: {} }
		)
		expect([200, 204]).toContain(assignRes.status())

		// Open the dialog and verify the slider value reflects the server state.
		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Adjust' }).click()
		await expect(page.getByRole('dialog')).toBeVisible()
		const slider = page.getByRole('dialog').getByRole('slider')
		await expect(slider).toHaveValue('40')
	})

	test('saving worker assignment from the dialog sends an update', async ({ page }) => {
		const gameId = 'default'

		// Build workers and reset assignment to a known state
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=5`, { data: {} })
		await page.request.post(`${baseURL}/api/workers/assign?gasPercent=20`, { data: {} })

		await page.goto(`/games/${gameId}/base`)
		await page.getByRole('button', { name: 'Adjust' }).click()
		await expect(page.getByRole('dialog')).toBeVisible()

		// Click the "Balance 50/50" preset and save.
		await page.getByRole('button', { name: /balance 50\/50/i }).click()

		const assignPromise = page.waitForResponse(
			(r) => r.url().includes('/api/workers/assign') && r.request().method() === 'POST'
		)
		await page.getByRole('button', { name: /^save$/i }).click()
		await assignPromise

		await expect(page.getByText('Something went wrong')).not.toBeVisible()

		// Verify the API reflects the change
		const workersRes = await page.request.get(`${baseURL}/api/workers`)
		expect(workersRes.ok()).toBeTruthy()
		const workers = await workersRes.json() as { gasPercent: number }
		expect(workers.gasPercent).toBe(50)
	})
})
