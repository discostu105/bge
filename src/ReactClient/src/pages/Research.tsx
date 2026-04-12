import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { useState } from 'react'
import apiClient from '@/api/client'
import type { UpgradesViewModel } from '@/api/types'
import { CostBadge } from '@/components/CostBadge'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

interface ResearchProps {
  gameId: string
}

function upgradeNodeClass(
  upgradeType: string,
  level: number,
  currentLevel: number,
  upgradeBeingResearched: string
): string {
  if (level <= currentLevel) return 'completed'
  if (level === currentLevel + 1) {
    if (upgradeBeingResearched === upgradeType) return 'in-progress'
    return 'available'
  }
  return 'locked'
}

export function Research({ gameId }: ResearchProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const { data: upgrades, isLoading: upgradesLoading, error: upgradesError, refetch: refetchUpgrades } = useQuery<UpgradesViewModel>({
    queryKey: ['upgrades', gameId],
    queryFn: () => apiClient.get('/api/upgrades').then((r) => r.data),
    refetchInterval: 10_000,
  })

  const researchUpgradeMutation = useMutation({
    mutationFn: (upgradeType: string) =>
      apiClient.post(`/api/upgrades/research?upgradeType=${upgradeType}`, ''),
    onSuccess: () => {
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['upgrades', gameId] })
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Research failed')
    },
  })

  if (upgradesLoading) return <PageLoader message="Loading research..." />
  if (upgradesError) {
    return <ApiError message="Failed to load research data." onRetry={() => { void refetchUpgrades() }} />
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Research</h1>

      {error && <div className="text-destructive text-sm">{error}</div>}

      {upgrades && (
        <div>
          <h2 className="text-lg font-semibold mb-4">Upgrades</h2>

          {/* Attack upgrades */}
          <div className="mb-4">
            <div className="text-sm font-medium text-muted-foreground mb-2 w-24 shrink-0">
              Attack
            </div>
            <div className="flex items-start gap-2 flex-wrap">
              {Array.from({ length: upgrades.maxUpgradeLevel }, (_, i) => i + 1).map((level) => {
                const nodeClass = upgradeNodeClass(
                  'Attack',
                  level,
                  upgrades.attackUpgradeLevel,
                  upgrades.upgradeBeingResearched
                )
                const statusStyles: Record<string, string> = {
                  completed: 'border-green-700 bg-green-900/15',
                  'in-progress': 'border-primary bg-card',
                  available: 'border-primary shadow-[0_0_6px_hsl(var(--color-primary))]',
                  locked: 'border-border opacity-45 grayscale',
                }
                return (
                  <div
                    key={level}
                    className={`flex items-start gap-2`}
                  >
                    <div className={`rounded-lg border-2 p-3 w-36 ${statusStyles[nodeClass] ?? ''}`}>
                      <div className="font-bold text-sm mb-1">Level {level}</div>
                      {level <= upgrades.attackUpgradeLevel ? (
                        <div className="text-xs text-green-400 font-bold">✓ Completed</div>
                      ) : nodeClass === 'in-progress' ? (
                        <div className="text-xs text-primary">
                          Researching… ({upgrades.upgradeResearchTimer} rounds)
                        </div>
                      ) : nodeClass === 'available' ? (
                        <>
                          {upgrades.nextAttackUpgradeCost && (
                            <div className="text-xs mb-2">
                              <CostBadge cost={upgrades.nextAttackUpgradeCost} />
                            </div>
                          )}
                          <button
                            onClick={() => researchUpgradeMutation.mutate('Attack')}
                            className="rounded bg-primary px-2 py-1 text-xs text-primary-foreground hover:opacity-90"
                          >
                            Research
                          </button>
                        </>
                      ) : level === upgrades.attackUpgradeLevel + 1 &&
                        upgrades.upgradeBeingResearched !== 'None' ? (
                        <div className="text-xs text-muted-foreground">
                          Another in progress
                        </div>
                      ) : (
                        <div className="text-xs text-muted-foreground">Locked</div>
                      )}
                    </div>
                    {level < upgrades.maxUpgradeLevel && (
                      <div className="text-xl text-muted-foreground pt-3 shrink-0">→</div>
                    )}
                  </div>
                )
              })}
            </div>
          </div>

          {/* Defense upgrades */}
          <div className="mb-4">
            <div className="text-sm font-medium text-muted-foreground mb-2 w-24 shrink-0">
              Defense
            </div>
            <div className="flex items-start gap-2 flex-wrap">
              {Array.from({ length: upgrades.maxUpgradeLevel }, (_, i) => i + 1).map((level) => {
                const nodeClass = upgradeNodeClass(
                  'Defense',
                  level,
                  upgrades.defenseUpgradeLevel,
                  upgrades.upgradeBeingResearched
                )
                const statusStyles: Record<string, string> = {
                  completed: 'border-green-700 bg-green-900/15',
                  'in-progress': 'border-primary bg-card',
                  available: 'border-primary shadow-[0_0_6px_hsl(var(--color-primary))]',
                  locked: 'border-border opacity-45 grayscale',
                }
                return (
                  <div key={level} className="flex items-start gap-2">
                    <div className={`rounded-lg border-2 p-3 w-36 ${statusStyles[nodeClass] ?? ''}`}>
                      <div className="font-bold text-sm mb-1">Level {level}</div>
                      {level <= upgrades.defenseUpgradeLevel ? (
                        <div className="text-xs text-green-400 font-bold">✓ Completed</div>
                      ) : nodeClass === 'in-progress' ? (
                        <div className="text-xs text-primary">
                          Researching… ({upgrades.upgradeResearchTimer} rounds)
                        </div>
                      ) : nodeClass === 'available' ? (
                        <>
                          {upgrades.nextDefenseUpgradeCost && (
                            <div className="text-xs mb-2">
                              <CostBadge cost={upgrades.nextDefenseUpgradeCost} />
                            </div>
                          )}
                          <button
                            onClick={() => researchUpgradeMutation.mutate('Defense')}
                            className="rounded bg-primary px-2 py-1 text-xs text-primary-foreground hover:opacity-90"
                          >
                            Research
                          </button>
                        </>
                      ) : level === upgrades.defenseUpgradeLevel + 1 &&
                        upgrades.upgradeBeingResearched !== 'None' ? (
                        <div className="text-xs text-muted-foreground">
                          Another in progress
                        </div>
                      ) : (
                        <div className="text-xs text-muted-foreground">Locked</div>
                      )}
                    </div>
                    {level < upgrades.maxUpgradeLevel && (
                      <div className="text-xl text-muted-foreground pt-3 shrink-0">→</div>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
