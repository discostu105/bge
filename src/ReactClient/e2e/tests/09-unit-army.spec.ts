import { test, expect } from '@playwright/test'

/**
 * Unit build → army tests.
 *
 * Verifies that building a unit via the API causes it to appear on the Units
 * page. The in-game state lives in the default game world; we create a game
 * record only for React router navigation.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

test.describe('Unit build — army visibility', () => {
	test('units page renders heading and shows unit count after building', async ({ page }) => {
		const gameId = 'default'

		// Build WBF units via API
		const buildRes = await page.request.post(
			`${baseURL}/api/units/build?unitDefId=wbf&count=3`,
			{ data: {} }
		)
		expect(buildRes.ok()).toBeTruthy()

		await page.goto(`/games/${gameId}/units`)
		await expect(page.getByRole('heading', { name: 'Units', exact: true })).toBeVisible()

		// The units page should show the "Total Units" stat (summary section only renders
		// when units.length > 0) and at least one DataTable row for the built unit type.
		await expect(page.getByText('Total Units')).toBeVisible({ timeout: 10_000 })

		// Confirm the built unit appears in the Home Base DataTable.
		// The unit definition name for 'wbf' (display name "SCV") is shown in the Name column.
		const scvRow = page.getByRole('row').filter({ hasText: /scv/i })
		await expect(scvRow.first()).toBeVisible({ timeout: 10_000 })

		// Read the count from the value span adjacent to the "Total Units" label.
		// Stat renders: <span class="label">Total Units</span><span class="mono ...">N</span>
		const totalValueSpan = page.locator('span.label', { hasText: 'Total Units' })
			.locator('..') // parent div.flex-col
			.locator('span.mono')
		const totalText = await totalValueSpan.textContent()
		const total = parseInt((totalText ?? '0').replace(/,/g, ''), 10)
		expect(total).toBeGreaterThanOrEqual(3)
	})

	test('units page renders without errors when army is empty', async ({ page }) => {
		// Fresh user — no units in army yet
		const freshUserId = `e2e-army-${Date.now()}`
		await page.request.post(`${baseURL}/signindev`, {
			form: { playerid: freshUserId, returnUrl: '/' },
		})

		const gameId = 'default'

		await page.goto(`/games/${gameId}/units`)
		await expect(page.getByRole('heading', { name: 'Units', exact: true })).toBeVisible()

		// EmptyState renders when the player has no units at all
		await expect(page.getByText('No units yet')).toBeVisible({ timeout: 10_000 })
		await expect(
			page.getByText('Train your first units to start building an army.')
		).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})
