interface ApiErrorProps {
  message?: string
  onRetry?: () => void
}

export function ApiError({ message = 'Failed to load data.', onRetry }: ApiErrorProps) {
  return (
    <div className="p-6">
      <div className="rounded border border-destructive/50 bg-destructive/10 p-4 flex items-center justify-between gap-4">
        <span className="text-destructive text-sm">{message}</span>
        {onRetry && (
          <button
            onClick={onRetry}
            className="shrink-0 rounded bg-secondary px-3 py-1 text-sm text-secondary-foreground hover:opacity-90"
          >
            Retry
          </button>
        )}
      </div>
    </div>
  )
}
