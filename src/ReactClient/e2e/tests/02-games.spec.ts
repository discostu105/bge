import { test, expect } from '@playwright/test'

/**
 * Game management tests: listing and creating games.
 * Authenticated as e2e-test-admin (set up in global-setup.ts).
 */
test.describe('Games list', () => {
	test('games page renders with season schedule', async ({ page }) => {
		await page.goto('/games')
		await expect(page.getByRole('heading', { name: 'Games', exact: true })).toBeVisible()
		// At least the Active and Upcoming sections are present
		await expect(page.getByRole('heading', { name: /Active/ })).toBeVisible()
		await expect(page.getByRole('heading', { name: /Upcoming/ })).toBeVisible()
	})
})

test.describe('Create game', () => {
	test('admin can create a game via the admin page', async ({ page }) => {
		await page.goto('/admin/games')
		await expect(page.getByRole('heading', { name: 'Game Admin' })).toBeVisible()
		await expect(page.getByRole('heading', { name: 'Create Game' })).toBeVisible()

		const gameName = `E2E Test Game ${Date.now()}`

		// Fill in the create form
		await page.getByPlaceholder('Game name').fill(gameName)
		// Leave tick duration and times at their defaults (pre-filled by the UI)

		// Submit
		await page.getByRole('button', { name: 'Create Game' }).click()

		// Success message appears
		await expect(page.getByText(`Game '${gameName}' created.`)).toBeVisible()

		// The new game should appear in the admin games table
		await expect(page.getByRole('cell', { name: gameName })).toBeVisible()
	})
})
