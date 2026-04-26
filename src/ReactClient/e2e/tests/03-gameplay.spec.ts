import { test, expect } from '@playwright/test'

/**
 * Core gameplay tests: join a game, create a player, navigate in-game pages.
 *
 * Strategy: create an active game via API (faster than UI), then join it and
 * navigate the in-game pages to verify they render without errors.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

/** Create an active (already-started) game via the REST API and return its gameId and name. */
async function createActiveGame(page: import('@playwright/test').Page): Promise<{ gameId: string; name: string }> {
	const now = new Date()
	const startTime = new Date(now.getTime() - 60_000).toISOString() // started 1 min ago
	const endTime = new Date(now.getTime() + 7 * 24 * 3600_000).toISOString() // ends in 7 days

	const name = `E2E Gameplay Game ${Date.now()}`
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name,
			gameDefType: 'sco',
			startTime,
			endTime,
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	const game = await res.json()
	return { gameId: game.gameId, name }
}

test.describe('Join game and navigate in-game pages', () => {
	let gameId: string
	let gameName: string

	test.beforeAll(async ({ browser }) => {
		// Create an active game once for all tests in this describe block
		const context = await browser.newContext({
			storageState: 'e2e/.auth/state.json',
		})
		const page = await context.newPage()
		const created = await createActiveGame(page)
		gameId = created.gameId
		gameName = created.name
		await context.close()
	})

	test('join a game and land on base page', async ({ page }) => {
		// Pre-enroll the admin user in the active game so the Games page shows the
		// "Enter →" action that navigates straight to the base page.
		const joinRes = await page.request.post(`${baseURL}/api/games/${gameId}/join`, {
			data: { playerName: 'E2E Admin' },
		})
		expect(joinRes.ok()).toBeTruthy()

		await page.goto('/games')
		await expect(page.getByRole('heading', { name: 'Games', exact: true })).toBeVisible()

		// Locate our game's row in the table by its unique name.
		const gameRow = page.getByRole('row').filter({ hasText: gameName })
		await expect(gameRow).toBeVisible({ timeout: 10_000 })

		// Click "Enter →" on the active game row — enrolled players land on /base directly.
		await gameRow.getByRole('link', { name: /Enter/ }).click()

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
	test.fixme('create player page renders and form is functional', async ({ browser }) => {
		// FIXME: signindev auth cookie is not shared with page navigation in fresh
		// browser contexts — neither page.request.post, fetch(credentials:'include'),
		// nor form submission reliably sets the cookie for subsequent page.goto calls.
		// Needs investigation into Playwright cookie handling with ASP.NET cookie auth.
		// Use a fresh browser context (no admin storageState) so the new user's
		// auth cookie is the only one present — avoids cookie conflicts with the
		// shared admin session that the page fixture carries by default.
		// Pass baseURL so relative navigation (page.goto('/...')) works correctly.
		const freshUserId = `e2e-newuser-${Date.now()}`
		const context = await browser.newContext({ baseURL })
		const page = await context.newPage()

		// Sign in as a fresh user who has no player profile yet.
		// createPlayer=false skips the automatic player creation so /createplayer works as intended.
		// Use fetch() inside page.evaluate so the auth cookie is set directly on the browser
		// (page.request.post doesn't share cookies with page navigation in fresh contexts).
		await page.goto(`${baseURL}/`)
		await page.evaluate(
			({ signinUrl, userId }) => {
				return fetch(signinUrl, {
					method: 'POST',
					headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
					body: `playerid=${encodeURIComponent(userId)}&returnUrl=%2F&createPlayer=false`,
					credentials: 'include',
					redirect: 'manual',
				})
			},
			{ signinUrl: `${baseURL}/signindev`, userId: freshUserId }
		)
		// Now the auth cookie is set in the browser. Navigate to /createplayer.
		await page.goto(`${baseURL}/createplayer`)
		await page.waitForLoadState('networkidle')
		await expect(page.getByRole('heading', { name: 'Welcome to BGE' })).toBeVisible({ timeout: 15_000 })
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
