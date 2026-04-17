import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import apiClient from '@/api/client'
import type { TradeResourceRequest } from '@/api/types'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ArrowRightLeftIcon } from 'lucide-react'
import { SectionHeader } from '@/components/ui/section'

export function ConvertResourceForm({ gameId }: { gameId: string }) {
  const qc = useQueryClient()
  const [from, setFrom] = useState<'minerals' | 'gas'>('minerals')
  const [amount, setAmount] = useState<number>(100)

  const mutation = useMutation({
    mutationFn: (payload: TradeResourceRequest) =>
      apiClient.post('/api/resources/trade', payload).then((r) => r.data),
    onSuccess: () => {
      toast.success('Resources converted')
      qc.invalidateQueries({ queryKey: ['resources', gameId] })
      setAmount(100)
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Conversion failed'
      toast.error(msg)
    },
  })

  const flip = () => setFrom((f) => (f === 'minerals' ? 'gas' : 'minerals'))
  const to = from === 'minerals' ? 'gas' : 'minerals'

  return (
    <section className="mb-6">
      <SectionHeader title="Convert resources" description="Swap minerals for gas or vice-versa at the current market rate." />
      <Card>
        <CardContent className="p-4 space-y-3 max-w-lg">
          <div className="grid grid-cols-[1fr_auto_1fr] items-end gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="convert-amount">Amount of {from}</Label>
              <Input
                id="convert-amount"
                type="number"
                min={1}
                value={amount}
                onChange={(e) => setAmount(Number(e.target.value))}
              />
            </div>
            <Button type="button" variant="ghost" size="icon" onClick={flip} aria-label="Swap direction" className="mb-0.5">
              <ArrowRightLeftIcon className="h-4 w-4" />
            </Button>
            <div className="space-y-1.5">
              <Label className="text-muted-foreground">→ {to}</Label>
              <div className="h-9 flex items-center px-3 rounded-md border bg-muted/30 text-sm text-muted-foreground mono">
                (market rate applied)
              </div>
            </div>
          </div>
          <Button
            variant="default"
            size="sm"
            disabled={mutation.isPending || amount <= 0}
            onClick={() => mutation.mutate({ fromResource: from, amount })}
          >
            {mutation.isPending ? 'Converting…' : `Convert ${from} → ${to}`}
          </Button>
        </CardContent>
      </Card>
    </section>
  )
}
