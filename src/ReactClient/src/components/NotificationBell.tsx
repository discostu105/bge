import { useState, useRef, useEffect } from 'react'
import { BellIcon, CheckCircleIcon } from 'lucide-react'
import { useNotifications } from '@/hooks/useNotifications'
import type { UseSignalRReturn } from '@/hooks/useSignalR'
import { relativeTime } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { EmptyState } from '@/components/EmptyState'

function notificationIcon(kind: string): string {
  switch (kind) {
    case 'Warning': return '⚠️'
    case 'GameEvent': return '⚡'
    default: return 'ℹ️'
  }
}

interface NotificationBellProps {
  signalR?: UseSignalRReturn
  onRealTimeNotification?: (n: { type: string; title: string; body?: string; createdAt: string }) => void
}

export function NotificationBell({ signalR, onRealTimeNotification }: NotificationBellProps) {
  const { notifications, dismissAll } = useNotifications({
    signalR,
    onRealTimeNotification,
  })
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [])

  const unread = notifications.length

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="relative p-2 rounded-md hover:bg-secondary transition-colors"
        aria-label={`Notifications${unread > 0 ? ` (${unread} unread)` : ''}`}
      >
        <BellIcon className="h-5 w-5" />
        {unread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 h-4 w-4 rounded-full bg-destructive text-destructive-foreground text-[10px] font-bold flex items-center justify-center">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div className={cn(
          'absolute right-0 top-full mt-1 w-80 rounded-lg border bg-popover shadow-lg z-50',
          'max-h-96 overflow-y-auto'
        )}>
          <div className="flex items-center justify-between px-3 py-2 border-b">
            <span className="font-semibold text-sm">Notifications</span>
            {unread > 0 && (
              <button
                onClick={() => { dismissAll(); setOpen(false) }}
                className="text-xs text-muted-foreground hover:text-foreground"
              >
                Clear all
              </button>
            )}
          </div>

          {notifications.length === 0 ? (
            <div className="py-2">
              <EmptyState icon={<CheckCircleIcon />} title="All caught up!" />
            </div>
          ) : (
            <ul>
              {notifications.slice(0, 15).map((n) => (
                <li key={n.id} className="flex items-start gap-2 px-3 py-2 border-b last:border-0 hover:bg-secondary/40">
                  <span className="mt-0.5">{notificationIcon(n.kind)}</span>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm leading-snug">{n.message}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">{relativeTime(n.createdAt)}</p>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
