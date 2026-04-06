import { test, expect } from '@playwright/test'

/**
 * Core gameplay tests: join a game, create a player, navigate in-game pages.
 *
 * Strategy: create an active game via API (faster than UI), then join it and
 * navigate the in-game pages to verify they render without errors.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

/** Create an active (already-started) game via the REST API and return its gameId. */
async function createActiveGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const startTime = new Date(now.getTime() - 60_000).toISOString() // started 1 min ago
	const endTime = new Date(now.getTime() + 7 * 24 * 3600_000).toISOString() // ends in 7 days

	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Gameplay Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime,
			endTime,
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	return game.gameId
}

test.describe('Join game and navigate in-game pages', () => {
	let gameId: string

	test.beforeAll(async ({ browser }) => {
		// Create an active game once for all tests in this describe block
		const context = await browser.newContext({
			storageState: 'e2e/.auth/state.json',
		})
		const page = await context.newPage()
		gameId = await createActiveGame(page)
		await context.close()
	})

	test('join a game and land on base page', async ({ page }) => {
		await page.goto('/games')
		await expect(page.getByRole('heading', { name: 'Season Schedule' })).toBeVisible()

		// The game we created should be listed as Active
		await page.waitForFunction(() => {
			return document.querySelector('body')?.textContent?.includes('LIVE')
		}, { timeout: 10_000 })

		// Click "Play Now" or "Join" for our game
		// Prefer "Play Now" if already enrolled, otherwise "Join"
		const gameCard = page.locator('.rounded-lg').filter({ hasText: 'LIVE' }).first()
		const playNow = gameCard.getByRole('link', { name: 'Play Now' })
		const joinBtn = gameCard.getByRole('button', { name: 'Join' })

		if (await playNow.isVisible()) {
			await playNow.click()
		} else {
			await joinBtn.click()
		}

		// Should land on base page inside the game
		await expect(page).toHaveURL(/\/games\/[^/]+\/base/, { timeout: 10_000 })
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()
	})

	test('base page renders without errors', async ({ page }) => {
		await page.goto(`/games/${gameId}/base`)
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()
		// No error boundary / crash message
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('units page renders without errors', async ({ page }) => {
		await page.goto(`/games/${gameId}/units`)
		await expect(page.locator('h1')).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('research page renders without errors', async ({ page }) => {
		await page.goto(`/games/${gameId}/research`)
		await expect(page.locator('h1')).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('market page renders without errors', async ({ page }) => {
		await page.goto(`/games/${gameId}/market`)
		await expect(page.locator('h1')).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})

test.describe('Create player', () => {
	test('create player page renders and form is functional', async ({ browser }) => {
		// Use a fresh browser context (no admin storageState) so the new user's
		// auth cookie is the only one present — avoids cookie conflicts with the
		// shared admin session that the page fixture carries by default.
		const freshUserId = `e2e-newuser-${Date.now()}`
		const context = await browser.newContext()
		const page = await context.newPage()

		// Sign in as a fresh user who has no player profile yet.
		// createPlayer=false skips the automatic player creation so /createplayer works as intended.
		await page.request.post(`${baseURL}/signindev`, {
			form: { playerid: freshUserId, returnUrl: '/', createPlayer: 'false' },
		})

		await page.goto('/createplayer')
		await page.waitForLoadState('networkidle')
		await expect(page.getByRole('heading', { name: 'Welcome to BGE' })).toBeVisible({ timeout: 10_000 })
		await expect(page.getByLabel('Commander name')).toBeVisible({ timeout: 10_000 })

		// Fill and submit
		await page.getByLabel('Commander name').fill(`Commander ${freshUserId.slice(-8)}`)
		await page.getByRole('button', { name: 'Enter the game' }).click()

		// After creating player, redirected to games with welcome message
		await expect(page).toHaveURL(/\/games\?welcome=1/, { timeout: 10_000 })
		await expect(page.getByText('Welcome to BGE!')).toBeVisible()

		await context.close()
	})
})
