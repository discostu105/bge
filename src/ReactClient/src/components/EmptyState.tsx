import type { ReactNode } from 'react'
import { Link } from 'react-router'

type ActionProp =
	| { label: string; onClick: () => void; to?: never }
	| { label: string; to: string; onClick?: never }

interface EmptyStateProps {
	icon: ReactNode
	title: string
	description?: string
	action?: ActionProp
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
	return (
		<div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
			<div className="text-muted-foreground [&>svg]:size-6">
				{icon}
			</div>
			<p className="text-lg font-semibold">{title}</p>
			{description && (
				<p className="text-sm text-muted-foreground max-w-xs">{description}</p>
			)}
			{action && (
				action.to ? (
					<Link
						to={action.to}
						className="mt-1 rounded border px-4 py-1.5 text-sm hover:bg-secondary transition-colors"
					>
						{action.label}
					</Link>
				) : (
					<button
						onClick={action.onClick}
						className="mt-1 rounded border px-4 py-1.5 text-sm hover:bg-secondary transition-colors"
					>
						{action.label}
					</button>
				)
			)}
		</div>
	)
}
