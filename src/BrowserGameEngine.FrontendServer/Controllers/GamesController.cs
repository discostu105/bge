using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/games")]
	public class GamesController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly GameRepository gameRepository;

		public GamesController(
			CurrentUserContext currentUserContext,
			GameRepository gameRepository
		) {
			this.currentUserContext = currentUserContext;
			this.gameRepository = gameRepository;
		}

		[HttpGet]
		public ActionResult<GameListViewModel> GetAll() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var games = gameRepository.GetAll().Select(g => new GameSummaryViewModel(
				g.GameId,
				g.Name,
				g.GameDefType,
				g.Status.ToString().ToLowerInvariant(),
				g.PlayerCount,
				g.MaxPlayers,
				g.StartTime,
				g.EndTime,
				g.Status == GameStatus.Upcoming
			)).ToList();
			return Ok(new GameListViewModel(games));
		}

		[HttpPost("{gameId}/join")]
		public ActionResult Join(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var game = gameRepository.Get(gameId);
			if (game == null) return NotFound();
			if (game.Status != GameStatus.Upcoming) return BadRequest("This game is not open for joining.");
			if (!gameRepository.AddPlayer(gameId, currentUserContext.PlayerId!.Id)) {
				return Conflict("You have already joined this game.");
			}
			return Ok();
		}
	}
}
