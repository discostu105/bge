import { useEffect } from 'react'
import { MinusIcon, PlusIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface NumberStepperProps {
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  step?: number
  /** Preset buttons shown below the stepper. When a value exceeds max, it clamps. */
  presets?: number[]
  /** Shows a "Max" preset that calls onChange(max) when clicked. Requires max to be finite. */
  showMax?: boolean
  disabled?: boolean
  /** Optional id for the input element (for labels). */
  id?: string
  className?: string
  inputClassName?: string
  /** Visual label shown inline after the numeric input, e.g. "units". */
  unit?: string
}

function clamp(v: number, min: number, max: number): number {
  if (Number.isNaN(v)) return min
  return Math.max(min, Math.min(max, Math.floor(v)))
}

export function NumberStepper({
  value, onChange, min = 0, max = Number.MAX_SAFE_INTEGER, step = 1,
  presets, showMax, disabled, id, className, inputClassName, unit,
}: NumberStepperProps) {
  const set = (v: number) => onChange(clamp(v, min, max))

  useEffect(() => {
    const clamped = clamp(value, min, max)
    if (clamped !== value) onChange(clamped)
  }, [value, min, max, onChange])

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowUp') { e.preventDefault(); set(value + step) }
    else if (e.key === 'ArrowDown') { e.preventDefault(); set(value - step) }
    else if (e.key === 'Home') { e.preventDefault(); set(min) }
    else if (e.key === 'End' && Number.isFinite(max)) { e.preventDefault(); set(max) }
  }

  const presetList = presets ?? []
  const hasPresets = presetList.length > 0 || showMax

  return (
    <div className={cn('flex flex-col gap-2', className)}>
      <div className="inline-flex items-stretch gap-1">
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          onClick={() => set(value - step)}
          disabled={disabled || value <= min}
          aria-label="Decrease"
        >
          <MinusIcon className="h-3.5 w-3.5" />
        </Button>
        <Input
          id={id}
          type="number"
          inputMode="numeric"
          min={min}
          max={Number.isFinite(max) ? max : undefined}
          step={step}
          value={Number.isFinite(value) ? value : ''}
          onChange={(e) => set(Number(e.target.value))}
          onKeyDown={onKeyDown}
          disabled={disabled}
          className={cn('w-20 text-center mono tabular-nums', inputClassName)}
        />
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          onClick={() => set(value + step)}
          disabled={disabled || value >= max}
          aria-label="Increase"
        >
          <PlusIcon className="h-3.5 w-3.5" />
        </Button>
        {unit && <span className="self-center text-xs text-muted-foreground ml-1">{unit}</span>}
      </div>
      {hasPresets && (
        <div className="flex flex-wrap gap-1">
          {presetList.map((p) => {
            const clamped = clamp(p, min, max)
            if (clamped !== p) return null
            return (
              <Button
                key={p}
                type="button"
                variant={value === p ? 'secondary' : 'ghost'}
                size="xs"
                onClick={() => set(p)}
                disabled={disabled}
              >
                {p}
              </Button>
            )
          })}
          {showMax && Number.isFinite(max) && max > 0 && (
            <Button
              type="button"
              variant={value === max ? 'secondary' : 'ghost'}
              size="xs"
              onClick={() => set(max)}
              disabled={disabled}
              title={`Maximum: ${max}`}
            >
              Max
            </Button>
          )}
        </div>
      )}
    </div>
  )
}
