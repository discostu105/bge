const GAME_PATH_PATTERN = /^\/games\/([^/]+)(?:\/|$)/

export function gameIdFromPath(pathname: string): string | null {
	const match = pathname.match(GAME_PATH_PATTERN)
	if (!match) return null
	const id = match[1]
	if (!id) return null
	return id
}
