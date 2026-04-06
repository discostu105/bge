import { test, expect } from '@playwright/test'

/**
 * Notification bell E2E tests (BGE-850).
 *
 * Tests the NotificationBell component rendered inside GameLayout at
 * /games/:gameId/*. Verifies that:
 *   - The bell renders with the correct aria-label.
 *   - The unread badge appears after an attack generates a battle-result
 *     notification for the attacker via BattleReportGenerator.
 *   - Clicking "Clear all" inside the popover removes the badge.
 *
 * The MilestoneUnlocked notification (BGE-850) is pushed via SignalR at game
 * end; that path is covered by unit tests. Here we test the badge/dismiss UI
 * with a battle notification, which is synchronously available via
 * GET /api/notifications/recent immediately after the attack API call.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createActiveGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Notifications Game ${Date.now()}`,
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

/** Create a fresh defender player that can be attacked immediately (protectionTicks=0). */
async function createDefender(browser: import('@playwright/test').Browser): Promise<string> {
	const playerId = `e2e-notif-defender-${Date.now()}`
	const ctx = await browser.newContext()
	const p = await ctx.newPage()
	await p.request.post(`${baseURL}/signindev`, {
		form: { playerid: playerId, returnUrl: '/', protectionTicks: '0' },
	})
	await ctx.close()
	return playerId
}

test.describe('Notification bell', () => {
	test('bell renders in game layout with correct aria-label', async ({ page }) => {
		const gameId = await createActiveGame(page)
		await page.goto(`/games/${gameId}/resources`)

		// GameLayout mounts the NotificationBell; it has a labelled button
		const bell = page.getByRole('button', { name: /notifications/i })
		await expect(bell).toBeVisible({ timeout: 10_000 })
	})

	test('unread badge appears after attack and clears after "Clear all"', async ({ page, browser }) => {
		const gameId = await createActiveGame(page)
		const defenderPlayerId = await createDefender(browser)

		// Clear any pre-existing notifications before the attack
		await page.request.delete(`${baseURL}/api/notifications/recent`)

		// Build a unit and send it to the defender's base, then attack
		await page.request.post(`${baseURL}/api/units/build?unitDefId=wbf&count=1`, { data: {} })
		const unitsRes = await page.request.get(`${baseURL}/api/units`)
		expect(unitsRes.ok()).toBeTruthy()
		const units = await unitsRes.json() as { units: Array<{ unitId: string }> }
		expect(units.units.length).toBeGreaterThan(0)
		const unitId = units.units[0].unitId

		const sendRes = await page.request.post(
			`${baseURL}/api/battle/sendunits?unitId=${encodeURIComponent(unitId)}&enemyPlayerId=${encodeURIComponent(defenderPlayerId)}`,
			{ data: {} }
		)
		expect(sendRes.ok()).toBeTruthy()

		const attackRes = await page.request.post(
			`${baseURL}/api/battle/attack?enemyPlayerId=${encodeURIComponent(defenderPlayerId)}`,
			{ data: {} }
		)
		expect(attackRes.ok()).toBeTruthy()

		// Navigate into a game page so GameLayout (and the bell) is mounted
		await page.goto(`/games/${gameId}/resources`)

		// The bell should now show an unread badge (aria-label includes "unread")
		const bell = page.getByRole('button', { name: /notifications.*unread/i })
		await expect(bell).toBeVisible({ timeout: 10_000 })

		// Open the notification popover
		await bell.click()

		// "Clear all" button appears when there are unread notifications
		const clearAll = page.getByRole('button', { name: /clear all/i })
		await expect(clearAll).toBeVisible()
		await clearAll.click()

		// After clearing, the bell aria-label should no longer include "unread"
		await expect(page.getByRole('button', { name: 'Notifications' })).toBeVisible({ timeout: 5_000 })
		await expect(page.getByRole('button', { name: /notifications.*unread/i })).not.toBeVisible()
	})

	test('bell opens popover and shows "All caught up!" when no notifications', async ({ page }) => {
		const gameId = await createActiveGame(page)

		// Clear all notifications so we start from a clean state
		await page.request.delete(`${baseURL}/api/notifications/recent`)

		await page.goto(`/games/${gameId}/resources`)

		const bell = page.getByRole('button', { name: 'Notifications' })
		await expect(bell).toBeVisible({ timeout: 10_000 })
		await bell.click()

		// EmptyState in the popover when there are no notifications
		await expect(page.getByText('All caught up!')).toBeVisible({ timeout: 5_000 })
	})
})
