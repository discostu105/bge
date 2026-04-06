import { test, expect } from '@playwright/test'

/**
 * Alliance tests: create, join + accept, invite + accept.
 *
 * Architecture note: alliance state lives in the default game world state.
 * signindev auto-registers each user as a player in the default game, so
 * every fresh user created here can immediately participate in alliances.
 *
 * Each test creates fresh players to avoid "already in alliance" interference.
 */

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080'

async function createNavGame(page: import('@playwright/test').Page): Promise<string> {
	const now = new Date()
	const res = await page.request.post(`${baseURL}/api/games`, {
		data: {
			name: `E2E Alliance Game ${Date.now()}`,
			gameDefType: 'sco',
			startTime: new Date(now.getTime() - 60_000).toISOString(),
			endTime: new Date(now.getTime() + 7 * 24 * 3600_000).toISOString(),
			tickDuration: '00:01:00',
			discordWebhookUrl: null,
		},
	})
	expect(res.ok()).toBeTruthy()
	return (await res.json()).gameId as string
}

/** Sign in a fresh user in a new browser context. Returns playerId = the signindev playerid param. */
async function signInFreshUser(
	browser: import('@playwright/test').Browser,
	tag: string
): Promise<{ page: import('@playwright/test').Page; context: import('@playwright/test').BrowserContext; playerId: string }> {
	const playerId = `e2e-all-${tag}-${Date.now()}`
	const context = await browser.newContext()
	const page = await context.newPage()
	// signindev creates the user AND the player in the default game world state
	await page.request.post(`${baseURL}/signindev`, { form: { playerid: playerId, returnUrl: '/' } })
	// Dismiss the new-player tutorial overlay so it never blocks UI interactions
	await page.request.post(`${baseURL}/api/playerprofile/complete-tutorial`)
	return { page, context, playerId }
}

// ---------------------------------------------------------------------------
// Test 1 — Create alliance via UI
// ---------------------------------------------------------------------------
test('create alliance via UI and see it in the list', async ({ browser }) => {
	const { page, context } = await signInFreshUser(browser, 'creator')
	const gameId = await createNavGame(page)
	const allianceName = `TestAlliance-${Date.now()}`

	await page.goto(`/games/${gameId}/alliances`)
	await expect(page.getByRole('heading', { name: 'Alliances' })).toBeVisible()

	// Open the create form
	await page.getByRole('button', { name: 'Create Alliance' }).click()
	await expect(page.getByText('Create New Alliance')).toBeVisible()

	await page.getByPlaceholder('Enter alliance name').fill(allianceName)
	await page.getByPlaceholder('Alliance password').fill('secret123')
	await page.getByRole('button', { name: 'Create' }).click()

	// Success confirmation
	await expect(page.getByText('Alliance created!')).toBeVisible({ timeout: 5_000 })

	// Alliance should appear in the All Alliances table (scope to table to avoid matching
	// the "Your Alliance" header link which also uses the same name after joining)
	await expect(page.locator('table').getByRole('link', { name: allianceName })).toBeVisible({ timeout: 5_000 })

	await context.close()
})

// ---------------------------------------------------------------------------
// Test 2 — Join alliance via UI; leader accepts the pending request
// ---------------------------------------------------------------------------
test('player joins alliance with password and leader accepts via UI', async ({ browser }) => {
	const { page: leaderPage, context: leaderCtx } = await signInFreshUser(browser, 'leader')
	const { page: memberPage, context: memberCtx } = await signInFreshUser(browser, 'joiner')

	// Leader creates alliance via API
	// The endpoint returns the alliance ID as plain text (not JSON-encoded), so use .text()
	const allianceName = `JoinTest-${Date.now()}`
	const password = 'joinpass'
	const createRes = await leaderPage.request.post(`${baseURL}/api/alliances`, {
		data: { allianceName, password },
	})
	expect(createRes.ok()).toBeTruthy()
	const allianceId = await createRes.text()

	const gameId = await createNavGame(leaderPage)

	// Member: navigate to alliances page and join
	await memberPage.goto(`/games/${gameId}/alliances`)
	await expect(memberPage.getByRole('heading', { name: 'Alliances' })).toBeVisible()
	await expect(memberPage.getByRole('link', { name: allianceName })).toBeVisible({ timeout: 5_000 })

	// Click the Join button in the row containing allianceName
	await memberPage.locator('tr').filter({ hasText: allianceName }).getByRole('button', { name: 'Join' }).click()

	// Enter password in the modal and submit
	await expect(memberPage.getByText('Join Alliance')).toBeVisible()
	await memberPage.getByPlaceholder('Alliance password').fill(password)
	// The modal's join button is inside the fixed overlay
	await memberPage.locator('.fixed').getByRole('button', { name: 'Join' }).click()

	await expect(memberPage.getByText('Join request sent!')).toBeVisible({ timeout: 5_000 })

	// Leader: navigate to alliance detail and accept the pending member
	await leaderPage.goto(`/alliances/${allianceId}`)
	await expect(leaderPage.getByText('Pending Members')).toBeVisible({ timeout: 5_000 })

	// Accept the first pending request (exact: true to avoid matching "Accept Peace" in war panels)
	await leaderPage.getByRole('button', { name: 'Accept', exact: true }).first().click()

	// Pending section vanishes once the request is resolved
	await expect(leaderPage.getByText('Pending Members')).not.toBeVisible({ timeout: 5_000 })

	await leaderCtx.close()
	await memberCtx.close()
})

// ---------------------------------------------------------------------------
// Test 3 — Invite player via leader controls; invitee accepts via Alliances page
// ---------------------------------------------------------------------------
test('leader invites player and invitee accepts invite via UI', async ({ browser }) => {
	const { page: leaderPage, context: leaderCtx } = await signInFreshUser(browser, 'invleader')
	const { page: inviteePage, context: inviteeCtx, playerId: inviteePlayerId } = await signInFreshUser(browser, 'invitee')

	// Leader creates alliance via API
	// The endpoint returns the alliance ID as plain text (not JSON-encoded), so use .text()
	const allianceName = `InviteTest-${Date.now()}`
	const createRes = await leaderPage.request.post(`${baseURL}/api/alliances`, {
		data: { allianceName, password: 'invpass' },
	})
	expect(createRes.ok()).toBeTruthy()
	const allianceId = await createRes.text()

	// Leader sends an invite via API (avoids dropdown complexity in UI)
	const inviteRes = await leaderPage.request.post(`${baseURL}/api/alliances/${allianceId}/invite`, {
		data: { targetPlayerId: inviteePlayerId },
	})
	expect([200, 204]).toContain(inviteRes.status())

	// Invitee: navigate to alliances page and accept the pending invite
	const gameId = await createNavGame(inviteePage)
	await inviteePage.goto(`/games/${gameId}/alliances`)
	await expect(inviteePage.getByRole('heading', { name: 'Alliances' })).toBeVisible()

	// "Pending Invites" section should appear
	await expect(inviteePage.getByText('Pending Invites')).toBeVisible({ timeout: 10_000 })
	// Scope to the pending invites section to avoid strict-mode violation from the
	// same name appearing in the All Alliances table link below.
	await expect(inviteePage.locator('span.font-medium', { hasText: allianceName })).toBeVisible()

	// Accept the invite — on the Alliances page only "Accept" (invite accept) and "Decline" are visible,
	// no "Accept Peace" buttons, so exact: true is safe here
	await inviteePage.getByRole('button', { name: 'Accept', exact: true }).first().click()

	await expect(inviteePage.getByText('Invite accepted!')).toBeVisible({ timeout: 5_000 })

	await leaderCtx.close()
	await inviteeCtx.close()
})
