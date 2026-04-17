import { NavLink } from 'react-router'
import {
  LayersIcon, ShieldIcon, FlaskConicalIcon, ShoppingCartIcon, ArrowRightLeftIcon,
  BarChart2Icon, FlagIcon, UsersIcon, MessageSquareIcon, MailIcon, TrophyIcon,
  BookOpenIcon, HistoryIcon, HelpCircleIcon, RadioIcon, LineChartIcon, UserIcon,
  StoreIcon, CoinsIcon,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useCurrentGame } from '@/contexts/CurrentGameContext'
import { useNavBadges } from '@/hooks/useNavBadges'
import { CurrencyBadge } from '@/components/CurrencyBadge'
import { Badge } from '@/components/ui/badge'

function NavItem({
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
        className={({ isActive }) =>
          cn(
            'relative flex items-center gap-2 pl-4 pr-3 py-2.5 rounded-r-md text-sm transition-colors min-h-[44px]',
            isActive
              ? 'text-foreground font-medium bg-primary/5 before:absolute before:left-0 before:top-1.5 before:bottom-1.5 before:w-[2px] before:bg-primary before:rounded-sm'
              : 'text-muted-foreground hover:text-foreground hover:bg-secondary',
          )
        }
      >
        <Icon className="h-4 w-4 shrink-0" aria-hidden />
        <span className="flex-1 truncate">{label}</span>
        {badge != null && badge > 0 && (
          <Badge variant="secondary" className="h-5 px-1.5 text-[10px] tabular-nums">{badge}</Badge>
        )}
      </NavLink>
    </li>
  )
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <li className="px-4 pt-4 pb-1">
      <span className="label">{children}</span>
    </li>
  )
}

export function NavMenu() {
  const { gameId } = useCurrentGame()
  const badges = useNavBadges()
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

        <SectionLabel>Info</SectionLabel>
        <NavItem to={g('live')} icon={RadioIcon} label="Live View" />
        <NavItem to={g('unitdefinitions')} icon={BookOpenIcon} label="Unit Types" />
        <NavItem to={g('ranking')} icon={BarChart2Icon} label="Ranking" />
        <NavItem to={g('help')} icon={HelpCircleIcon} label="Help" />

        <SectionLabel>Social</SectionLabel>
        <NavItem to={g('diplomacy')} icon={FlagIcon} label="Diplomacy" badge={badges.diplomacy} />
        <NavItem to={g('alliances')} icon={UsersIcon} label="Alliances" badge={badges.alliances} />
        <NavItem to={g('allianceranking')} icon={TrophyIcon} label="Alliance Ranking" />
        <NavItem to={g('chat')} icon={MessageSquareIcon} label="Chat" />
        <NavItem to={g('messages')} icon={MailIcon} label="Messages" badge={badges.messages} />

        <SectionLabel>Account</SectionLabel>
        <NavItem to={g('history')} icon={HistoryIcon} label="History" />
        <NavItem to={g('stats')} icon={LineChartIcon} label="My Stats" />
        <NavItem to={g('profile')} icon={UserIcon} label="Profile" />

        <SectionLabel>Economy</SectionLabel>
        <NavItem to={g('shop')} icon={StoreIcon} label="Shop" />
        <NavItem to={g('economy')} icon={CoinsIcon} label="My Economy" />
      </ul>

      <div className="mt-4 px-4">
        <CurrencyBadge />
      </div>
    </nav>
  )
}
