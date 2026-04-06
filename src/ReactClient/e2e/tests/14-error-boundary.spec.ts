import { test, expect } from '@playwright/test'

/**
 * Error boundary (BGE-746) smoke tests.
 *
 * The ErrorBoundary component wraps the entire app in RootLayout and catches
 * any unhandled React render errors, showing a friendly fallback UI instead of
 * a blank screen.
 *
 * Strategy: load the app so all static (eagerly-imported) chunks are in the
 * browser's HTTP cache, then intercept future JS chunk requests so the next
 * lazy-loaded page chunk is aborted.  React.lazy propagates the failed import
 * as a render error, which the ErrorBoundary catches.
 *
 * Note: Sentry integration is not yet wired up in the client; only the UI
 * fallback behaviour is verified here.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createActiveGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Error Boundary Game ${Date.now()}`,
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

test.describe('Error boundary fallback UI', () => {
	test('shows "Something went wrong" heading and Refresh Page button when a lazy chunk fails to load', async ({ page }) => {
		const gameId = await createActiveGame(page)

		// Load the base page so all eagerly-imported chunks are in browser cache.
		// After networkidle, only lazy page-specific chunks still need to be fetched.
		await page.goto(`/games/${gameId}/base`)
		await page.waitForLoadState('networkidle')
		await expect(page.getByRole('heading', { name: /base/i })).toBeVisible()

		// Intercept future JS chunk requests — clicking a nav link triggers a SPA
		// navigation (no page reload), so only the target page's lazy chunk is fetched.
		// Aborting it causes React.lazy to throw, which ErrorBoundary catches.
		await page.route('**/assets/*.js', (route) => route.abort())

		// Navigate to the Help page via the sidebar nav link (SPA navigation).
		await page.getByRole('link', { name: 'Help' }).click()

		// ErrorBoundary should render its fallback.
		await expect(page.getByRole('heading', { name: 'Something went wrong' })).toBeVisible({ timeout: 10_000 })
		await expect(page.getByText('An unexpected error occurred on this page.')).toBeVisible()
		await expect(page.getByRole('button', { name: 'Try Again' })).toBeVisible()
	})

	test('normal pages do not trigger the error boundary', async ({ page }) => {
		const gameId = await createActiveGame(page)

		for (const route of ['base', 'ranking', 'research']) {
			await page.goto(`/games/${gameId}/${route}`)
			await expect(page.getByText('Something went wrong')).not.toBeVisible({ timeout: 10_000 })
		}
	})
})
