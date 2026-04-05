import { defineConfig, devices } from '@playwright/test'

/**
 * E2E test configuration for BGE React client.
 * Tests run against the local docker-compose stack (http://localhost:8080).
 *
 * Prerequisites:
 *   docker-compose up   (starts the full BGE stack with DevAuth enabled)
 *
 * Run tests:
 *   npm run test:e2e
 */
export default defineConfig({
	testDir: './e2e/tests',
	fullyParallel: false,
	forbidOnly: !!process.env.CI,
	retries: process.env.CI ? 1 : 0,
	workers: 1,
	reporter: process.env.CI ? 'github' : 'list',
	globalSetup: './e2e/global-setup.ts',
	use: {
		baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8080',
		trace: 'on-first-retry',
		storageState: 'e2e/.auth/state.json',
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] },
		},
	],
})
