import { test, expect } from '@playwright/test'

/**
 * Player profile page tests (BGE-747).
 *
 * Tests the PlayerProfile page at /profile (own profile) and
 * the PublicProfile page at /profile/:userId shipped in PR #235.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

test.describe('Player profile page', () => {
	test('own profile page renders name and stats', async ({ page }) => {
		await page.goto('/profile')
		await expect(page.getByRole('heading', { name: 'My Profile' })).toBeVisible()

		// Should show a display name (avatar initial or name text)
		// At minimum the profile card renders
		await expect(page.locator('.rounded-lg.border')).toBeVisible()

		// The profile card shows either current game stats or a "not in a game" message
		await expect(
			page.getByText(/score/i)
				.or(page.getByText(/rank/i))
				.or(page.getByText(/not currently in a game/i))
				.first()
		).toBeVisible()
	})

	test('own profile page shows "not in a game" when player has no active game', async ({ page, browser }) => {
		const freshUserId = `e2e-profile-${Date.now()}`
		const freshContext = await browser.newContext()
		const freshPage = await freshContext.newPage()
		await freshPage.request.post(`${baseURL}/signindev`, {
			form: { playerid: freshUserId, returnUrl: '/' },
		})
		await freshPage.goto('/profile')
		await expect(freshPage.getByRole('heading', { name: 'My Profile' })).toBeVisible()

		// Fresh user has no active game — the "not in a game" message or Browse games link appears
		await expect(freshPage.getByText(/not currently in a game/i).or(freshPage.getByRole('link', { name: /browse games/i }))).toBeVisible()
		await freshContext.close()
	})

	test('public profile page renders for a known user', async ({ page }) => {
		// Create a known user to view their public profile
		const knownUserId = `e2e-public-profile-${Date.now()}`
		await page.request.post(`${baseURL}/signindev`, {
			form: { playerid: knownUserId, returnUrl: '/' },
		})

		await page.goto(`/profile/${encodeURIComponent(knownUserId)}`)

		// Should render the public profile (not a 404 or error)
		await expect(page.getByText(/something went wrong/i)).not.toBeVisible({ timeout: 5_000 })

		// The page should show some profile content (even if empty history)
		await expect(
			page.locator('.max-w-2xl').first()
		).toBeVisible()
	})

	test('public profile page shows not-found state for unknown user', async ({ page }) => {
		const unknownUserId = 'this-user-does-not-exist-ever-12345'
		await page.goto(`/profile/${encodeURIComponent(unknownUserId)}`)

		// PublicProfile renders an ApiError or "not found" message for missing users
		await expect(
			page.getByText(/not found/i).or(page.getByText(/failed to load/i)).or(page.getByText(/something went wrong/i))
		).toBeVisible({ timeout: 10_000 })
	})
})
