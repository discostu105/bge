import { test, expect } from '@playwright/test'

/**
 * Battle report detail page tests (BGE-747).
 *
 * Tests the BattleReplay page at /games/:gameId/battles/:reportId
 * shipped in PR #232.
 *
 * Flow:
 *   1. Admin builds a unit, creates a defender, sends units and attacks.
 *   2. Fetch the resulting battle report ID from /api/battle/reports.
 *   3. Navigate to /games/:gameId/battles/:reportId and verify the page renders.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createFreshDefender(browser: import('@playwright/test').Browser): Promise<string> {
	const freshUserId = `e2e-defender-br-${Date.now()}`
	const context = await browser.newContext()
	const page = await context.newPage()
	const res = await page.request.post(`${baseURL}/signindev`, {
		form: { playerid: freshUserId, returnUrl: '/', protectionTicks: '0' },
	})
	expect([200, 302]).toContain(res.status())
	await context.close()
	return freshUserId
}

async function triggerBattle(page: import('@playwright/test').Page, defenderPlayerId: string): Promise<void> {
	await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=1`, { data: {} })
	const unitsRes = await page.request.get(`${baseURL}/api/units`)
	const units = await unitsRes.json() as { units: Array<{ unitId: string; positionPlayerId: string | null }> }
	// Pick a unit that is at home (positionPlayerId is null) — previously sent units may still be en-route
	const homeUnit = units.units.find(u => !u.positionPlayerId)
	expect(homeUnit).toBeTruthy()
	const unitId = homeUnit!.unitId

	const sendRes = await page.request.post(
		`${baseURL}/api/battle/sendunits?unitId=${encodeURIComponent(unitId)}&enemyPlayerId=${encodeURIComponent(defenderPlayerId)}`,
		{ data: {} }
	)
	expect(sendRes.ok()).toBeTruthy()
}

test.describe('Battle report detail page', () => {
	test('renders battle report with key stats after completing a battle', async ({ page, browser }) => {
		const gameId = 'default'
		const defenderPlayerId = await createFreshDefender(browser)
		await triggerBattle(page, defenderPlayerId)

		// Trigger the attack
		await page.goto(`/games/${gameId}/enemybase/${encodeURIComponent(defenderPlayerId)}`)
		await expect(page.getByRole('heading', { name: /attack/i })).toBeVisible({ timeout: 10_000 })
		await expect(page.getByText(/your troops/i)).toBeVisible({ timeout: 10_000 })
		await page.getByRole('button', { name: /^attack$/i }).click()
		await expect(page.getByRole('heading', { name: /battle result/i })).toBeVisible({ timeout: 10_000 })

		// Fetch report ID from the API
		const reportsRes = await page.request.get(`${baseURL}/api/battle/reports`)
		expect(reportsRes.ok()).toBeTruthy()
		const reports = await reportsRes.json() as Array<{ id: string }>
		expect(reports.length).toBeGreaterThan(0)
		const reportId = reports[0].id

		// Navigate to the battle replay page
		await page.goto(`/games/${gameId}/battles/${encodeURIComponent(reportId)}`)
		await expect(page.getByRole('heading', { name: 'Battle Report' })).toBeVisible()

		// Verify key sections render
		await expect(page.getByText('Initial Forces')).toBeVisible()
		await expect(page.getByText('Round-by-Round Replay')).toBeVisible()

		// Outcome badge (Victory / Defeat / Draw) should be present
		const outcomeBadge = page.locator('span').filter({ hasText: /^(Victory|Defeat|Draw)$/ })
		await expect(outcomeBadge).toBeVisible()
	})

	test('shows error state for an unknown report ID', async ({ page }) => {
		const gameId = 'default'
		const fakeReportId = '00000000-0000-0000-0000-000000000000'

		await page.goto(`/games/${gameId}/battles/${fakeReportId}`)

		// The ApiError component renders when the API returns 404
		await expect(page.getByText(/battle report not found/i)).toBeVisible({ timeout: 10_000 })
	})
})
