import { NavLink } from 'react-router'
import {
  LayersIcon,
  ShieldIcon,
  FlaskConicalIcon,
  ShoppingCartIcon,
  ArrowRightLeftIcon,
  EyeIcon,
  CrosshairIcon,
  BarChart2Icon,
  FlagIcon,
  UsersIcon,
  MessageSquareIcon,
  MailIcon,
  TrophyIcon,
  BookOpenIcon,
  HistoryIcon,
  HelpCircleIcon,
  RadioIcon,
  LineChartIcon,
  UserIcon,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useCurrentGame } from '@/contexts/CurrentGameContext'

function NavItem({
  to,
  icon: Icon,
  label,
}: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  label: string
}) {
  return (
    <li>
      <NavLink
        to={to}
        className={({ isActive }) =>
          cn(
            'flex items-center gap-2 px-3 py-2.5 rounded-md text-sm transition-colors min-h-[44px]',
            isActive
              ? 'bg-primary/20 text-primary font-medium'
              : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
          )
        }
      >
        <Icon className="h-4 w-4 shrink-0" aria-hidden="true" />
        {label}
      </NavLink>
    </li>
  )
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <li className="px-3 pt-4 pb-1">
      <span className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">
        {children}
      </span>
    </li>
  )
}

export function NavMenu() {
  const { gameId } = useCurrentGame()

  if (!gameId) return null

  const g = (path: string) => `/games/${gameId}/${path}`

  return (
    <nav aria-label="Game navigation">
      <ul className="space-y-0.5">
        <SectionLabel>Play</SectionLabel>
        <NavItem to={g('base')} icon={LayersIcon} label="Base" />
        <NavItem to={g('units')} icon={ShieldIcon} label="Units" />
        <NavItem to={g('research')} icon={FlaskConicalIcon} label="Research" />
        <NavItem to={g('market')} icon={ShoppingCartIcon} label="Market" />
        <NavItem to={g('trade')} icon={ArrowRightLeftIcon} label="Trade" />
        <NavItem to={g('spy')} icon={EyeIcon} label="Spy" />
        <NavItem to={g('operations')} icon={CrosshairIcon} label="Operations" />

        <SectionLabel>Info</SectionLabel>
        <NavItem to={g('live')} icon={RadioIcon} label="Live View" />
        <NavItem to={g('unitdefinitions')} icon={BookOpenIcon} label="Unit Types" />
        <NavItem to={g('ranking')} icon={BarChart2Icon} label="Ranking" />
        <NavItem to={g('help')} icon={HelpCircleIcon} label="Help" />

        <SectionLabel>Social</SectionLabel>
        <NavItem to={g('diplomacy')} icon={FlagIcon} label="Diplomacy" />
        <NavItem to={g('alliances')} icon={UsersIcon} label="Alliances" />
        <NavItem to={g('allianceranking')} icon={TrophyIcon} label="Alliance Ranking" />
        <NavItem to={g('chat')} icon={MessageSquareIcon} label="Chat" />
        <NavItem to={g('messages')} icon={MailIcon} label="Messages" />
      </ul>

      <div className="mt-6 px-3">
        <ul className="space-y-0.5">
          <SectionLabel>Account</SectionLabel>
          <NavItem to="/history" icon={HistoryIcon} label="History" />
          <NavItem to="/stats" icon={LineChartIcon} label="My Stats" />
          <NavItem to="/profile" icon={UserIcon} label="Profile" />
          <NavItem to="/achievements" icon={TrophyIcon} label="Achievements" />
        </ul>
      </div>
    </nav>
  )
}
