export interface EconomyFormula {
  baseIncomePerTick: number
  maxIncomePerWorker: number
  mineralSweetSpotLandPerWorker: number
  gasSweetSpotLandPerWorker: number
  minEfficiency: number
}

/**
 * Predicted income per tick for one worker pool. Mirrors
 * ResourceGrowthSco.CalculateWorkerIncome on the server. Returns just the flat
 * base income when there are no workers in this pool.
 */
export function workerIncome(
  workers: number,
  land: number,
  sweetSpotLandPerWorker: number,
  formula: EconomyFormula,
): number {
  if (workers <= 0) return formula.baseIncomePerTick
  const ratio = land / (workers * sweetSpotLandPerWorker)
  const eff = Math.min(1, Math.max(formula.minEfficiency, ratio))
  return formula.baseIncomePerTick + workers * formula.maxIncomePerWorker * eff
}

export function landPerWorker(workers: number, land: number): number {
  if (workers <= 0) return NaN
  return land / workers
}
