using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/profile")]
	public class ProfileController : ControllerBase {
		private readonly ILogger<ProfileController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly ScoreRepository scoreRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly UnitRepository unitRepository;
		private readonly GameRegistry gameRegistry;
		private readonly GlobalState globalState;

		public ProfileController(
			ILogger<ProfileController> logger,
			CurrentUserContext currentUserContext,
			PlayerRepository playerRepository,
			UserRepository userRepository,
			ScoreRepository scoreRepository,
			ResourceRepository resourceRepository,
			UnitRepository unitRepository,
			GameRegistry gameRegistry,
			GlobalState globalState
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.scoreRepository = scoreRepository;
			this.resourceRepository = resourceRepository;
			this.unitRepository = unitRepository;
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
		}

		/// <summary>Returns the authenticated user's full profile: display name, avatar, current game stats, and game history stubs.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(ProfileViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<ProfileViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();

			var playerId = currentUserContext.PlayerId!;
			var player = playerRepository.Get(playerId);

			var user = currentUserContext.UserId != null
				? userRepository.GetByUserId(currentUserContext.UserId)
				: null;

			var avatarUrl = user?.GithubLogin != null
				? $"https://github.com/{user.GithubLogin}.png"
				: null;

			var land = resourceRepository.GetAmount(playerId, Id.ResDef("land"));
			var minerals = resourceRepository.GetAmount(playerId, Id.ResDef("minerals"));
			var gas = resourceRepository.GetAmount(playerId, Id.ResDef("gas"));

			var armySize = unitRepository.GetAll(playerId).Sum(u => u.Count);

			var allPlayers = playerRepository.GetAll().ToList();
			var ranked = allPlayers
				.OrderByDescending(p => scoreRepository.GetScore(p.PlayerId))
				.ToList();
			var rank = ranked.FindIndex(p => p.PlayerId == playerId) + 1;

			var currentInstance = gameRegistry.GetAllInstances()
				.FirstOrDefault(i => i.HasPlayer(playerId));
			var currentGameId = currentInstance?.Record.GameId.Id;

			var userAchievements = currentUserContext.UserId != null
				? globalState.GetAchievements().Where(a => a.UserId == currentUserContext.UserId).ToList()
				: new System.Collections.Generic.List<PlayerAchievementImmutable>();
			var gamesPlayed = userAchievements.Count;
			var wins = userAchievements.Count(a => a.FinalRank == 1);
			var bestRank = gamesPlayed > 0 ? userAchievements.Min(a => a.FinalRank) : 0;

			return new ProfileViewModel {
				PlayerName = player.Name,
				DisplayName = user?.DisplayName,
				AvatarUrl = avatarUrl,
				Score = scoreRepository.GetScore(playerId),
				Land = land,
				Minerals = minerals,
				Gas = gas,
				ArmySize = armySize,
				Rank = rank,
				TotalPlayers = allPlayers.Count,
				GamesPlayed = gamesPlayed,
				Wins = wins,
				BestRank = bestRank,
				CurrentGameId = currentGameId,
				JoinedAt = currentUserContext.UserId != null
					? globalState.GetUserCreated(currentUserContext.UserId)
					: null
			};
		}
	}
}
