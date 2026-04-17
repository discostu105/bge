// TODO(batch-1): extend GameDetailViewModel to include tickDurationSeconds
// so this hook can return the live value per game. Default matches
// StarcraftOnlineGameDefFactory.cs:TickDuration = 30s.
export const DEFAULT_TICK_DURATION_MS = 30_000

export function useTickDuration(): number {
  return DEFAULT_TICK_DURATION_MS
}
