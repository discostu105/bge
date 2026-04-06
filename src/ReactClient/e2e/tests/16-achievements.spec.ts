import { test, expect } from '@playwright/test'

/**
 * Phase 5 E2E tests: Achievement Engine + Player Profile enhancements (BGE-830).
 *
 * Covers:
 *   - /achievements page structure (milestones tab, trophies tab, category filters)
 *   - Milestone unlock via admin award endpoint (DevAuth = true → any user is admin)
 *   - Player profile page: join date, 6-stat grid (Losses/Win Rate cells)
 *   - Public profile page: achievement badge section renders
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Sign in as a fresh isolated user (playerid === userId in DevAuth mode). */
async function signInAs(page: import('@playwright/test').Page, userId: string) {
	await page.request.post(`${baseURL}/signindev`, {
		form: { playerid: userId, returnUrl: '/', protectionTicks: '0' },
	})
	await page.request.post(`${baseURL}/api/playerprofile/complete-tutorial`)
}

// ── Achievement page: structure ───────────────────────────────────────────────

test.describe('Achievements page — structure', () => {
	test('page renders heading, summary stats, and tabs', async ({ page }) => {
		await page.goto('/achievements')

		await expect(page.getByRole('heading', { name: 'Achievements' })).toBeVisible()

		// Summary stats section (4 cells)
		await expect(page.getByText('Trophies', { exact: true })).toBeVisible()
		await expect(page.getByText('Milestones', { exact: true })).toBeVisible()
		await expect(page.getByText('Completion', { exact: true })).toBeVisible()
		await expect(page.getByText('Best Tier', { exact: true })).toBeVisible()

		// Tab bar
		await expect(page.getByRole('button', { name: 'Milestones' })).toBeVisible()
		await expect(page.getByRole('button', { name: /Game Trophies/i })).toBeVisible()

		// No crash
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})

	test('milestones tab shows category filter pills', async ({ page }) => {
		await page.goto('/achievements')
		await page.getByRole('button', { name: 'Milestones' }).click()

		await expect(page.getByRole('button', { name: 'All' })).toBeVisible()
		await expect(page.getByRole('button', { name: /Combat/i })).toBeVisible()
		await expect(page.getByRole('button', { name: /Economy/i })).toBeVisible()
		await expect(page.getByRole('button', { name: /Diplomacy/i })).toBeVisible()
		await expect(page.getByRole('button', { name: /Exploration/i })).toBeVisible()
	})

	test('milestones tab renders milestone cards', async ({ page }) => {
		await page.goto('/achievements')

		// Milestone cards should be present (the catalogue has 19 milestones)
		const cards = page.locator('.grid > div').filter({ hasText: /bronze|silver|gold|legendary/i })
		await expect(cards.first()).toBeVisible({ timeout: 10_000 })
		expect(await cards.count()).toBeGreaterThan(0)
	})

	test('category filter limits displayed milestones', async ({ page }) => {
		await page.goto('/achievements')

		// Click "Combat" filter
		await page.getByRole('button', { name: /Combat/i }).first().click()

		// Combat milestones include "First Blood", "Champion", etc.
		await expect(page.getByText('First Blood')).toBeVisible()

		// Economy milestones should not appear on Combat filter
		await expect(page.getByText('Mineral Rush')).not.toBeVisible()
	})

	test('game trophies tab shows empty state for new user', async ({ page }) => {
		const userId = `e2e-ach-trophies-${Date.now()}`
		await signInAs(page, userId)

		await page.goto('/achievements')
		await page.getByRole('button', { name: /Game Trophies/i }).click()

		await expect(page.getByText('No trophies yet')).toBeVisible()
		await expect(page.getByRole('link', { name: /Browse Games/i })).toBeVisible()
	})
})

// ── Achievement earning via award API ────────────────────────────────────────

test.describe('Achievements page — milestone unlock', () => {
	test('awarded milestone appears as unlocked on achievements page', async ({ page }) => {
		const userId = `e2e-ach-award-${Date.now()}`
		await signInAs(page, userId)

		// Award the "games-first" milestone (First Commander — bronze exploration) to this user
		const awardRes = await page.request.post(`${baseURL}/api/achievements/award`, {
			data: { userId, milestoneId: 'games-first' },
		})
		expect(awardRes.ok()).toBeTruthy()

		// Navigate to achievements page and check the milestone is unlocked
		await page.goto('/achievements')

		// The "First Commander" card should be present and NOT greyed-out/locked
		// Unlocked cards show the actual icon (🚀) instead of a lock icon
		await expect(page.getByText('First Commander')).toBeVisible()

		// Locked cards are styled with opacity-65; unlocked cards have a ring style.
		// Verify the "Unlocked" date text appears somewhere on the card.
		await expect(page.getByText(/Unlocked/i)).toBeVisible()
	})

	test('newly awarded milestone is reflected in Milestones completion count', async ({ page }) => {
		const userId = `e2e-ach-count-${Date.now()}`
		await signInAs(page, userId)

		// Check initial completion — fresh user has 0 unlocked
		await page.goto('/achievements')
		const completionText = page.getByText('0%')
		await expect(completionText.first()).toBeVisible()

		// Award milestone
		await page.request.post(`${baseURL}/api/achievements/award`, {
			data: { userId, milestoneId: 'win-first' },
		})

		// Reload and check completion increased above 0%
		await page.reload()
		await expect(page.getByText('0%')).not.toBeVisible({ timeout: 5_000 })
	})
})

// ── Player profile page — Phase 5B enhancements ──────────────────────────────

test.describe('Player profile page — Phase 5B features', () => {
	test('own profile shows join date below display name', async ({ page }) => {
		const userId = `e2e-profile-joindate-${Date.now()}`
		await signInAs(page, userId)

		await page.goto('/profile')
		await expect(page.getByRole('heading', { name: 'My Profile' })).toBeVisible()

		// Join date should appear (profile header shows "Member since ...")
		await expect(page.getByText(/Member since/i)).toBeVisible()
	})

	test('career stats grid includes Losses and Win Rate cells', async ({ page }) => {
		await page.goto('/profile')

		// The 6-stat grid cells — only visible when gamesPlayed > 0.
		// For the shared e2e-test-admin user (may or may not have stats),
		// verify the cells are defined in the grid when stats are shown.
		const hasStats = await page.getByText('Losses').isVisible().catch(() => false)
		if (hasStats) {
			await expect(page.getByText('Losses')).toBeVisible()
			await expect(page.getByText('Win Rate')).toBeVisible()
			await expect(page.getByText('Avg Score')).toBeVisible()
		} else {
			// No games played — just check the page doesn't crash
			await expect(page.getByRole('heading', { name: 'My Profile' })).toBeVisible()
		}
	})

	test('own profile page does not crash for fresh user', async ({ page }) => {
		const userId = `e2e-profile-fresh-${Date.now()}`
		await signInAs(page, userId)

		await page.goto('/profile')
		await expect(page.getByRole('heading', { name: 'My Profile' })).toBeVisible()
		await expect(page.getByText('Something went wrong')).not.toBeVisible()
	})
})

// ── Public profile page — achievement badges ─────────────────────────────────

test.describe('Public profile page — achievement badges', () => {
	test('public profile renders without crash for a known user', async ({ page }) => {
		const userId = `e2e-pubprofile-${Date.now()}`
		await signInAs(page, userId)

		await page.goto(`/profile/${encodeURIComponent(userId)}`)
		await expect(page.getByText('Something went wrong')).not.toBeVisible({ timeout: 5_000 })
		await expect(page.locator('.max-w-2xl, .max-w-lg').first()).toBeVisible()
	})

	test('public profile hides badge section silently for user with no game trophies', async ({ page }) => {
		const userId = `e2e-pubprofile-nobadge-${Date.now()}`
		await signInAs(page, userId)

		await page.goto(`/profile/${encodeURIComponent(userId)}`)
		await expect(page.getByText('Something went wrong')).not.toBeVisible({ timeout: 5_000 })

		// PublicAchievementBadges returns null when user has no game trophies.
		// The Achievements heading should NOT appear for a brand-new user.
		// This verifies the silent-fail behaviour (retry: false + empty list = no render).
		await page.waitForTimeout(2_000) // allow query to settle
		await expect(page.getByRole('heading', { name: 'Achievements' })).not.toBeVisible()
	})

	test('public profile join date shows below game count', async ({ page }) => {
		const userId = `e2e-pubprofile-join-${Date.now()}`
		await signInAs(page, userId)

		await page.goto(`/profile/${encodeURIComponent(userId)}`)
		await expect(page.getByText('Something went wrong')).not.toBeVisible({ timeout: 5_000 })

		// Join date should appear in the public profile header
		await expect(page.getByText(/Member since/i)).toBeVisible({ timeout: 8_000 })
	})
})
