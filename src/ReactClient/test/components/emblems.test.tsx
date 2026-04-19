import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import { TerranEmblem, ZergEmblem, ProtossEmblem } from '@/components/icons/emblems'

describe('race emblems', () => {
  it('Terran emblem renders an svg with role=img', () => {
    const { getByRole } = render(<TerranEmblem aria-label="Terran" />)
    expect(getByRole('img')).toBeDefined()
  })

  it('Zerg emblem accepts custom size via width/height', () => {
    const { container } = render(<ZergEmblem width={48} height={48} />)
    const svg = container.querySelector('svg')!
    expect(svg.getAttribute('width')).toBe('48')
    expect(svg.getAttribute('height')).toBe('48')
  })

  it('Protoss emblem uses currentColor for stroke', () => {
    const { container } = render(<ProtossEmblem />)
    expect(container.innerHTML).toContain('currentColor')
  })
})
