// Shared visual identity for the three primary resources.
// Minerals = blue, Gas = green, Land = brown.

export const RESOURCE_HEX: Record<string, string> = {
  minerals: '#60a5fa', // blue-400
  gas: '#4ade80',      // green-400
  land: '#b8804a',     // warm brown
  credits: '#facc15',  // yellow-400
  gold: '#facc15',
}

export const RESOURCE_TEXT_CLASS: Record<string, string> = {
  minerals: 'text-blue-400',
  gas: 'text-green-400',
  land: 'text-[#b8804a]',
  credits: 'text-yellow-400',
  gold: 'text-yellow-400',
}

export function resourceTextClass(name: string): string {
  return RESOURCE_TEXT_CLASS[name.toLowerCase()] ?? ''
}

export function resourceHex(name: string): string {
  return RESOURCE_HEX[name.toLowerCase()] ?? 'currentColor'
}
