import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MountainIcon } from 'lucide-react'
import axios from 'axios'
import { toast } from 'sonner'
import apiClient from '@/api/client'
import type { PlayerResourcesViewModel } from '@/api/types'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { NumberStepper } from '@/components/ui/number-stepper'
import { ResourceIcon } from '@/components/ui/resource-icon'
import { formatNumber } from '@/lib/formatters'
import { cn } from '@/lib/utils'

interface ColonizeDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
}

const PRESETS = [1, 5, 10, 20]
const MAX_PER_CALL = 24

export function ColonizeDialog({ open, onOpenChange, gameId }: ColonizeDialogProps) {
  const queryClient = useQueryClient()
  const [amount, setAmount] = useState(1)

  const { data: resources } = useQuery<PlayerResourcesViewModel>({
    queryKey: ['resources', gameId],
    queryFn: () => apiClient.get('/api/resources').then((r) => r.data),
    enabled: open,
    refetchInterval: open ? 10_000 : false,
  })

  useEffect(() => {
    if (open) setAmount(1)
  }, [open])

  const costPerLand = resources?.colonizationCostPerLand ?? 0
  const minerals =
    (resources?.primaryResource.cost.minerals ?? 0) +
    (resources?.secondaryResources.cost.minerals ?? 0)
  const maxByResources = costPerLand > 0 ? Math.floor(minerals / costPerLand) : MAX_PER_CALL
  const maxAllowed = Math.min(MAX_PER_CALL, Math.max(1, maxByResources || 1))
  const total = amount * costPerLand
  const affordable = total <= minerals

  const colonizeMutation = useMutation({
    mutationFn: () => apiClient.post(`/api/colonize/colonize?amount=${amount}`, ''),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
      toast.success(`Colonized +${formatNumber(amount)} land`)
      onOpenChange(false)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) toast.error((err.response?.data as string) ?? 'Colonize failed')
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MountainIcon className="h-5 w-5 text-primary" aria-hidden /> Colonize land
          </DialogTitle>
          <DialogDescription>
            Claim new land at <span className="mono">{formatNumber(costPerLand)}</span> <ResourceIcon name="minerals" className="inline" /> per tile. More land means more workers over time.
          </DialogDescription>
        </DialogHeader>

        <div>
          <div className="label mb-1.5">Tiles</div>
          <NumberStepper
            value={amount}
            onChange={setAmount}
            min={1}
            max={MAX_PER_CALL}
            presets={PRESETS.filter((p) => p <= MAX_PER_CALL)}
          />
          <div className="mt-2">
            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={() => setAmount(maxAllowed)}
              title="Colonize as much as your minerals allow (capped at 24)"
            >
              Max affordable ({formatNumber(maxAllowed)})
            </Button>
          </div>
        </div>

        <div className={cn(
          'rounded-md border px-3 py-2 flex items-center justify-between gap-3 text-sm',
          affordable ? 'border-success/40 bg-success/5' : 'border-destructive/40 bg-destructive/5',
        )}>
          <span className="text-muted-foreground">Total</span>
          <span className="inline-flex items-center gap-1 mono">
            <ResourceIcon name="minerals" /> {formatNumber(total)}
          </span>
        </div>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={colonizeMutation.isPending}>
            Cancel
          </Button>
          <Button
            onClick={() => colonizeMutation.mutate()}
            disabled={!affordable || amount < 1 || colonizeMutation.isPending}
          >
            {colonizeMutation.isPending ? 'Colonizing…' : `Colonize +${formatNumber(amount)}`}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
