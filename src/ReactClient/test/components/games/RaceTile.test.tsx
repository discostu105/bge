import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { RaceTile } from '@/components/games/RaceTile'

describe('RaceTile', () => {
  it('renders the race name and description', () => {
    render(<RaceTile race="terran" selected={false} onSelect={() => {}} />)
    expect(screen.getByText(/terran/i)).toBeDefined()
    expect(screen.getByText(/industrial/i)).toBeDefined()
  })

  it('calls onSelect with the race when clicked', () => {
    const onSelect = vi.fn()
    render(<RaceTile race="zerg" selected={false} onSelect={onSelect} />)
    fireEvent.click(screen.getByRole('button'))
    expect(onSelect).toHaveBeenCalledWith('zerg')
  })

  it('applies aria-pressed when selected', () => {
    render(<RaceTile race="protoss" selected={true} onSelect={() => {}} />)
    expect(screen.getByRole('button').getAttribute('aria-pressed')).toBe('true')
  })
})
