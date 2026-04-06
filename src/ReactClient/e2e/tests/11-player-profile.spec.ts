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

		// Games played stat should be visible
		await expect(page.getByText(/games played/i)).toBeVisible()
	})

	test('own profile page shows "not in a game" when player has no active game', async ({ page }) => {
		// The shared e2e-test-admin user may or may not be in a game.
		// We create a fresh user who definitely has no active game.
		const freshUserId = `e2e-profile-${Date.now()}`
		await page.request.post(`${baseURL}/signindev`, {
			form: { playerid: freshUserId, returnUrl: '/' },
		})
		await page.goto('/profile')
		await expect(page.getByRole('heading', { name: 'My Profile' })).toBeVisible()

		// Fresh user has no active game — the "not in a game" message or Browse games link appears
		await expect(page.getByText(/not currently in a game/i).or(page.getByRole('link', { name: /browse games/i }))).toBeVisible()
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

		// The page should show some profile content
		await expect(page.locator('main, [role="main"], .max-w-lg, .max-w-2xl').first()).toBeVisible()
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
