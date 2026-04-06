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

// Resources

export interface PlayerResourcesViewModel {
  primaryResource: CostViewModel
  secondaryResources: CostViewModel
  colonizationCostPerLand: number
}

// Resource History

export interface ResourceSnapshotViewModel {
  tick: number
  minerals: number
  gas: number
  land: number
}

export interface ResourceHistoryViewModel {
  snapshots: ResourceSnapshotViewModel[]
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
  tournamentId: string | null
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
  createdByUserId: string | null
  tournamentId: string | null
}

export interface GameListViewModel {
  games: GameSummaryViewModel[]
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
  tournamentId: string | null
}

export interface JoinGameRequest {
  playerName: string
  playerType?: string
}

export interface GameSettingsViewModel {
  startingLand: number
  startingMinerals: number
  startingGas: number
  protectionTicks: number
  victoryThreshold: number
  victoryConditionType: string
  maxPlayers: number
}

export interface GameSettingsRequest {
  startingLand?: number | null
  startingMinerals?: number | null
  startingGas?: number | null
  protectionTicks?: number | null
  victoryThreshold?: number | null
  victoryConditionType?: string | null
  maxPlayers?: number | null
}

export interface CreateGameRequest {
  name: string
  gameDefType: string
  startTime: string
  endTime: string
  tickDuration: string
  discordWebhookUrl: string | null
  maxPlayers: number
  settings?: GameSettingsRequest | null
  tournamentId?: string | null
}

export interface CreateTournamentRequest {
  name: string
  format: string
  registrationDeadline: string
  maxPlayers: number
  gameDefType: string
  tickDuration: string
  matchDurationHours: number
}

export interface TournamentSummaryViewModel {
  tournamentId: string
  name: string
  format: string
  status: string
  registrationDeadline: string
  maxPlayers: number
  registrationCount: number
}

export interface TournamentRegistrationViewModel {
  userId: string
  displayName: string
  registeredAt: string
}

export interface PlayerRefViewModel {
  userId: string
  displayName: string
}

export interface MatchViewModel {
  matchId: string
  round: number
  matchNumber: number
  player1: PlayerRefViewModel | null
  player2: PlayerRefViewModel | null
  winnerId: string | null
  gameId: string | null
  status: string
}

export interface RoundViewModel {
  round: number
  matches: MatchViewModel[]
}

export interface TournamentBracketViewModel {
  tournamentId: string
  name: string
  format: string
  status: string
  rounds: RoundViewModel[]
}

export interface TournamentDetailViewModel {
  tournamentId: string
  name: string
  format: string
  status: string
  registrationDeadline: string
  maxPlayers: number
  registrations: TournamentRegistrationViewModel[]
  isRegistered: boolean
  isCreator: boolean
  bracket: TournamentBracketViewModel | null
}

export interface TournamentPlayerResultViewModel {
  rank: number
  userId: string | null
  playerName: string
  gamesPlayed: number
  wins: number
  totalScore: number
}

export interface TournamentResultsViewModel {
  tournamentId: string
  totalGames: number
  rankings: TournamentPlayerResultViewModel[]
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

export interface LeaderboardEntryViewModel {
  rank: number
  playerId: string
  playerName: string
  score: number
  isCurrentPlayer: boolean
  level: number
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

export interface AllianceInviteViewModel {
  inviteId: string
  allianceId: string
  allianceName: string
  inviterPlayerName: string
  expiresAt: string
}

export interface ElectionCandidateViewModel {
  playerId: string
  playerName: string
  nominatedAt: string
  voteCount: number
}

export interface AllianceElectionViewModel {
  electionId: string
  allianceId: string
  status: string
  startedByPlayerName: string
  startedAt: string
  nominationEndsAt: string
  votingEndsAt: string
  candidates: ElectionCandidateViewModel[]
  myVote: string | null
  winnerId: string | null
  winnerName: string | null
  completedAt: string | null
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

export interface PostAllianceChatRequest {
  body: string
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

// Battle Reports

export interface UnitCountViewModel {
  unitName: string
  count: number
}

export interface BattleRoundViewModel {
  roundNumber: number
  attackerUnitsRemaining: UnitCountViewModel[]
  defenderUnitsRemaining: UnitCountViewModel[]
  attackerCasualties: UnitCountViewModel[]
  defenderCasualties: UnitCountViewModel[]
}

export interface BattleReportSummaryViewModel {
  id: string
  opponentName: string
  outcome: string
  createdAt: string
}

export interface BattleReportDetailViewModel {
  id: string
  attackerId: string
  attackerName: string
  defenderId: string
  defenderName: string
  attackerRace: string
  defenderRace: string
  outcome: string
  totalAttackerStrengthBefore: number
  totalDefenderStrengthBefore: number
  attackerUnitsInitial: UnitCountViewModel[]
  defenderUnitsInitial: UnitCountViewModel[]
  rounds: BattleRoundViewModel[]
  landTransferred: number
  workersCaptured: number
  resourcesStolen: Record<string, number>
  createdAt: string
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

export type MilestoneCategory = 'combat' | 'economy' | 'diplomacy' | 'exploration' | 'progression'
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

// Player Stats (api/stats)

export interface PlayerStatsGameEntry {
  gameId: string
  gameName: string
  endTime: string
  finalRank: number
  playersInGame: number
  finalScore: number
  isWin: boolean
  durationMs: number | null
}

export interface PlayerStatsViewModel {
  totalGamesPlayed: number
  totalWins: number
  winRate: number
  bestRank: number
  avgFinalRank: number
  totalScore: number
  avgScorePerGame: number
  avgGameDurationMs: number | null
  games: PlayerStatsGameEntry[]
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
  userId: string
  totalGames: number
  totalWins: number
  bestRank: number
  totalScore: number
  totalXp: number
  level: number
}

export interface AllTimePlayerListViewModel {
  players: AllTimePlayerEntryViewModel[]
}

// Create Player

export interface CreatePlayerRequest {
  playerName: string
  playerType: string
}

// Lobby

export interface GameLobbyViewModel {
  gameId: string
  gameName: string
  status: string
  maxPlayers: number
  startTime: string | null
  endTime: string | null
  players: LobbyPlayerViewModel[]
  canJoin: boolean
  settings?: GameSettingsViewModel | null
}

export interface LobbyPlayerViewModel {
  playerId: string
  playerName: string
  playerType: string
  joined: string
}

export interface RaceViewModel {
  id: string
  name: string
}

export interface RaceListViewModel {
  races: RaceViewModel[]
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
  joinedAt?: string | null
  totalXp: number
  level: number
  levelProgress: number
  xpToNextLevel: number
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
  gameDefType?: string
}

export interface PlayerCrossGameStatsViewModel {
  userId: string
  playerName: string | null
  totalGames: number
  totalWins: number
  bestRank: number
  totalScore: number
  games: PlayerCrossGameEntry[]
  joinedAt?: string | null
  totalResourcesGathered?: number | null
  totalXp: number
  level: number
}

// Public achievements (api/players/{userId}/achievements)

export interface PublicAchievementEntry {
  achievementType: string
  achievementLabel: string
  achievementIcon: string
  gameId: string
  gameName: string
  earnedAt: string
}

export interface PublicPlayerAchievementsViewModel {
  achievements: PublicAchievementEntry[]
}

// Global Leaderboard

export interface GlobalLeaderboardEntryViewModel {
  rank: number
  userId: string
  displayName: string
  score: number
  tournamentWins: number
  gameWins: number
  achievementsUnlocked: number
  isCurrentPlayer: boolean
  level: number
}

export interface GlobalLeaderboardViewModel {
  entries: GlobalLeaderboardEntryViewModel[]
  seasonStart: string
  seasonEnd: string
}

export interface PlayerLeaderboardContextViewModel {
  rank: number
  nearbyEntries: GlobalLeaderboardEntryViewModel[]
}

// Pagination

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Replay Viewer

export interface ReplayPlayerViewModel {
  playerId: string
  playerName: string
  race: string
  finalRank: number
  finalScore: number
}

export interface ReplayBattleEventViewModel {
  reportId: string
  occurredAt: string
  attackerName: string
  defenderName: string
  outcome: string
  isCurrentPlayerAttacker: boolean
  isCurrentPlayerDefender: boolean
}

export interface GameReplayViewModel {
  gameId: string
  gameName: string
  gameDefType: string
  startTime: string
  actualEndTime: string | null
  status: string
  finalStandings: ReplayPlayerViewModel[]
  battleEvents: ReplayBattleEventViewModel[]
}

export interface SpectatorPlayerEntryViewModel {
  rank: number
  playerId: string
  playerName: string
  score: number
  isOnline: boolean
  isAgent: boolean
}

export interface SpectatorSnapshotViewModel {
  gameId: string
  gameName: string
  gameStatus: string
  topPlayers: SpectatorPlayerEntryViewModel[]
  tick: number
}

// Economy & Shop types

export interface CurrencyBalanceViewModel {
  balance: number
  currencyName: string
}

export interface CurrencyTransactionViewModel {
  transactionId: string
  amount: number
  type: string
  description: string
  createdAt: string
  relatedEntityId: string | null
}

export interface TransactionHistoryViewModel {
  balance: number
  totalCount: number
  transactions: CurrencyTransactionViewModel[]
}

export interface ShopItemViewModel {
  itemId: string
  name: string
  description: string
  category: string
  price: number
  isOwned: boolean
}

export interface ShopViewModel {
  items: ShopItemViewModel[]
}

export interface PurchaseItemRequest {
  itemId: string
  idempotencyKey: string
}

export interface OwnedItemViewModel {
  ownershipId: string
  itemId: string
  name: string
  description: string
  purchasedAt: string
}

export interface CurrencyTradeOfferViewModel {
  offerId: string
  fromUserId: string
  fromDisplayName: string | null
  offeredAmount: number
  wantedItemId: string | null
  wantedItemName: string | null
  wantedCurrencyAmount: number | null
  createdAt: string
  status: string
}

export interface CreateCurrencyTradeOfferRequest {
  toUserId: string
  offeredAmount: number
  wantedItemId: string | null
  wantedCurrencyAmount: number | null
}

