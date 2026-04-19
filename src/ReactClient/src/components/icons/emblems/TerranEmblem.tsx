import type { SVGProps } from 'react'

export function TerranEmblem({ width = 32, height = 32, ...rest }: SVGProps<SVGSVGElement>) {
  return (
    <svg
      role="img"
      width={width}
      height={height}
      viewBox="0 0 32 32"
      fill="none"
      {...rest}
    >
      <path d="M6 28 L6 10 L16 4 L26 10 L26 28 Z" stroke="currentColor" strokeWidth="1.5" fill="var(--color-race-emblem-fill)" />
      <path d="M6 18 L26 18" stroke="var(--color-race-secondary)" strokeWidth="1.2" />
      <path d="M10 28 L10 22 L22 22 L22 28" stroke="currentColor" strokeWidth="1" />
    </svg>
  )
}
