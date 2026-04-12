import { useState, useEffect, useCallback, useRef, type ReactNode } from 'react'
import { useNavigate } from 'react-router'
import {
  XIcon,
  SwordsIcon,
  CoinsIcon,
  MailIcon,
  ZapIcon,
  AlertTriangleIcon,
  InfoIcon,
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface Toast {
  id: string
  type: string
  title: string
  body?: string
  createdAt: string
}

const MAX_VISIBLE = 3
const AUTO_DISMISS_MS = 5_000

function toastIcon(type: string): ReactNode {
  const common = 'h-5 w-5 shrink-0'
  switch (type) {
    case 'Attack':
    case 'BattleResult':
      return <SwordsIcon className={cn(common, 'text-destructive')} aria-hidden="true" />
    case 'TradeComplete':
    case 'MarketOrderFilled':
      return <CoinsIcon className={cn(common, 'text-warning')} aria-hidden="true" />
    case 'MessageReceived':
      return <MailIcon className={cn(common, 'text-primary')} aria-hidden="true" />
    case 'GameEvent':
      return <ZapIcon className={cn(common, 'text-primary')} aria-hidden="true" />
    case 'Warning':
      return <AlertTriangleIcon className={cn(common, 'text-warning')} aria-hidden="true" />
    default:
      return <InfoIcon className={cn(common, 'text-muted-foreground')} aria-hidden="true" />
  }
}

function toastRoute(type: string): string | null {
  switch (type) {
    case 'Attack':
    case 'BattleResult': return 'units'
    case 'TradeComplete':
    case 'MarketOrderFilled': return 'market'
    case 'MessageReceived': return 'messages'
    default: return null
  }
}

export function useNotificationToasts() {
  const [toasts, setToasts] = useState<Toast[]>([])

  const addToast = useCallback((toast: Omit<Toast, 'id'>) => {
    const id = crypto.randomUUID()
    setToasts((prev) => [{ ...toast, id }, ...prev].slice(0, MAX_VISIBLE + 5))
  }, [])

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  return { toasts: toasts.slice(0, MAX_VISIBLE), addToast, dismiss }
}

export function NotificationToasts({
  toasts,
  dismiss,
  gameId,
}: {
  toasts: Toast[]
  dismiss: (id: string) => void
  gameId: string | null
}) {
  if (toasts.length === 0) return null

  return (
    <div className="fixed top-3 right-3 z-50 flex flex-col gap-2 w-80">
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} dismiss={dismiss} gameId={gameId} />
      ))}
    </div>
  )
}

function ToastItem({
  toast,
  dismiss,
  gameId,
}: {
  toast: Toast
  dismiss: (id: string) => void
  gameId: string | null
}) {
  const navigate = useNavigate()
  const timerRef = useRef<ReturnType<typeof setTimeout>>(undefined)

  useEffect(() => {
    timerRef.current = setTimeout(() => dismiss(toast.id), AUTO_DISMISS_MS)
    return () => clearTimeout(timerRef.current)
  }, [toast.id, dismiss])

  function handleClick() {
    const route = toastRoute(toast.type)
    if (route && gameId) {
      navigate(`/games/${gameId}/${route}`)
    }
    dismiss(toast.id)
  }

  return (
    <div
      onClick={handleClick}
      className={cn(
        'flex items-start gap-2.5 rounded-lg border bg-card px-4 py-3 shadow-lg',
        'animate-in slide-in-from-right-4 fade-in duration-300',
        'cursor-pointer hover:bg-secondary/40 transition-colors'
      )}
    >
      <span className="mt-0.5">{toastIcon(toast.type)}</span>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium leading-snug">{toast.title}</p>
        {toast.body && (
          <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{toast.body}</p>
        )}
      </div>
      <button
        onClick={(e) => { e.stopPropagation(); dismiss(toast.id) }}
        className="shrink-0 text-muted-foreground hover:text-foreground p-0.5"
        aria-label="Dismiss"
      >
        <XIcon className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}
