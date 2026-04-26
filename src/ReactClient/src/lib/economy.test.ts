import { describe, it, expect } from 'vitest'
import { workerIncome, landPerWorker, type EconomyFormula } from './economy'

const sco: EconomyFormula = {
  baseIncomePerTick: 10,
  maxIncomePerWorker: 4,
  mineralSweetSpotLandPerWorker: 3,
  gasSweetSpotLandPerWorker: 6,
  minEfficiency: 0.002,
}

describe('workerIncome (mineral pool)', () => {
  const sweet = sco.mineralSweetSpotLandPerWorker

  it('returns just the base income when there are no workers', () => {
    expect(workerIncome(0, 100, sweet, sco)).toBe(10)
  })

  it('caps at full efficiency at the sweet spot', () => {
    // 20 workers × 3 land/worker = 60 land → eff 1.0
    // 10 + 20 × 4 × 1.0 = 90
    expect(workerIncome(20, 60, sweet, sco)).toBe(90)
  })

  it('does not exceed full efficiency past the sweet spot', () => {
    expect(workerIncome(20, 600, sweet, sco)).toBe(90)
  })

  it('scales linearly below the sweet spot', () => {
    // 20 workers, 30 land → ratio 0.5 → 10 + 20 × 4 × 0.5 = 50
    expect(workerIncome(20, 30, sweet, sco)).toBe(50)
  })

  it('floors at minEfficiency for tiny land', () => {
    // 100 workers, 0 land → eff floored at 0.002
    // 10 + 100 × 4 × 0.002 = 10.8
    expect(workerIncome(100, 0, sweet, sco)).toBeCloseTo(10.8, 5)
  })
})

describe('workerIncome (gas pool — bigger sweet spot)', () => {
  const sweet = sco.gasSweetSpotLandPerWorker

  it('reaches full efficiency only with twice the land per worker', () => {
    // 10 workers, 60 land, sweet 6 → ratio 1.0 → eff 1.0
    expect(workerIncome(10, 60, sweet, sco)).toBe(50)
  })

  it('lags minerals at the same land/worker ratio', () => {
    // 10 workers, 30 land → mineral pool would be at sweet (eff 1)
    // gas pool sweet = 6, ratio = 30/(10*6) = 0.5 → eff 0.5
    // 10 + 10 × 4 × 0.5 = 30
    expect(workerIncome(10, 30, sweet, sco)).toBe(30)
  })
})

describe('landPerWorker', () => {
  it('returns NaN when there are no workers (so callers can render "—")', () => {
    expect(landPerWorker(0, 100)).toBeNaN()
  })

  it('divides land by worker count', () => {
    expect(landPerWorker(20, 60)).toBe(3)
  })
})
