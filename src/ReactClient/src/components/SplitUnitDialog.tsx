import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { SplitIcon } from 'lucide-react'
import axios from 'axios'
import apiClient from '@/api/client'
import type { UnitViewModel } from '@/api/types'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { NumberStepper } from '@/components/ui/number-stepper'
import { formatNumber } from '@/lib/formatters'

interface SplitUnitDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  gameId: string
  unit: UnitViewModel | null
}

export function SplitUnitDialog({ open, onOpenChange, gameId, unit }: SplitUnitDialogProps) {
  const queryClient = useQueryClient()
  const [splitCount, setSplitCount] = useState(1)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open && unit) {
      setSplitCount(Math.max(1, Math.floor(unit.count / 2)))
      setError(null)
    }
  }, [open, unit])

  const splitMutation = useMutation({
    mutationFn: () => {
      if (!unit) throw new Error('No unit')
      return apiClient.post(`/api/units/split?unitId=${unit.unitId}&splitCount=${splitCount}`, '')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['units', gameId] })
      onOpenChange(false)
    },
    onError: (err) => {
      if (axios.isAxiosError(err)) setError((err.response?.data as string) ?? 'Split failed')
      else setError('Split failed')
    },
  })

  if (!unit) return null
  const maxSplit = Math.max(1, unit.count - 1)
  const half = Math.max(1, Math.floor(unit.count / 2))

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <SplitIcon className="h-5 w-5 text-primary" aria-hidden /> Split stack
          </DialogTitle>
          <DialogDescription>
            Move units from this {unit.definition.name} stack ({formatNumber(unit.count)}) into a new stack.
          </DialogDescription>
        </DialogHeader>

        <div>
          <div className="label mb-1.5">Units to move</div>
          <NumberStepper
            value={splitCount}
            onChange={setSplitCount}
            min={1}
            max={maxSplit}
            presets={[1, half].filter((v, i, arr) => arr.indexOf(v) === i && v <= maxSplit)}
          />
          <div className="mt-2 text-xs text-muted-foreground">
            Remaining in original stack: <span className="mono">{formatNumber(unit.count - splitCount)}</span>
          </div>
        </div>

        {error && <div className="text-sm text-destructive">{error}</div>}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={splitMutation.isPending}>
            Cancel
          </Button>
          <Button onClick={() => splitMutation.mutate()} disabled={splitMutation.isPending || splitCount < 1 || splitCount > maxSplit}>
            {splitMutation.isPending ? 'Splitting…' : `Split off ${formatNumber(splitCount)}`}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
