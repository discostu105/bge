import { test, expect } from '@playwright/test'

/**
 * Notification bell E2E tests (BGE-850).
 *
 * Tests the NotificationBell component rendered inside GameLayout at
 * /games/:gameId/*. Verifies that:
 *   - The bell renders with the correct aria-label.
 *   - The unread badge appears when notifications exist and dismisses on "Clear all".
 *   - The popover shows "All caught up!" when there are no notifications.
 *
 * The badge/dismiss flow is tested by intercepting GET /api/notifications/recent
 * to inject a fake notification, so the test is independent of the sendunits/attack
 * API (which has pre-existing CI flakiness unrelated to this PR).
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

test.describe('Notification bell', () => {
	test('bell renders in game layout with correct aria-label', async ({ page }) => {
		const gameId = await createActiveGame(page)
		await page.goto(`/games/${gameId}/base`)

		// GameLayout mounts the NotificationBell; it has a labelled button
		const bell = page.getByRole('button', { name: /notifications/i })
		await expect(bell).toBeVisible({ timeout: 10_000 })
	})

	test('unread badge appears when notifications exist and clears after "Clear all"', async ({ page }) => {
		const gameId = await createActiveGame(page)

		// Intercept GET /api/notifications/recent to return a fake notification,
		// avoiding the flaky sendunits/attack API dependency.
		await page.route('**/api/notifications/recent', async (route, request) => {
			if (request.method() === 'GET') {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: JSON.stringify([
						{
							id: 'test-notif-1',
							message: 'You have been attacked!',
							kind: 'GameEvent',
							createdAt: new Date().toISOString(),
							isRead: false,
						},
					]),
				})
			} else {
				// DELETE and other methods pass through to the real endpoint
				await route.continue()
			}
		})

		await page.goto(`/games/${gameId}/base`)

		// The bell should show an unread badge (aria-label includes "unread")
		const bell = page.getByRole('button', { name: /notifications.*unread/i })
		await expect(bell).toBeVisible({ timeout: 10_000 })

		// Open the notification popover
		await bell.click()

		// "Clear all" button appears when there are unread notifications
		const clearAll = page.getByRole('button', { name: /clear all/i })
		await expect(clearAll).toBeVisible()

		// Remove the route intercept so subsequent GET calls return the real (empty) response
		await page.unroute('**/api/notifications/recent')

		await clearAll.click()

		// After clearing, dismissAll sets the local cache to [] immediately —
		// the bell label should revert to plain "Notifications"
		await expect(page.getByRole('button', { name: 'Notifications' })).toBeVisible({ timeout: 5_000 })
		await expect(page.getByRole('button', { name: /notifications.*unread/i })).not.toBeVisible()
	})

	test('bell opens popover and shows "All caught up!" when no notifications', async ({ page }) => {
		const gameId = await createActiveGame(page)

		// Clear all notifications so we start from a clean state
		await page.request.delete(`${baseURL}/api/notifications/recent`)

		await page.goto(`/games/${gameId}/base`)

		const bell = page.getByRole('button', { name: 'Notifications' })
		await expect(bell).toBeVisible({ timeout: 10_000 })
		await bell.click()

		// EmptyState in the popover when there are no notifications
		await expect(page.getByText('All caught up!')).toBeVisible({ timeout: 5_000 })
	})
})
