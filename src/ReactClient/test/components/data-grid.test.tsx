import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import { DataGrid } from '@/components/ui/data-grid'

describe('DataGrid', () => {
  it('renders children inside a grid', () => {
    const { container } = render(
      <DataGrid cols={2}><div>a</div><div>b</div></DataGrid>
    )
    const grid = container.firstChild as HTMLElement
    expect(grid.style.gridTemplateColumns).toBe('repeat(2, minmax(0, 1fr))')
  })

  it('defaults to 3 columns', () => {
    const { container } = render(<DataGrid><div>a</div></DataGrid>)
    const grid = container.firstChild as HTMLElement
    expect(grid.style.gridTemplateColumns).toBe('repeat(3, minmax(0, 1fr))')
  })
})
