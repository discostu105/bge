import { test, expect } from '@playwright/test'

/**
 * Chat tests: send a message in game chat and verify it appears.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createAndJoinGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Chat Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()

	// Join the game
	const joinRes = await page.request.post(`${baseURL}/api/games/${game.gameId}/players`, {
		data: {},
	})
	// 200 or 409 (already enrolled) are both fine
	expect([200, 201, 204, 409]).toContain(joinRes.status())

	return game.gameId
}

test.describe('Game chat', () => {
	test('send a message and verify it appears in the chat window', async ({ page }) => {
		const gameId = await createAndJoinGame(page)

		await page.goto(`/games/${gameId}/chat`)
		await expect(page.getByRole('heading', { name: 'Game Chat' })).toBeVisible()

		const message = `Hello E2E test ${Date.now()}`

		// Type the message
		await page.getByPlaceholder('Type a message…').fill(message)

		// Send via button click
		await page.getByRole('button', { name: 'Send' }).click()

		// Message should appear in the chat window
		await expect(page.getByText(message)).toBeVisible({ timeout: 10_000 })
	})

	test('send a message via Enter key and verify it appears', async ({ page }) => {
		const gameId = await createAndJoinGame(page)

		await page.goto(`/games/${gameId}/chat`)
		await expect(page.getByRole('heading', { name: 'Game Chat' })).toBeVisible()

		const message = `Enter key test ${Date.now()}`

		await page.getByPlaceholder('Type a message…').fill(message)
		await page.getByPlaceholder('Type a message…').press('Enter')

		await expect(page.getByText(message)).toBeVisible({ timeout: 10_000 })
	})
})
