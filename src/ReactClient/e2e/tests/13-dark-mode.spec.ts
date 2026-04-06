import { test, expect } from '@playwright/test'

/**
 * Dark mode toggle tests (BGE-747).
 *
 * Tests the theme toggle shipped in PR #236.
 * The toggle lives in the GameLayout header and uses ThemeContext,
 * which persists the preference in localStorage under key "bge-theme".
 *
 * CSS: the <html> element gets the class "light" when light mode is active;
 * dark mode is the default (no class added).
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Dark Mode Game ${Date.now()}`,
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

test.describe('Dark mode toggle', () => {
	test('toggle button is present and switches between dark and light mode', async ({ page }) => {
		const gameId = await createNavGame(page)
		await page.goto(`/games/${gameId}/base`)

		const toggleBtn = page.getByRole('button', { name: /switch to (light|dark) mode/i })
		await expect(toggleBtn).toBeVisible()

		// Default is dark — html should NOT have class "light"
		const htmlEl = page.locator('html')
		const initialClasses = await htmlEl.getAttribute('class')
		const startsInDark = !initialClasses?.includes('light')

		if (startsInDark) {
			// Toggle to light
			await toggleBtn.click()
			await expect(htmlEl).toHaveClass(/light/)
			await expect(page.getByRole('button', { name: /switch to dark mode/i })).toBeVisible()

			// Toggle back to dark
			await toggleBtn.click()
			const classes = await htmlEl.getAttribute('class')
			expect(classes ?? '').not.toContain('light')
			await expect(page.getByRole('button', { name: /switch to light mode/i })).toBeVisible()
		} else {
			// Page loaded in light mode (persisted pref)
			await toggleBtn.click()
			const classes = await htmlEl.getAttribute('class')
			expect(classes ?? '').not.toContain('light')
		}
	})

	test('dark mode preference persists across page reload', async ({ page }) => {
		const gameId = await createNavGame(page)
		await page.goto(`/games/${gameId}/base`)

		const htmlEl = page.locator('html')

		// Ensure we start in dark mode by clearing any stored preference
		await page.evaluate(() => localStorage.setItem('bge-theme', 'dark'))
		await page.reload()

		const toggleBtn = page.getByRole('button', { name: /switch to light mode/i })
		await expect(toggleBtn).toBeVisible()

		// Toggle to light mode
		await toggleBtn.click()
		await expect(htmlEl).toHaveClass(/light/)

		// Verify localStorage updated
		const storedTheme = await page.evaluate(() => localStorage.getItem('bge-theme'))
		expect(storedTheme).toBe('light')

		// Reload — should still be in light mode
		await page.reload()
		await expect(htmlEl).toHaveClass(/light/)
		await expect(page.getByRole('button', { name: /switch to dark mode/i })).toBeVisible()
	})

	test('switching to dark mode removes light class from html', async ({ page }) => {
		const gameId = await createNavGame(page)

		// Navigate first, then set theme preference via localStorage
		await page.goto(`/games/${gameId}/base`)
		await page.evaluate(() => localStorage.setItem('bge-theme', 'light'))
		await page.reload()

		const htmlEl = page.locator('html')
		await expect(htmlEl).toHaveClass(/light/)

		// Toggle to dark
		const toggleBtn = page.getByRole('button', { name: /switch to dark mode/i })
		await expect(toggleBtn).toBeVisible()
		await toggleBtn.click()

		const classes = await htmlEl.getAttribute('class')
		expect(classes ?? '').not.toContain('light')

		// Verify localStorage
		const storedTheme = await page.evaluate(() => localStorage.getItem('bge-theme'))
		expect(storedTheme).toBe('dark')
	})
})
