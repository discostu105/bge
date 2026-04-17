import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { useState } from 'react'
import apiClient from '@/api/client'
import type { UpgradesViewModel } from '@/api/types'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'
import { PageHeader } from '@/components/ui/page-header'
import { UpgradeTrack } from '@/components/UpgradeTrack'

export function Research({ gameId }: { gameId: string }) {
	const queryClient = useQueryClient()
	const [error, setError] = useState<string | null>(null)

	const { data, isLoading, error: queryError, refetch } = useQuery<UpgradesViewModel>({
		queryKey: ['upgrades', gameId],
		queryFn: () => apiClient.get('/api/upgrades').then((r) => r.data),
		refetchInterval: 10_000,
	})

	const upgradeMutation = useMutation({
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

	if (isLoading) return <PageLoader message="Loading research..." />
	if (queryError) {
		return <ApiError message="Failed to load research data." onRetry={() => { void refetch() }} />
	}

	const upgrades = data!

	return (
		<>
			<PageHeader title="Research" description="Upgrade your units' combat capabilities." />

			{error && <div className="text-destructive text-sm mb-4">{error}</div>}

			<UpgradeTrack
				type="Attack"
				currentLevel={upgrades.attackUpgradeLevel}
				maxLevel={upgrades.maxUpgradeLevel}
				upgradeBeingResearched={upgrades.upgradeBeingResearched}
				researchTimer={upgrades.upgradeResearchTimer}
				nextCost={upgrades.nextAttackUpgradeCost?.cost ?? null}
				canAfford={true}
				isResearching={upgradeMutation.isPending}
				onResearch={() => upgradeMutation.mutate('Attack')}
				description="Increase attack damage of all units."
			/>
			<UpgradeTrack
				type="Defense"
				currentLevel={upgrades.defenseUpgradeLevel}
				maxLevel={upgrades.maxUpgradeLevel}
				upgradeBeingResearched={upgrades.upgradeBeingResearched}
				researchTimer={upgrades.upgradeResearchTimer}
				nextCost={upgrades.nextDefenseUpgradeCost?.cost ?? null}
				canAfford={true}
				isResearching={upgradeMutation.isPending}
				onResearch={() => upgradeMutation.mutate('Defense')}
				description="Increase defense of all units."
			/>
		</>
	)
}
