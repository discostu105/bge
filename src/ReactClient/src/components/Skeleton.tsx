/** Skeleton loading primitives — use aria-hidden=true on the container. */

/** A single skeleton row for use inside a <tbody>. Mimics a table row with N cells. */
export function SkeletonRow({ cols = 5 }: { cols?: number }) {
	return (
		<tr aria-hidden="true" className="animate-pulse">
			{Array.from({ length: cols }).map((_, i) => (
				<td key={i} className="px-4 py-3">
					<div className="h-3.5 rounded bg-muted" />
				</td>
			))}
		</tr>
	)
}

/** A skeleton card block — mimics a stat card with label + value. */
export function SkeletonCard({ className = '' }: { className?: string }) {
	return (
		<div aria-hidden="true" className={`animate-pulse rounded border bg-secondary/20 p-3 ${className}`}>
			<div className="h-2.5 w-16 rounded bg-muted mb-2" />
			<div className="h-6 w-24 rounded bg-muted" />
		</div>
	)
}

/** A generic skeleton line — for headings, text, etc. */
export function SkeletonLine({ className = '' }: { className?: string }) {
	return (
		<div aria-hidden="true" className={`animate-pulse h-3 rounded bg-muted ${className}`} />
	)
}
