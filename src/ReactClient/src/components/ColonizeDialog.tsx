import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MountainIcon } from 'lucide-react'
import axios from 'axios'
import { toast } from 'sonner'
import apiClient from '@/api/client'
import type { ColonizePreviewViewModel } from '@/api/types'
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

const SLIDER_TICKS = 1000
const LOG_THRESHOLD = 100

function sliderToAmount(s: number, max: number): number {
  if (max <= 1) return 1
  const t = s / SLIDER_TICKS
  if (max > LOG_THRESHOLD) {
    return Math.max(1, Math.min(max, Math.round(Math.exp(t * Math.log(max)))))
  }
  return Math.max(1, Math.min(max, Math.round(1 + t * (max - 1))))
}

function amountToSlider(a: number, max: number): number {
  if (max <= 1) return 0
  const clamped = Math.max(1, Math.min(max, a))
  if (max > LOG_THRESHOLD) {
    return Math.round((Math.log(clamped) / Math.log(max)) * SLIDER_TICKS)
  }
  return Math.round(((clamped - 1) / (max - 1)) * SLIDER_TICKS)
}

export function ColonizeDialog({ open, onOpenChange, gameId }: ColonizeDialogProps) {
  const queryClient = useQueryClient()
  const [amount, setAmount] = useState(1)
  const [debouncedAmount, setDebouncedAmount] = useState(1)

  useEffect(() => {
    if (open) setAmount(1)
  }, [open])

  useEffect(() => {
    const t = window.setTimeout(() => setDebouncedAmount(amount), 150)
    return () => window.clearTimeout(t)
  }, [amount])

  const { data: preview } = useQuery<ColonizePreviewViewModel>({
    queryKey: ['colonize-preview', gameId, debouncedAmount],
    queryFn: () =>
      apiClient.get(`/api/colonize/preview?amount=${debouncedAmount}`).then((r) => r.data),
    enabled: open && debouncedAmount >= 1,
    refetchInterval: open ? 10_000 : false,
    placeholderData: (prev) => prev,
  })

  const maxAffordable = preview?.maxAffordable ?? 1
  const sliderMax = Math.max(1, maxAffordable)
  const total = preview?.totalCost ?? 0
  const firstTileCost = preview?.firstTileCost ?? 0
  const lastTileCost = preview?.lastTileCost ?? 0
  const affordable = preview ? amount <= maxAffordable : false

  const sliderValue = useMemo(() => amountToSlider(amount, sliderMax), [amount, sliderMax])

  const colonizeMutation = useMutation({
    mutationFn: () => apiClient.post(`/api/colonize/colonize?amount=${amount}`, ''),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['resources', gameId] })
      void queryClient.invalidateQueries({ queryKey: ['colonize-preview', gameId] })
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
            Each new tile costs more than the last. More land means more workers over time.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-3">
          <div>
            <div className="label mb-1.5">Tiles</div>
            <input
              type="range"
              min={0}
              max={SLIDER_TICKS}
              step={1}
              value={sliderValue}
              onChange={(e) => setAmount(sliderToAmount(Number(e.target.value), sliderMax))}
              disabled={maxAffordable < 1}
              className="w-full accent-primary"
              aria-label="Tiles to colonize"
            />
          </div>

          <NumberStepper value={amount} onChange={setAmount} min={1} max={Math.max(1, maxAffordable)} />

          <div>
            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={() => setAmount(Math.max(1, maxAffordable))}
              disabled={maxAffordable < 1}
              title="Set to the largest amount your minerals can afford"
            >
              Max affordable ({formatNumber(maxAffordable)})
            </Button>
          </div>
        </div>

        <div className={cn(
          'rounded-md border px-3 py-2 flex flex-col gap-1 text-sm',
          affordable ? 'border-success/40 bg-success/5' : 'border-destructive/40 bg-destructive/5',
        )}>
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Total</span>
            <span className="inline-flex items-center gap-1 mono">
              <ResourceIcon name="minerals" /> {formatNumber(total)}
            </span>
          </div>
          {amount > 1 && (
            <div className="flex items-center justify-between gap-3 text-xs text-muted-foreground">
              <span>Per tile</span>
              <span className="mono">
                {formatNumber(firstTileCost)} → {formatNumber(lastTileCost)}
              </span>
            </div>
          )}
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
