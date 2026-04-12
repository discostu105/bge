import { createContext, type ReactNode, useCallback, useContext, useRef, useState } from 'react'
import { ConfirmDialog, type ConfirmDialogProps } from '@/components/ConfirmDialog'

type ConfirmOptions = Omit<ConfirmDialogProps, 'open' | 'onOpenChange' | 'onConfirm'>

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>

const ConfirmContext = createContext<ConfirmFn | null>(null)

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolverRef = useRef<((v: boolean) => void) | null>(null)

  const confirm = useCallback<ConfirmFn>((opts) => {
    return new Promise((resolve) => {
      resolverRef.current = resolve
      setOptions(opts)
    })
  }, [])

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      resolverRef.current?.(false)
      resolverRef.current = null
      setOptions(null)
    }
  }

  const handleConfirm = () => {
    resolverRef.current?.(true)
    resolverRef.current = null
  }

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {options && (
        <ConfirmDialog
          open
          onOpenChange={handleOpenChange}
          onConfirm={handleConfirm}
          {...options}
        />
      )}
    </ConfirmContext.Provider>
  )
}

export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext)
  if (!ctx) throw new Error('useConfirm must be used within ConfirmProvider')
  return ctx
}
