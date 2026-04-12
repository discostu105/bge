interface GuideSection {
  title: string
  icon: string
  items: string[]
}

const SECTIONS: GuideSection[] = [
  {
    title: 'Getting Started',
    icon: '🏗️',
    items: [
      'Your Command Center is built automatically when you join a game.',
      'Assign workers to Minerals and Vespene Gas on the Base page to start gathering resources.',
      'Workers are generated passively over time — the more land you control, the more workers you get.',
    ],
  },
  {
    title: 'Building & Expanding',
    icon: '🏭',
    items: [
      'Construct buildings from the Base page to unlock new unit types and upgrades.',
      'Each building requires resources and a build slot. Higher-level buildings are more expensive.',
      'Expand your land by colonizing — this costs resources but gives you more worker capacity.',
    ],
  },
  {
    title: 'Training Units',
    icon: '⚔️',
    items: [
      'Train military units on the Units page. Different units have different attack, defense, and speed stats.',
      'Check the Unit Types page under Info for detailed stats on each unit.',
      'You can merge units of the same type to combine them into a stronger group.',
    ],
  },
  {
    title: 'Combat',
    icon: '🎯',
    items: [
      'Select a unit and click "Attack" to choose an enemy player.',
      'Battles are resolved automatically based on unit strength, upgrades, and numbers.',
      'Defeated units are lost permanently — plan your attacks carefully.',
      'New players have protection for the first few hours. Use this time to build up.',
    ],
  },
  {
    title: 'Research & Upgrades',
    icon: '🔬',
    items: [
      'Research upgrades to improve your units\' attack and defense globally.',
      'Research takes time — queue it early and let it complete while you play.',
    ],
  },
  {
    title: 'Economy & Trading',
    icon: '💰',
    items: [
      'Trade resources with other players on the Market page.',
      'Post buy/sell orders or accept existing offers.',
      'Keep a balanced economy — running out of one resource can stall your progress.',
    ],
  },
  {
    title: 'Alliances & Diplomacy',
    icon: '🤝',
    items: [
      'Create or join an alliance to team up with other players.',
      'Alliance members can chat privately and coordinate attacks.',
      'Declare wars or propose peace through the Diplomacy page.',
    ],
  },
]

export function Help() {
  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-bold">Game Guide</h1>
        <p className="text-sm text-muted-foreground mt-1">
          A quick reference for all the game mechanics. New to the game? Read through these sections to get up to speed.
        </p>
      </div>

      {SECTIONS.map((section) => (
        <div key={section.title} className="rounded-lg border bg-card p-4">
          <h2 className="flex items-center gap-2 text-base font-semibold mb-2">
            <span>{section.icon}</span>
            {section.title}
          </h2>
          <ul className="space-y-1.5">
            {section.items.map((item, i) => (
              <li key={i} className="text-sm text-muted-foreground flex gap-2">
                <span className="text-muted-foreground/50 shrink-0">•</span>
                {item}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  )
}
