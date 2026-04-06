import { Component, type ReactNode } from 'react'

interface Props {
	children: ReactNode
}

interface State {
	hasError: boolean
	error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
	constructor(props: Props) {
		super(props)
		this.state = { hasError: false, error: null }
	}

	static getDerivedStateFromError(error: Error): State {
		return { hasError: true, error }
	}

	resetBoundary = () => {
		this.setState({ hasError: false, error: null })
	}

	render() {
		if (this.state.hasError) {
			const isDev = import.meta.env.DEV
			return (
				<div role="alert" className="p-6">
					<div className="rounded-lg border border-destructive/50 bg-destructive/10 p-6 max-w-md">
						<div className="text-4xl mb-3">💥</div>
						<h2 className="text-lg font-semibold mb-2">Something went wrong</h2>
						<p className="text-sm text-muted-foreground mb-4">
							An unexpected error occurred on this page.
						</p>
						{isDev && this.state.error && (
							<details className="mb-4 text-xs text-destructive font-mono">
								<summary className="cursor-pointer mb-1">Error details</summary>
								<pre className="whitespace-pre-wrap break-all bg-destructive/10 p-2 rounded">
									{this.state.error.message}
									{this.state.error.stack && '\n\n' + this.state.error.stack}
								</pre>
							</details>
						)}
						<button
							onClick={this.resetBoundary}
							className="rounded bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90"
						>
							Try Again
						</button>
					</div>
				</div>
			)
		}

		return this.props.children
	}
}
