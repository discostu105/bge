interface SkeletonRowProps {
	cols: number
}

export function SkeletonRow({ cols }: SkeletonRowProps) {
	return (
		<tr>
			{Array.from({ length: cols }).map((_, i) => (
				<td key={i} className="px-4 py-2">
					<div className="h-4 rounded bg-secondary animate-pulse" />
				</td>
			))}
		</tr>
	)
}
