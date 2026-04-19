import { NavLink } from 'react-router'
import {
  LayersIcon, ShieldIcon, FlaskConicalIcon, ShoppingCartIcon,
  RadioIcon, UsersIcon, BarChart2Icon, MessageSquareIcon, MailIcon,
  BookOpenIcon, HelpCircleIcon, UserIcon,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useCurrentGame } from '@/contexts/CurrentGameContext'
import { useNavBadges } from '@/hooks/useNavBadges'
import { Badge } from '@/components/ui/badge'

function PrimaryItem({
  to, icon: Icon, label, badge,
}: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  label: string
  badge?: number
}) {
  return (
    <li>
      <NavLink
        to={to}
        className={({ isActive }) => cn(
          'relative flex items-center gap-2 pl-4 pr-3 py-2.5 text-sm transition-colors min-h-[44px]',
          isActive
            ? 'text-foreground bg-[linear-gradient(90deg,var(--color-race-primary)_0%,transparent_100%)]/10 before:absolute before:left-0 before:top-1.5 before:bottom-1.5 before:w-[2px] before:bg-[color:var(--color-race-secondary)]'
            : 'text-muted-foreground hover:text-foreground hover:bg-secondary/40',
        )}
      >
        <Icon className="h-4 w-4 shrink-0 text-race-secondary" aria-hidden />
        <span className="flex-1 truncate">{label}</span>
        {badge != null && badge > 0 && (
          <Badge variant="secondary" className="h-5 px-1.5 text-[10px] tabular-nums">{badge}</Badge>
        )}
      </NavLink>
    </li>
  )
}

function UtilityLink({ to, icon: Icon, label, badge }: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  label: string
  badge?: number
}) {
  return (
    <NavLink to={to} className={({ isActive }) => cn(
      'inline-flex items-center gap-1.5 text-[11px] text-interactive hover:text-[color:var(--color-race-secondary)] py-1 pr-3',
      isActive && 'text-foreground',
    )}>
      <Icon className="h-3 w-3" aria-hidden /> {label}
      {badge != null && badge > 0 && <span className="ml-1 text-[10px] text-muted-foreground">({badge})</span>}
    </NavLink>
  )
}

export function NavMenu() {
  const { gameId } = useCurrentGame()
  const badges = useNavBadges()
  if (!gameId) return null
  const g = (path: string) => `/games/${gameId}/${path}`

  return (
    <nav aria-label="Game navigation" className="flex flex-col h-full">
      <ul className="space-y-0.5 pt-2">
        <PrimaryItem to={g('base')} icon={LayersIcon} label="Base" />
        <PrimaryItem to={g('units')} icon={ShieldIcon} label="Units" />
        <PrimaryItem to={g('research')} icon={FlaskConicalIcon} label="Research" />
        <PrimaryItem to={g('market')} icon={ShoppingCartIcon} label="Market" />
        <PrimaryItem to={g('live')} icon={RadioIcon} label="Live View" />
        <PrimaryItem to={g('alliances')} icon={UsersIcon} label="Alliance" badge={badges.alliances} />
      </ul>
      <div className="mt-auto border-t border-border px-4 pt-3 pb-4 text-[11px] leading-relaxed">
        <div className="flex flex-wrap gap-x-3 gap-y-1">
          <UtilityLink to={g('ranking')} icon={BarChart2Icon} label="Ranking" />
          <UtilityLink to={g('messages')} icon={MailIcon} label="Messages" badge={badges.messages} />
          <UtilityLink to={g('chat')} icon={MessageSquareIcon} label="Chat" />
          <UtilityLink to={g('unitdefinitions')} icon={BookOpenIcon} label="Codex" />
          <UtilityLink to={g('profile')} icon={UserIcon} label="Profile" />
          <UtilityLink to={g('help')} icon={HelpCircleIcon} label="Help" />
        </div>
      </div>
    </nav>
  )
}
