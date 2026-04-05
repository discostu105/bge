import { chromium } from '@playwright/test'
import * as fs from 'fs'
import * as path from 'path'

/**
 * Global setup: authenticates as the shared E2E test user via dev auth
 * (POST /signindev) and saves cookie state so tests skip the login flow.
 *
 * Requires: docker-compose up (Bge__DevAuth=true).
 */
export default async function globalSetup() {
	const authDir = path.join(__dirname, '.auth')
	if (!fs.existsSync(authDir)) {
		fs.mkdirSync(authDir, { recursive: true })
	}

	const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'
	const browser = await chromium.launch()
	const context = await browser.newContext()
	const page = await context.newPage()

	// Use dev auth to sign in — no UI form required; POST directly to the endpoint.
	// Bge__DevAuth=true is set in docker-compose so this endpoint is active.
	await page.request.post(`${baseURL}/signindev`, {
		form: { playerid: 'e2e-test-admin', returnUrl: '/' },
	})

	// Save auth cookies so all tests share the session
	await context.storageState({ path: path.join(authDir, 'state.json') })
	await browser.close()
}
