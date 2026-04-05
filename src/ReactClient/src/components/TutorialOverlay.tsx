import { useState, useEffect, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { XIcon, ChevronRightIcon, ChevronLeftIcon, SkipForwardIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { PlayerProfileViewModel } from '@/api/types'
import { cn } from '@/lib/utils'

interface TutorialStep {
  title: string
  body: string
  icon: string
}

const STEPS: TutorialStep[] = [
  {
    title: 'Welcome, Commander!',
    body: 'This quick tutorial will walk you through the basics. You can skip it at any time or revisit the Help page later.',
    icon: '👋',
  },
  {
    title: 'Your Base',
    body: 'Head to Base to see your buildings. Your Command Center is already built — it produces workers that gather resources for you.',
    icon: '🏗️',
  },
  {
    title: 'Gathering Resources',
    body: 'Assign workers to mine Minerals and Vespene Gas. Open your Base page and use the worker assignment panel to balance production.',
    icon: '⛏️',
  },
  {
    title: 'Constructing Buildings',
    body: 'Buildings unlock new units and upgrades. Each building costs resources and a build slot. Check the Base page to start construction.',
    icon: '🏭',
  },
  {
    title: 'Training Units',
    body: 'Go to Units to train military forces. Each unit type has different stats — hover over them to see attack, defense, and speed.',
    icon: '⚔️',
  },
  {
    title: 'Attacking Enemies',
    body: 'Select a unit on the Units page and choose an enemy to attack. Battles are resolved automatically based on unit strength and upgrades.',
    icon: '🎯',
  },
  {
    title: 'You\'re Ready!',
    body: 'That covers the basics. Explore Research for upgrades, Market for trading, and Alliances to team up. Good luck, Commander!',
    icon: '🚀',
  },
]

export function TutorialOverlay() {
  const queryClient = useQueryClient()
  const [step, setStep] = useState(0)
  const [dismissed, setDismissed] = useState(false)
  const dialogRef = useRef<HTMLDivElement>(null)

  const { data: profile } = useQuery<PlayerProfileViewModel>({
    queryKey: ['player-profile-tutorial'],
    queryFn: () => apiClient.get('/api/playerprofile').then((r) => r.data),
  })

  const completeMut = useMutation({
    mutationFn: () => apiClient.post('/api/playerprofile/complete-tutorial'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['player-profile-tutorial'] })
    },
  })

  useEffect(() => {
    dialogRef.current?.focus()
  }, [step])

  // Handle escape key
  useEffect(() => {
    function handleEscape(e: KeyboardEvent) {
      if (e.key === 'Escape') handleSkip()
    }
    window.addEventListener('keydown', handleEscape)
    return () => window.removeEventListener('keydown', handleEscape)
  }, [])

  if (!profile || profile.tutorialCompleted || dismissed) return null

  function handleSkip() {
    setDismissed(true)
    completeMut.mutate()
  }

  function handleNext() {
    if (step < STEPS.length - 1) {
      setStep(step + 1)
    } else {
      handleSkip()
    }
  }

  function handlePrev() {
    if (step > 0) setStep(step - 1)
  }

  const current = STEPS[step]
  const isLast = step === STEPS.length - 1

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/60"
      onClick={handleSkip}
    >
      <div
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-label="Tutorial"
        className="relative mx-4 w-full max-w-md rounded-lg border bg-card p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Close button */}
        <button
          onClick={handleSkip}
          className="absolute top-3 right-3 p-1 rounded hover:bg-secondary text-muted-foreground"
          aria-label="Skip tutorial"
        >
          <XIcon className="h-4 w-4" />
        </button>

        {/* Step indicator */}
        <div className="flex gap-1 mb-4">
          {STEPS.map((_, i) => (
            <div
              key={i}
              className={cn(
                'h-1 flex-1 rounded-full transition-colors',
                i <= step ? 'bg-primary' : 'bg-muted'
              )}
            />
          ))}
        </div>

        {/* Content */}
        <div className="text-center mb-6">
          <div className="text-4xl mb-3">{current.icon}</div>
          <h2 className="text-lg font-bold mb-2">{current.title}</h2>
          <p className="text-sm text-muted-foreground leading-relaxed">{current.body}</p>
        </div>

        {/* Step counter */}
        <p className="text-xs text-muted-foreground text-center mb-4">
          Step {step + 1} of {STEPS.length}
        </p>

        {/* Navigation */}
        <div className="flex justify-between items-center">
          <button
            onClick={handleSkip}
            className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
          >
            <SkipForwardIcon className="h-3.5 w-3.5" />
            Skip tutorial
          </button>

          <div className="flex gap-2">
            {step > 0 && (
              <button
                onClick={handlePrev}
                className="flex items-center gap-1 rounded-md border px-3 py-1.5 text-sm hover:bg-secondary transition-colors"
              >
                <ChevronLeftIcon className="h-4 w-4" />
                Back
              </button>
            )}
            <button
              onClick={handleNext}
              className="flex items-center gap-1 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
            >
              {isLast ? 'Get Started' : 'Next'}
              {!isLast && <ChevronRightIcon className="h-4 w-4" />}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
