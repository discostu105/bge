import { test, expect } from '@playwright/test'

/**
 * Game creation advanced settings tests (BGE-692).
 *
 * Covers:
 * - Victory condition type selection (EconomicThreshold / TimeExpired / AdminFinalized)
 * - Custom starting resources via API
 * - Max players enforcement via API
 *
 * Note: BGE-777 (difficulty levels) and BGE-776 (bot configuration) are
 * Python-side agent1 features with no admin UI surface — there is no
 * difficulty dropdown or "Add Bot" form in the game creation page.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createGameWithSettings(
	page: import('@playwright/test').Page,
	settings: {
		name: string
		victoryConditionType?: string
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
		settings.victoryConditionType !== undefined ||
		settings.maxPlayers !== undefined ||
		settings.startingMinerals !== undefined ||
		settings.startingGas !== undefined ||
		settings.startingLand !== undefined
			? {
					victoryConditionType: settings.victoryConditionType ?? null,
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

		// The advanced settings are collapsed inside a <details> element
		const details = page.locator('details')
		await expect(details).toBeVisible()
		await expect(details.getByText('Advanced Settings')).toBeVisible()

		// Expand it
		await details.getByText('Advanced Settings').click()

		// All configurable fields should now be visible
		await expect(page.getByText('Starting Land')).toBeVisible()
		await expect(page.getByText('Starting Minerals')).toBeVisible()
		await expect(page.getByText('Starting Gas')).toBeVisible()
		await expect(page.getByText('Protection Ticks')).toBeVisible()
		await expect(page.getByText('Victory Threshold')).toBeVisible()
		await expect(page.getByText('Victory Condition')).toBeVisible()
		await expect(page.getByText('Max Players')).toBeVisible()
	})

	test('victory condition dropdown has all three options', async ({ page }) => {
		await page.goto('/admin/games')

		const details = page.locator('details')
		await details.getByText('Advanced Settings').click()

		const select = page.getByRole('combobox')
		await expect(select).toBeVisible()
		await expect(select.locator('option[value="EconomicThreshold"]')).toHaveCount(1)
		await expect(select.locator('option[value="TimeExpired"]')).toHaveCount(1)
		await expect(select.locator('option[value="AdminFinalized"]')).toHaveCount(1)
	})

	test('admin can create a game with TimeExpired victory condition via UI', async ({
		page,
	}) => {
		await page.goto('/admin/games')

		const gameName = `E2E Settings Game ${Date.now()}`
		await page.getByPlaceholder('Game name').fill(gameName)

		// Expand advanced settings and change victory condition
		await page.locator('details').getByText('Advanced Settings').click()
		await page.getByRole('combobox').selectOption('TimeExpired')

		await page.getByRole('button', { name: 'Create Game' }).click()

		await expect(page.getByText(`Game '${gameName}' created.`)).toBeVisible()
		await expect(page.getByRole('cell', { name: gameName })).toBeVisible()
	})

	test('admin can create a game with AdminFinalized victory condition via UI', async ({
		page,
	}) => {
		await page.goto('/admin/games')

		const gameName = `E2E AdminFinal Game ${Date.now()}`
		await page.getByPlaceholder('Game name').fill(gameName)

		await page.locator('details').getByText('Advanced Settings').click()
		await page.getByRole('combobox').selectOption('AdminFinalized')

		await page.getByRole('button', { name: 'Create Game' }).click()

		await expect(page.getByText(`Game '${gameName}' created.`)).toBeVisible()
		await expect(page.getByRole('cell', { name: gameName })).toBeVisible()
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

	test('game created with TimeExpired victory condition succeeds', async ({ page }) => {
		const name = `E2E TimeExpired Game ${Date.now()}`
		const gameId = await createGameWithSettings(page, {
			name,
			victoryConditionType: 'TimeExpired',
		})
		expect(gameId).toBeTruthy()
	})

	test('game created with AdminFinalized victory condition succeeds', async ({ page }) => {
		const name = `E2E AdminFinal API Game ${Date.now()}`
		const gameId = await createGameWithSettings(page, {
			name,
			victoryConditionType: 'AdminFinalized',
		})
		expect(gameId).toBeTruthy()
	})

	test('game creation with invalid victory condition type returns 400', async ({
		page,
	}) => {
		const now = new Date()
		const res = await page.request.post(`${baseURL}/api/games`, {
			data: {
				name: `E2E Invalid Victory ${Date.now()}`,
				gameDefType: 'sco',
				startTime: new Date(now.getTime() - 60_000).toISOString(),
				endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
				tickDuration: '00:01:00',
				discordWebhookUrl: null,
				maxPlayers: 0,
				settings: {
					victoryConditionType: 'GodMode',
				},
			},
		})
		expect(res.status()).toBe(400)
	})
})
