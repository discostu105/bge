import { test, expect } from '@playwright/test'

/**
 * Attack flow tests: select enemy, send troops, verify battle report.
 *
 * Architecture note: all in-game API calls (/api/units, /api/battle, etc.) hit the
 * default game world state. signindev auto-registers each new user as a player in
 * that default game. We create a game record here only for React router navigation.
 *
 * Flow:
 *   1. Admin builds WBF units.
 *   2. A second fresh player is created via signindev (enters default game world state).
 *   3. Admin navigates to SelectEnemy, sends units to fresh player's base.
 *   4. Admin opens EnemyBase page and clicks Attack — verifies battle report.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Attack Game ${Date.now()}`,
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
	return game.gameId
}

/** Create a fresh defender user and return their PlayerId (same as their userId in devauth). */
async function createFreshDefender(browser: import('@playwright/test').Browser): Promise<{ playerId: string; context: import('@playwright/test').BrowserContext }> {
	const freshUserId = `e2e-defender-${Date.now()}`
	const context = await browser.newContext()
	const page = await context.newPage()
	// signindev creates the user and immediately registers them as a player in the default game.
	// protectionTicks=0 so the defender is immediately attackable (no new-player protection).
	const res = await page.request.post(`${baseURL}/signindev`, {
		form: { playerid: freshUserId, returnUrl: '/', protectionTicks: '0' },
	})
	// A 200 redirect means the signin succeeded
	expect([200, 302]).toContain(res.status())
	await context.close()
	// In devauth the PlayerId equals the playerid form parameter
	return { playerId: freshUserId, context }
}

test.describe('Attack flow', () => {
	test('SelectEnemy page renders and shows attackable players', async ({ page, browser }) => {
		const gameId = await createNavGame(page)
		const { playerId: defenderPlayerId } = await createFreshDefender(browser)

		// Build a WBF unit for admin so the unit list is non-empty
		const buildRes = await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=1`, { data: {} })
		expect(buildRes.ok()).toBeTruthy()

		// Retrieve the unit ID
		const unitsRes = await page.request.get(`${baseURL}/api/units`)
		expect(unitsRes.ok()).toBeTruthy()
		const units = await unitsRes.json() as { units: Array<{ unitId: string }> }
		expect(units.units.length).toBeGreaterThan(0)
		const unitId = units.units[0].unitId

		// Navigate to SelectEnemy
		await page.goto(`/games/${gameId}/selectenemy/${encodeURIComponent(unitId)}`)
		await expect(page.getByRole('heading', { name: 'Select Enemy' })).toBeVisible()

		// The defender should be listed in the enemy dropdown
		const enemySelect = page.locator('select')
		await expect(enemySelect).toBeVisible()
		const options = await enemySelect.locator('option').all()
		const optionValues = await Promise.all(options.map((o) => o.getAttribute('value')))
		expect(optionValues).toContain(defenderPlayerId)
	})

	test('sends troops and navigates to EnemyBase page', async ({ page, browser }) => {
		const gameId = await createNavGame(page)
		const { playerId: defenderPlayerId } = await createFreshDefender(browser)

		// Build and retrieve unit
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=1`, { data: {} })
		const unitsRes = await page.request.get(`${baseURL}/api/units`)
		const units = await unitsRes.json() as { units: Array<{ unitId: string }> }
		const unitId = units.units[0].unitId

		// Navigate to SelectEnemy and send troops
		await page.goto(`/games/${gameId}/selectenemy/${encodeURIComponent(unitId)}`)
		await expect(page.getByRole('heading', { name: 'Select Enemy' })).toBeVisible()

		const enemySelect = page.locator('select')
		await enemySelect.selectOption({ value: defenderPlayerId })

		await page.getByRole('button', { name: 'Send Troops' }).click()

		// Should navigate to EnemyBase
		await expect(page).toHaveURL(
			new RegExp(`/games/${gameId}/enemybase/${encodeURIComponent(defenderPlayerId)}`),
			{ timeout: 10_000 }
		)
		await expect(page.getByRole('heading', { name: /attack/i })).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('attacks from EnemyBase page and shows battle report', async ({ page, browser }) => {
		const gameId = await createNavGame(page)
		const { playerId: defenderPlayerId } = await createFreshDefender(browser)

		// Build a unit and send it to the defender's base via API
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

		// Navigate to EnemyBase and click Attack
		await page.goto(`/games/${gameId}/enemybase/${encodeURIComponent(defenderPlayerId)}`)
		await expect(page.getByRole('heading', { name: /attack/i })).toBeVisible({ timeout: 10_000 })

		// Our units are en-route — the page shows a "Your Troops" table
		await expect(page.getByText(/your troops/i)).toBeVisible({ timeout: 10_000 })

		await page.getByRole('button', { name: /^attack$/i }).click()

		// Battle result heading (h2) should appear
		await expect(page.getByRole('heading', { name: /battle result/i })).toBeVisible({ timeout: 10_000 })
	})
})
