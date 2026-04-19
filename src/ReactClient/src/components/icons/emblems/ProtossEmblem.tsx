import type { SVGProps } from 'react'

export function ProtossEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg role="img" width={width} height={height} viewBox="0 0 32 32" fill="none" {...rest}>
      <path d="M16 2 L26 10 L26 22 L16 30 L6 22 L6 10 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <circle cx="16" cy="16" r="4" fill="currentColor" opacity="0.5" />
      <circle cx="16" cy="16" r="2" fill="currentColor" />
    </svg>
  )
}
