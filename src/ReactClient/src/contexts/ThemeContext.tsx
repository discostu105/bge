import { createContext, useContext, useEffect, useState } from 'react'

type Theme = 'dark' | 'light'

interface ThemeContextValue {
	theme: Theme
	toggleTheme: () => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

function getInitialTheme(): Theme {
	const stored = localStorage.getItem('bge-theme')
	if (stored === 'dark' || stored === 'light') return stored
	if (window.matchMedia('(prefers-color-scheme: light)').matches) return 'light'
	return 'dark'
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
	const [theme, setTheme] = useState<Theme>(getInitialTheme)

	useEffect(() => {
		const root = document.documentElement
		root.classList.toggle('light', theme === 'light')
		localStorage.setItem('bge-theme', theme)
	}, [theme])

	function toggleTheme() {
		setTheme((prev) => (prev === 'dark' ? 'light' : 'dark'))
	}

	return (
		<ThemeContext.Provider value={{ theme, toggleTheme }}>
			{children}
		</ThemeContext.Provider>
	)
}

export function useTheme(): ThemeContextValue {
	const ctx = useContext(ThemeContext)
	if (!ctx) throw new Error('useTheme must be used within ThemeProvider')
	return ctx
}
