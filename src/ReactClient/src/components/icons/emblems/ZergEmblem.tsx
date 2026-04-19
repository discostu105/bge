import type { SVGProps } from 'react'

export function ZergEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg role="img" width={width} height={height} viewBox="0 0 32 32" fill="none" {...rest}>
      <path d="M16 3 C8 5 5 12 7 20 C9 26 14 28 16 28 C18 28 23 26 25 20 C27 12 24 5 16 3 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <path d="M11 12 Q14 14 13 18 M21 12 Q18 14 19 18" stroke="var(--color-race-secondary)" strokeWidth="0.8" fill="none" />
    </svg>
  )
}
