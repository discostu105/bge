import { useCallback, useRef, useState } from 'react'

interface TouchZoomPanState {
	scale: number
	translateX: number
	translateY: number
}

interface UseTouchZoomPanResult {
	containerProps: {
		style: React.CSSProperties
		onTouchStart: (e: React.TouchEvent) => void
		onTouchMove: (e: React.TouchEvent) => void
		onTouchEnd: () => void
		onPointerMove: (e: React.PointerEvent) => void
		onPointerDown: (e: React.PointerEvent) => void
		onPointerUp: () => void
	}
	wrapperStyle: React.CSSProperties
}

const MIN_SCALE = 0.5
const MAX_SCALE = 3.0

function getDistance(t1: React.Touch, t2: React.Touch): number {
	const dx = t1.clientX - t2.clientX
	const dy = t1.clientY - t2.clientY
	return Math.sqrt(dx * dx + dy * dy)
}

export function useTouchZoomPan(): UseTouchZoomPanResult {
	const [state, setState] = useState<TouchZoomPanState>({ scale: 1, translateX: 0, translateY: 0 })
	const lastDistanceRef = useRef<number | null>(null)
	const lastPointerRef = useRef<{ x: number; y: number } | null>(null)
	const isPanningRef = useRef(false)

	const handleTouchStart = useCallback((e: React.TouchEvent) => {
		if (e.touches.length === 2) {
			lastDistanceRef.current = getDistance(e.touches[0], e.touches[1])
		}
	}, [])

	const handleTouchMove = useCallback((e: React.TouchEvent) => {
		if (e.touches.length === 2 && lastDistanceRef.current !== null) {
			const newDistance = getDistance(e.touches[0], e.touches[1])
			const delta = newDistance / lastDistanceRef.current
			lastDistanceRef.current = newDistance
			setState((prev) => ({
				...prev,
				scale: Math.min(MAX_SCALE, Math.max(MIN_SCALE, prev.scale * delta)),
			}))
		}
	}, [])

	const handleTouchEnd = useCallback(() => {
		lastDistanceRef.current = null
	}, [])

	const handlePointerDown = useCallback((e: React.PointerEvent) => {
		setState((prev) => {
			if (prev.scale > 1) {
				lastPointerRef.current = { x: e.clientX, y: e.clientY }
				isPanningRef.current = true
			}
			return prev
		})
	}, [])

	const handlePointerMove = useCallback((e: React.PointerEvent) => {
		if (!isPanningRef.current || lastPointerRef.current === null) return
		const dx = e.clientX - lastPointerRef.current.x
		const dy = e.clientY - lastPointerRef.current.y
		lastPointerRef.current = { x: e.clientX, y: e.clientY }
		setState((prev) => ({
			...prev,
			translateX: prev.translateX + dx,
			translateY: prev.translateY + dy,
		}))
	}, [])

	const handlePointerUp = useCallback(() => {
		isPanningRef.current = false
		lastPointerRef.current = null
	}, [])

	const isZoomed = state.scale > 1

	return {
		containerProps: {
			style: {
				touchAction: isZoomed ? 'none' : 'pinch-zoom',
				userSelect: 'none',
				overflow: 'hidden',
			},
			onTouchStart: handleTouchStart,
			onTouchMove: handleTouchMove,
			onTouchEnd: handleTouchEnd,
			onPointerDown: handlePointerDown,
			onPointerMove: handlePointerMove,
			onPointerUp: handlePointerUp,
		},
		wrapperStyle: {
			transform: `scale(${state.scale}) translate(${state.translateX / state.scale}px, ${state.translateY / state.scale}px)`,
			transformOrigin: 'top left',
			willChange: 'transform',
		},
	}
}
