using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
			var games = gameRepository.GetAll().Select(g => new GameSummaryViewModel {
				GameId = g.GameId,
				Name = g.Name,
				Status = g.Status.ToString().ToLowerInvariant(),
				PlayerCount = g.PlayerCount,
				MaxPlayers = g.MaxPlayers,
				StartTime = g.StartTime,
				CanJoin = g.Status == GameStatus.Upcoming
			}).ToList();
			return Ok(new GameListViewModel { Games = games });
		}

		[HttpPost("{gameId}/join")]
		public ActionResult Join(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var game = gameRepository.Get(gameId);
			if (game == null) return NotFound();
			if (game.Status != GameStatus.Upcoming) return BadRequest("This game is not open for joining.");
			gameRepository.AddPlayer(gameId, currentUserContext.PlayerId!.Id);
			return Ok();
		}
	}
}
