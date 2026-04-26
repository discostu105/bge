const GAME_PATH_PATTERN = /^\/games\/([^/]+)(?:\/|$)/
const ADMIN_GAME_PATH_PATTERN = /^\/admin\/(?:players|ticks|stats|metrics)\/([^/]+)(?:\/|$)/

export function gameIdFromPath(pathname: string): string | null {
	const match = pathname.match(GAME_PATH_PATTERN) ?? pathname.match(ADMIN_GAME_PATH_PATTERN)
	if (!match) return null
	const id = match[1]
	if (!id) return null
	return id
}
