import { test, expect, request } from '@playwright/test'

/**
 * Sign-in flow tests.
 *
 * The production build does not include the dev-login UI form
 * (VITE_DEV_AUTH is not set at build time), so these tests verify:
 *   1. The sign-in page renders correctly (GitHub OAuth button visible).
 *   2. Dev auth via POST /signindev creates a session and redirects to the app.
 */
test.describe('Sign-in page', () => {
	test.use({ storageState: { cookies: [], origins: [] } }) // run unauthenticated

	test('sign-in page renders with GitHub login button', async ({ page }) => {
		await page.goto('/signin')
		await expect(page.getByRole('heading', { name: 'Age of Agents' })).toBeVisible()
		await expect(page.getByText('Sign in to continue')).toBeVisible()
		await expect(page.getByRole('button', { name: /sign in with github/i })).toBeVisible()
	})

	test('unauthenticated access to protected route redirects to sign-in', async ({ page }) => {
		await page.goto('/games')
		// The app redirects unauthenticated users to /signin
		await expect(page).toHaveURL(/\/signin/)
	})

	test('dev auth POST authenticates and redirects to home', async ({ page, baseURL }) => {
		const uniqueId = `e2e-signin-${Date.now()}`
		const base = baseURL ?? 'http://localhost:8080'

		// POST to /signindev — same form action the React SignIn component uses in dev builds
		await page.request.post(`${base}/signindev`, {
			form: { playerid: uniqueId, returnUrl: '/' },
		})

		// Navigate — should now land on the app (not redirect to /signin)
		await page.goto('/')
		await expect(page).not.toHaveURL(/\/signin/)
	})
})
