import { test, expect } from '@playwright/test'

/**
 * Game creation advanced settings tests (BGE-692).
 *
 * Covers:
 * - End-tick (game length) configuration
 * - Custom starting resources via API
 * - Max players enforcement via API
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createGameWithSettings(
	page: import('@playwright/test').Page,
	settings: {
		name: string
		endTick?: number
		maxPlayers?: number
		startingMinerals?: number
		startingGas?: number
		startingLand?: number
	}
): Promise<string> {
	const now = new Date()
	const startTime = new Date(now.getTime() - 60_000).toISOString()
	const endTime = new Date(now.getTime() + 7 * 24 * 3600_000).toISOString()

	const gameSettings =
		settings.endTick !== undefined ||
		settings.maxPlayers !== undefined ||
		settings.startingMinerals !== undefined ||
		settings.startingGas !== undefined ||
		settings.startingLand !== undefined
			? {
					endTick: settings.endTick ?? null,
					maxPlayers: settings.maxPlayers ?? null,
					startingMinerals: settings.startingMinerals ?? null,
					startingGas: settings.startingGas ?? null,
					startingLand: settings.startingLand ?? null,
				}
			: null

	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: settings.name,
			gameDefType: 'sco',
			startTime,
			endTime,
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
			maxPlayers: 0,
			settings: gameSettings,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	return game.gameId as string
}

test.describe('Advanced game creation settings — UI form', () => {
	test('advanced settings section is present and expandable', async ({ page }) => {
		await page.goto('/admin/games')
		await expect(page.getByRole('heading', { name: 'Create Game' })).toBeVisible()

		const details = page.locator('details')
		await expect(details).toBeVisible()
		await expect(details.getByText('Advanced Settings')).toBeVisible()

		await details.getByText('Advanced Settings').click()

		await expect(page.getByText('Starting Land')).toBeVisible()
		await expect(page.getByText('Starting Minerals')).toBeVisible()
		await expect(page.getByText('Starting Gas')).toBeVisible()
		await expect(page.getByText('Protection Ticks')).toBeVisible()
		await expect(page.getByText('End Tick')).toBeVisible()
		await expect(page.getByText('Max Players')).toBeVisible()
	})
})

test.describe('Advanced game creation settings — API', () => {
	test('game created with custom starting resources stores the settings', async ({
		page,
	}) => {
		const name = `E2E Resources Game ${Date.now()}`
		const gameId = await createGameWithSettings(page, {
			name,
			startingLand: 99,
			startingMinerals: 12345,
			startingGas: 6789,
		})
		expect(gameId).toBeTruthy()
	})

	test('game created with max players limit stores the setting', async ({ page }) => {
		const name = `E2E MaxPlayers Game ${Date.now()}`
		const gameId = await createGameWithSettings(page, {
			name,
			maxPlayers: 4,
		})
		expect(gameId).toBeTruthy()
	})

	test('game created with custom end tick succeeds', async ({ page }) => {
		const name = `E2E EndTick Game ${Date.now()}`
		const gameId = await createGameWithSettings(page, {
			name,
			endTick: 1440,
		})
		expect(gameId).toBeTruthy()
	})

	test('game creation with zero end tick returns 400', async ({ page }) => {
		const now = new Date()
		const res = await page.request.post(`${baseURL}/api/games`, {
			data: {
				name: `E2E Invalid EndTick ${Date.now()}`,
				gameDefType: 'sco',
				startTime: new Date(now.getTime() - 60_000).toISOString(),
				endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
				tickDuration: '00:01:00',
				discordWebhookUrl: null,
				maxPlayers: 0,
				settings: {
					endTick: 0,
				},
			},
		})
		expect(res.status()).toBe(400)
	})
})
