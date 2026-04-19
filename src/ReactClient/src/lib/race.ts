export const RACES = ['terran', 'zerg', 'protoss'] as const
export type Race = (typeof RACES)[number]

export function normalizeRace(id: string | null | undefined): Race | null {
  if (!id) return null
  const lower = id.toLowerCase()
  return (RACES as readonly string[]).includes(lower) ? (lower as Race) : null
}
