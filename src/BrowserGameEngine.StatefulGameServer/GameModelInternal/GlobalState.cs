using BrowserGameEngine.GameModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class UserCurrencyState {
		public string UserId { get; init; } = "";
		public decimal Balance { get; private set; }
		public List<CurrencyTransactionImmutable> Transactions { get; } = new();
		private readonly object _lock = new();

		public void Credit(CurrencyTransactionImmutable tx) {
			lock (_lock) {
				Transactions.Add(tx);
				Balance += tx.Amount;
			}
		}

		public bool TryDebit(CurrencyTransactionImmutable tx) {
			lock (_lock) {
				if (Balance + tx.Amount < 0) return false;
				Transactions.Add(tx);
				Balance += tx.Amount;
				return true;
			}
		}

		public UserCurrencyImmutable ToImmutable() {
			lock (_lock) {
				return new UserCurrencyImmutable(UserId, Balance, Transactions.ToList());
			}
		}
	}

	public class GlobalState {
		internal ConcurrentDictionary<string, User> Users { get; set; } = new();
		internal ConcurrentDictionary<string, UserCurrencyState> CurrencyLedger { get; set; } = new();

		private readonly object _gamesLock = new();
		private List<GameRecordImmutable> _games = new();

		private readonly object _achievementsLock = new();
		private List<PlayerAchievementImmutable> _achievements = new();

		private readonly object _milestonesLock = new();
		private List<UserMilestoneImmutable> _milestones = new();

		private readonly object _tournamentsLock = new();
		private List<TournamentImmutable> _tournaments = new();

		private readonly object _ownedItemsLock = new();
		private List<ItemOwnershipImmutable> _ownedItems = new();

		private readonly object _currencyTradeOffersLock = new();
		private List<CurrencyTradeOfferImmutable> _currencyTradeOffers = new();

		public string? GetUserDisplayName(string userId) {
			var user = Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.DisplayName;
		}

		public System.DateTime? GetUserCreated(string userId) {
			var user = Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.Created;
		}

		public IReadOnlyList<GameRecordImmutable> GetGames() {
			lock (_gamesLock) return _games.ToList();
		}

		public void AddGame(GameRecordImmutable record) {
			lock (_gamesLock) _games.Add(record);
		}

		public void UpdateGame(GameRecordImmutable old, GameRecordImmutable updated) {
			lock (_gamesLock) {
				var idx = _games.IndexOf(old);
				if (idx >= 0) _games[idx] = updated;
			}
		}

		public void SetGames(IEnumerable<GameRecordImmutable> games) {
			lock (_gamesLock) _games = games.ToList();
		}

		public IReadOnlyList<PlayerAchievementImmutable> GetAchievements() {
			lock (_achievementsLock) return _achievements.ToList();
		}

		public void AddAchievement(PlayerAchievementImmutable achievement) {
			lock (_achievementsLock) _achievements.Add(achievement);
		}

		public void SetAchievements(IEnumerable<PlayerAchievementImmutable> achievements) {
			lock (_achievementsLock) _achievements = achievements.ToList();
		}

		public IReadOnlyList<UserMilestoneImmutable> GetAllMilestones() {
			lock (_milestonesLock) return _milestones.ToList();
		}

		public IReadOnlyList<UserMilestoneImmutable> GetMilestonesForUser(string userId) {
			lock (_milestonesLock) return _milestones.Where(m => m.UserId == userId).ToList();
		}

		public bool HasMilestone(string userId, string milestoneId) {
			lock (_milestonesLock) return _milestones.Any(m => m.UserId == userId && m.MilestoneId == milestoneId);
		}

		public void AddMilestone(UserMilestoneImmutable milestone) {
			lock (_milestonesLock) _milestones.Add(milestone);
		}

		public void SetMilestones(IEnumerable<UserMilestoneImmutable> milestones) {
			lock (_milestonesLock) _milestones = milestones.ToList();
		}

		public long GetUserTotalXp(string userId) {
			Users.TryGetValue(userId, out var user);
			return user?.TotalXp ?? 0;
		}

		public void AddXpToUser(string userId, long xp) {
			if (xp <= 0) return;
			// Silently skip if the user record doesn't exist (e.g. bot/guest players)
			if (!Users.ContainsKey(userId)) return;
			Users.AddOrUpdate(userId,
				addValueFactory: _ => throw new System.InvalidOperationException($"User {userId} not found when awarding XP"),
				updateValueFactory: (_, existing) => { existing.TotalXp += xp; return existing; });
		}

		public IReadOnlyList<TournamentImmutable> GetTournaments() {
			lock (_tournamentsLock) return _tournaments.ToList();
		}

		public TournamentImmutable? GetTournamentById(string tournamentId) {
			lock (_tournamentsLock) return _tournaments.FirstOrDefault(t => t.TournamentId == tournamentId);
		}

		public void AddTournament(TournamentImmutable tournament) {
			lock (_tournamentsLock) _tournaments.Add(tournament);
		}

		public void UpdateTournament(TournamentImmutable old, TournamentImmutable updated) {
			lock (_tournamentsLock) {
				var idx = _tournaments.IndexOf(old);
				if (idx >= 0) _tournaments[idx] = updated;
			}
		}

		public void SetTournaments(IEnumerable<TournamentImmutable> tournaments) {
			lock (_tournamentsLock) _tournaments = tournaments.ToList();
		}

		public IReadOnlyList<ItemOwnershipImmutable> GetOwnedItems() {
			lock (_ownedItemsLock) return _ownedItems.ToList();
		}

		public void AddOwnedItem(ItemOwnershipImmutable item) {
			lock (_ownedItemsLock) _ownedItems.Add(item);
		}

		public void SetOwnedItems(IEnumerable<ItemOwnershipImmutable> items) {
			lock (_ownedItemsLock) _ownedItems = items.ToList();
		}

		public IReadOnlyList<CurrencyTradeOfferImmutable> GetCurrencyTradeOffers() {
			lock (_currencyTradeOffersLock) return _currencyTradeOffers.ToList();
		}

		public void AddCurrencyTradeOffer(CurrencyTradeOfferImmutable offer) {
			lock (_currencyTradeOffersLock) _currencyTradeOffers.Add(offer);
		}

		public void UpdateCurrencyTradeOffer(CurrencyTradeOfferImmutable old, CurrencyTradeOfferImmutable updated) {
			lock (_currencyTradeOffersLock) {
				var idx = _currencyTradeOffers.IndexOf(old);
				if (idx >= 0) _currencyTradeOffers[idx] = updated;
			}
		}

		public void SetCurrencyTradeOffers(IEnumerable<CurrencyTradeOfferImmutable> offers) {
			lock (_currencyTradeOffersLock) _currencyTradeOffers = offers.ToList();
		}
	}

	public static class GlobalStateExtensions {
		public static GlobalStateImmutable ToImmutable(this GlobalState globalState) {
			return new GlobalStateImmutable(
				Users: globalState.Users.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				Games: globalState.GetGames().ToList(),
				Achievements: globalState.GetAchievements().ToList(),
				Milestones: globalState.GetAllMilestones().ToList(),
				Tournaments: globalState.GetTournaments().ToList(),
				CurrencyLedger: globalState.CurrencyLedger.Values.Select(s => s.ToImmutable()).ToList(),
				OwnedItems: globalState.GetOwnedItems().ToList(),
				CurrencyTradeOffers: globalState.GetCurrencyTradeOffers().ToList()
			);
		}

		public static GlobalState ToMutable(this GlobalStateImmutable globalStateImmutable) {
			var state = new GlobalState {
				Users = new ConcurrentDictionary<string, User>(
					globalStateImmutable.Users.ToDictionary(x => x.Key, y => y.Value.ToMutable()))
			};
			state.SetGames(globalStateImmutable.Games);
			state.SetAchievements(globalStateImmutable.Achievements);
			state.SetMilestones(globalStateImmutable.Milestones ?? Enumerable.Empty<UserMilestoneImmutable>());
			state.SetTournaments(globalStateImmutable.Tournaments ?? Enumerable.Empty<TournamentImmutable>());
			state.SetOwnedItems(globalStateImmutable.OwnedItems ?? Enumerable.Empty<ItemOwnershipImmutable>());
			state.SetCurrencyTradeOffers(globalStateImmutable.CurrencyTradeOffers ?? Enumerable.Empty<CurrencyTradeOfferImmutable>());
			foreach (var userCurrency in globalStateImmutable.CurrencyLedger ?? Enumerable.Empty<UserCurrencyImmutable>()) {
				var currencyState = new UserCurrencyState { UserId = userCurrency.UserId };
				foreach (var tx in userCurrency.Transactions) {
					currencyState.Credit(tx);
				}
				state.CurrencyLedger[userCurrency.UserId] = currencyState;
			}
			return state;
		}
	}
}
