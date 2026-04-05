export function vcEmoji(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return '💰'
    case 'TimeExpired': return '⏰'
    case 'AdminFinalized': return '🛑'
    default: return '🏁'
  }
}

export function vcBadgeCss(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return 'bg-yellow-500 text-black'
    case 'TimeExpired': return 'bg-gray-500 text-white'
    case 'AdminFinalized': return 'bg-red-700 text-white'
    default: return 'bg-gray-500 text-white'
  }
}

export function vcText(type: string | null | undefined): string {
  switch (type) {
    case 'EconomicThreshold': return 'CONQUERED'
    case 'TimeExpired': return 'TIME EXPIRED'
    case 'AdminFinalized': return 'ADMIN ENDED'
    default: return 'FINISHED'
  }
}
