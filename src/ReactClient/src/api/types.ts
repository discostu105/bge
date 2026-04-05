// TypeScript interfaces ported from BrowserGameEngine.Shared C# ViewModels

// Core

export interface CostViewModel {
  cost: Record<string, number>
}

export interface UnitDefinitionViewModel {
  id: string
  name: string
  playerTypeRestriction: string
  cost: CostViewModel
  attack: number
  defense: number
  hitpoints: number
  speed: number
  isMobile: boolean
  prerequisites: string[]
  prerequisitesMet: boolean
}

export interface AssetDefinitionViewModel {
  id: string
  name: string
  playerTypeRestriction: string
  cost: Record<string, number>
  attack: number
  defense: number
  hitpoints: number
  prerequisites: string[]
  buildTimeTicks: number
}

// Assets

export interface AssetViewModel {
  definition: AssetDefinitionViewModel
  level: number
  built: boolean
  ticksLeftForBuild: number
  prerequisites: string
  prerequisitesMet: boolean
  alreadyQueued: boolean
  cost: CostViewModel
  canAfford: boolean
  availableUnits: UnitDefinitionViewModel[]
}

export interface AssetsViewModel {
  assets: AssetViewModel[]
}

// Units

export interface UnitViewModel {
  unitId: string
  definition: UnitDefinitionViewModel
  count: number
  positionPlayerId: string | null
  positionPlayerName: string | null
}

export interface UnitsViewModel {
  units: UnitViewModel[]
}

// Build Queue

export interface BuildQueueEntryViewModel {
  id: string
  type: string
  defId: string
  name: string
  count: number
  priority: number
}

export interface BuildQueueViewModel {
  entries: BuildQueueEntryViewModel[]
}

export interface AddToQueueRequest {
  type: string
  defId: string
  count: number
}

export interface ReorderQueueRequest {
  entryId: string
  newPriority: number
}

// Resources

export interface PlayerResourcesViewModel {
  primaryResource: CostViewModel
  secondaryResources: CostViewModel
  colonizationCostPerLand: number
}

// Worker Assignment

export interface WorkerAssignmentViewModel {
  totalWorkers: number
  mineralWorkers: number
  gasWorkers: number
  idleWorkers: number
}

// Research / Upgrades

export interface UpgradesViewModel {
  attackUpgradeLevel: number
  defenseUpgradeLevel: number
  upgradeResearchTimer: number
  upgradeBeingResearched: string
  maxUpgradeLevel: number
  nextAttackUpgradeCost: CostViewModel | null
  nextDefenseUpgradeCost: CostViewModel | null
  playerType: string
}

export interface TechNodeViewModel {
  id: string
  name: string
  description: string
  tier: number
  cost: CostViewModel
  researchTimeTicks: number
  prerequisiteIds: string[]
  effectType: string
  effectValue: number
  status: 'Unlocked' | 'InProgress' | 'Available' | 'Locked'
}

export interface TechTreeViewModel {
  playerType: string
  currentResearchId: string | null
  researchTimerTicks: number
  nodes: TechNodeViewModel[]
}

// Market

export interface MarketOrderViewModel {
  orderId: string
  sellerPlayerId: string
  sellerPlayerName: string
  offeredResourceId: string
  offeredResourceName: string
  offeredAmount: number
  wantedResourceId: string
  wantedResourceName: string
  wantedAmount: number
  createdAt: string
  isOwnOrder: boolean
}

export interface ResourceOptionViewModel {
  id: string
  name: string
}

export interface MarketViewModel {
  openOrders: MarketOrderViewModel[]
  currentPlayerId: string
  resourceOptions: ResourceOptionViewModel[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateMarketOrderRequest {
  offeredResourceId: string
  offeredAmount: number
  wantedResourceId: string
  wantedAmount: number
}

// Trade (player-to-player)

export interface TradeOfferViewModel {
  offerId: string
  fromPlayerId: string
  fromPlayerName: string
  toPlayerId: string
  toPlayerName: string
  offeredAmount: number
  offeredResourceId: string
  wantedAmount: number
  wantedResourceId: string
  note: string | null
  sentAt: string
  status: string
}

export interface TradeHistoryItemViewModel {
  offerId: string
  withPlayerId: string
  withPlayerName: string
  gaveAmount: number
  gaveResourceId: string
  receivedAmount: number
  receivedResourceId: string
  completedAt: string
  status: string
}

export interface CreateTradeOfferRequest {
  targetPlayerId: string
  offeredResourceId: string
  offeredAmount: number
  wantedResourceId: string
  wantedAmount: number
  note: string | null
}

// Colonize

export interface TradeResourceRequest {
  fromResource: string | null
  amount: number
}

// Notifications

export type NotificationKind =
  | 'Info'
  | 'Warning'
  | 'GameEvent'
  | 'AttackReceived'
  | 'AllianceRequest'
  | 'MessageReceived'
  | 'SpyAttempted'

export interface PlayerNotificationViewModel {
  id: string
  message: string
  kind: NotificationKind
  createdAt: string
  isRead: boolean
}

// Game

export interface GameDetailViewModel {
  gameId: string
  name: string
  gameDefType: string
  status: string
  startTime: string
  endTime: string
  playerCount: number
  winnerId: string | null
  winnerName: string | null
  actualEndTime: string | null
  victoryConditionType: string | null
  victoryConditionLabel: string | null
}

export interface GameSummaryViewModel {
  gameId: string
  name: string
  gameDefType: string
  status: string
  playerCount: number
  maxPlayers: number
  startTime: string | null
  endTime: string | null
  canJoin: boolean
  winnerId: string | null
  winnerName: string | null
  isPlayerEnrolled: boolean
  victoryConditionType: string | null
  discordWebhookUrl: string | null
}

export interface GameListViewModel {
  games: GameSummaryViewModel[]
}

export interface MyGameViewModel {
  gameId: string
  gameName: string
  gameStatus: string
  playerId: string
  playerName: string
}

export interface GameResultEntryViewModel {
  rank: number
  playerName: string
  playerId: string
  score: number
  isWinner: boolean
}

export interface GameResultsViewModel {
  gameId: string
  name: string
  startTime: string
  actualEndTime: string | null
  endTime: string
  standings: GameResultEntryViewModel[]
  currentPlayerId: string | null
  victoryConditionType: string | null
  victoryConditionLabel: string | null
}

export interface JoinGameRequest {
  playerName: string
}

export interface JoinGameViewModel {
  playerId: string
}

export interface CreateGameRequest {
  name: string
  gameDefType: string
  startTime: string
  endTime: string
  tickDuration: string
  discordWebhookUrl: string | null
}

// Players / Profile

export interface PlayerProfileViewModel {
  playerId: string | null
  playerName: string | null
  score: number
  protectionTicksRemaining: number
  isOnline: boolean
  lastOnline: string | null
  tutorialCompleted: boolean
}

export interface PublicPlayerViewModel {
  playerId: string | null
  playerName: string | null
  score: number
  protectionTicksRemaining: number
  userDisplayName: string | null
  isAgent: boolean
  lastOnline: string | null
  isOnline: boolean
  approxMinerals: number | null
  approxGas: number | null
  approxHomeUnitCount: number | null
  isCurrentPlayer: boolean
}

export interface InGamePlayerProfileViewModel {
  playerId: string
  playerName: string
  score: number
  rank: number
  totalPlayers: number
  allianceId: string | null
  allianceName: string | null
  allianceRole: string | null
  techsResearched: number
  isOnline: boolean
  lastOnline: string | null
  isAgent: boolean
  protectionTicksRemaining: number
}

// Rankings

export interface PlayerRankingEntryViewModel {
  rank: number
  playerId: string
  playerName: string
  score: number
}

export interface PlayerRankingViewModel {
  entries: PlayerRankingEntryViewModel[]
}

export interface LeaderboardEntryViewModel {
  rank: number
  playerId: string
  playerName: string
  score: number
  isCurrentPlayer: boolean
}

// Alliance

export interface AllianceMemberViewModel {
  playerId: string
  playerName: string
  isPending: boolean
  joinedAt: string
  voteCount: number
  isLeader: boolean
  votedForPlayerId?: string | null
}

export interface AllianceViewModel {
  allianceId: string
  name: string
  message: string | null
  memberCount: number
  created: string
  isAtWar: boolean
}

export interface AllianceDetailViewModel {
  allianceId: string
  name: string
  message: string | null
  created: string
  leaderId: string
  members: AllianceMemberViewModel[]
}

export interface MyAllianceStatusViewModel {
  allianceId: string | null
  allianceName: string | null
  isMember: boolean
  isPending: boolean
  isLeader: boolean
}

export interface AllianceRankingEntryViewModel {
  rank: number
  allianceId: string
  allianceName: string
  memberCount: number
  totalScore: number
}

export interface AllianceRankingViewModel {
  entries: AllianceRankingEntryViewModel[]
}

export interface AllianceInviteViewModel {
  inviteId: string
  allianceId: string
  allianceName: string
  inviterPlayerName: string
  expiresAt: string
}

export interface AllianceWarViewModel {
  warId: string
  attackerAllianceId: string
  attackerAllianceName: string
  defenderAllianceId: string
  defenderAllianceName: string
  status: string
  declaredAt: string
  proposerAllianceId: string | null
}

export interface AllianceChatPostViewModel {
  postId: string
  authorPlayerId: string
  authorName: string
  playerType: string
  body: string
  createdAt: string
}

export interface CreateAllianceRequest {
  allianceName: string
  password: string
}

export interface JoinAllianceRequest {
  password: string
}

export interface VoteLeaderRequest {
  voteePlayerId: string
}

export interface SetAlliancePasswordRequest {
  newPassword: string
}

export interface SetAllianceMessageRequest {
  message: string
}

export interface PostAllianceChatRequest {
  body: string
}

export interface InvitePlayerRequest {
  targetPlayerId: string
}

export interface DeclareWarRequest {
  targetAllianceId: string
}

export interface AcceptInviteRequest {
  inviteId: string
}

export interface PeaceRequest {
  warId: string
}

// Diplomacy

export type DiplomacyProposalType = 'Nap' | 'ResourceAgreement'

export interface DiplomacyProposalViewModel {
  proposalId: string
  type: DiplomacyProposalType
  proposerPlayerId: string
  proposerPlayerName: string
  targetPlayerId: string
  targetPlayerName: string
  durationTicks: number
  mineralsPerTick: number
  gasPerTick: number
  proposedAt: string
}

export interface ActiveNapViewModel {
  napId: string
  partnerPlayerId: string
  partnerPlayerName: string
  ticksRemaining: number
}

export interface ActiveResourceAgreementViewModel {
  agreementId: string
  partnerPlayerId: string
  partnerPlayerName: string
  mineralsPerTick: number
  gasPerTick: number
  ticksRemaining: number
}

export interface DiplomacyStatusViewModel {
  pendingIncoming: DiplomacyProposalViewModel[]
  pendingSent: DiplomacyProposalViewModel[]
  activeNaps: ActiveNapViewModel[]
  activeResourceAgreements: ActiveResourceAgreementViewModel[]
}

export interface ProposeNapRequest {
  targetPlayerId: string
  durationTicks: number
}

export interface ProposeResourceAgreementRequest {
  targetPlayerId: string
  durationTicks: number
  mineralsPerTick: number
  gasPerTick: number
}

export interface RespondToProposalRequest {
  accept: boolean
}

// Spy

export interface SpyPlayerEntryViewModel {
  playerId: string
  playerName: string
  score: number
  cooldownExpiresAt: string | null
}

export interface SpyMissionViewModel {
  id: string
  targetPlayerId: string
  targetPlayerName: string
  missionType: string
  status: string
  createdAt: string
  resolvedAt: string | null
  result: string | null
}

export interface UnitEstimateViewModel {
  unitDefId: string
  unitTypeName: string
  approximateCount: number
}

export interface SpyReportViewModel {
  targetPlayerId: string
  targetPlayerName: string
  approximateMinerals: number
  approximateGas: number
  unitEstimates: UnitEstimateViewModel[]
  reportTime: string
  cooldownExpiresAt: string
}

export interface SpyAttemptViewModel {
  id: string
  attackerName: string
  actionType: string
  timestamp: string
}

export interface SendSpyMissionRequest {
  targetPlayerId: string
  missionType: string
}

export interface SendSpyMissionResponse {
  missionId: string
  estimatedResolveAt: string
}

// Enemy Base / Battle

export interface UnitLossViewModel {
  unitName: string
  count: number
}

export interface BattleResultViewModel {
  attackerId: string | null
  attackerName: string | null
  defenderId: string | null
  defenderName: string | null
  outcome: string | null
  totalAttackerStrengthBefore: number
  totalDefenderStrengthBefore: number
  unitsLostByAttacker: UnitLossViewModel[]
  unitsLostByDefender: UnitLossViewModel[]
  resourcesPillaged: Record<string, number>
  landTransferred: number
  workersCaptured: number
}

export interface EnemyBaseViewModel {
  playerAttackingUnits: UnitsViewModel
  enemyDefendingUnits: UnitsViewModel
  spyCostLabel: string
}

export interface SelectEnemyViewModel {
  attackablePlayers: PublicPlayerViewModel[]
}

// Chat

export interface ChatMessageViewModel {
  messageId: string
  authorPlayerId: string
  authorName: string
  playerType: string
  body: string
  createdAt: string
}

export interface ChatMessagesViewModel {
  messages: ChatMessageViewModel[]
}

export interface PostChatMessageRequest {
  body: string
}

// Messages

export interface MessageViewModel {
  messageId: string
  senderId: string | null
  senderName: string
  recipientId: string
  recipientName: string
  subject: string
  body: string
  isRead: boolean
  sentAt: string
}

export interface MessageInboxViewModel {
  messages: MessageViewModel[]
}

export interface MessageThreadViewModel {
  withPlayerId: string
  withPlayerName: string
  messages: MessageViewModel[]
}

export interface SendMessageViewModel {
  recipientId: string
  subject: string
  body: string
}

// Achievements (tech/in-game)

export interface AchievementViewModel {
  id: string
  name: string
  description: string
  unlockedAt: string | null
  isUnlocked: boolean
}

export interface AchievementsViewModel {
  achievements: AchievementViewModel[]
}

// Milestone achievements (progress-based, unlockable)

export type MilestoneCategory = 'combat' | 'economy' | 'diplomacy' | 'exploration'
export type MilestoneTier = 'bronze' | 'silver' | 'gold' | 'legendary'

export interface MilestoneAchievementViewModel {
  id: string
  name: string
  description: string
  category: MilestoneCategory
  icon: string
  isUnlocked: boolean
  unlockedAt: string | null
  currentProgress: number
  targetProgress: number
  tier: MilestoneTier
}

export interface MilestoneAchievementsSummaryViewModel {
  totalAchievements: number
  unlockedCount: number
  unlockedByCategory: Record<string, number>
}

export interface MilestoneAchievementsViewModel {
  achievements: MilestoneAchievementViewModel[]
  summary: MilestoneAchievementsSummaryViewModel
}

// Player game-completion achievements (api/player-management/me/achievements)

export interface PlayerAchievementViewModel {
  achievementType: string
  achievementLabel: string
  achievementIcon: string
  gameId: string
  gameName: string
  gameDefType: string
  finalRank: number
  score: number
  earnedAt: string
}

export interface PlayerAchievementsViewModel {
  achievements: PlayerAchievementViewModel[]
}

// Player History (api/history)

export interface PlayerGameHistoryEntryViewModel {
  gameId: string
  gameName: string
  gameDefType: string
  startTime: string
  endTime: string
  finishedAt: string
  finalRank: number
  finalScore: number
  playersInGame: number
  isWin: boolean
}

export interface PlayerHistoryViewModel {
  totalGames: number
  totalWins: number
  bestRank: number
  totalScore: number
  games: PlayerGameHistoryEntryViewModel[]
}

// Update Game Request

export interface UpdateGameRequest {
  name: string
  endTime: string
  discordWebhookUrl: string | null
}

// Player List (all-time)

export interface AllTimePlayerEntryViewModel {
  displayName: string
  totalGames: number
  totalWins: number
  bestRank: number
  totalScore: number
}

export interface AllTimePlayerListViewModel {
  players: AllTimePlayerEntryViewModel[]
}

// Create Player

export interface CreatePlayerRequest {
  playerName: string
  playerType: string
}

// Unit Definitions

export interface UnitDefinitionsViewModel {
  unitDefinitions: UnitDefinitionViewModel[]
}

// Lobby

export interface GameLobbyViewModel {
  gameId: string
  gameName: string
  players: LobbyPlayerViewModel[]
  startTime: string | null
  status: string
}

export interface LobbyPlayerViewModel {
  playerId: string
  playerName: string
  playerType: string
  isReady: boolean
}

// Tick info

export interface TickInfoViewModel {
  currentTick: number
  tickDuration: string
  lastTickAt: string
}

// Version

export interface VersionInfoViewModel {
  version: string
  commitHash: string | null
}

// User Preferences

export interface UserPreferencesViewModel {
  theme: string
}

// Profile

export interface ProfileViewModel {
  playerName: string | null
  displayName: string | null
  avatarUrl: string | null
  score: number
  land: number
  minerals: number
  gas: number
  armySize: number
  rank: number
  totalPlayers: number
  gamesPlayed: number
  wins: number
  bestRank: number
  currentGameId: string | null
}

// Public cross-game stats

export interface PlayerCrossGameEntry {
  gameId: string
  gameName: string
  gameStatus: string
  gameEndTime: string
  finalRank: number
  finalScore: number
  isWinner: boolean
}

export interface PlayerCrossGameStatsViewModel {
  userId: string
  playerName: string | null
  totalGames: number
  totalWins: number
  bestRank: number
  totalScore: number
  games: PlayerCrossGameEntry[]
}

// Pagination

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
