using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/tournaments")]
	public class TournamentController : ControllerBase {
		private readonly GlobalState globalState;
		private readonly GameRegistry gameRegistry;

		public TournamentController(GlobalState globalState, GameRegistry gameRegistry) {
			this.globalState = globalState;
			this.gameRegistry = gameRegistry;
		}

		/// <summary>Returns aggregated tournament standings ranked by total score.</summary>
		/// <param name="tournamentId">The tournament identifier.</param>
		[AllowAnonymous]
		[HttpGet("{tournamentId}/results")]
		[ProducesResponseType(typeof(TournamentResultsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<TournamentResultsViewModel> GetResults(string tournamentId) {
			var games = globalState.GetGames()
				.Where(g => g.TournamentId == tournamentId)
				.ToList();

			if (games.Count == 0) return NotFound();

			var gameIds = new HashSet<string>(games.Select(g => g.GameId.Id));
			var achievements = globalState.GetAchievements()
				.Where(a => gameIds.Contains(a.GameId.Id))
				.ToList();

			var rankings = achievements
				.GroupBy(a => a.UserId ?? a.PlayerId.Id)
				.Select(group => {
					var first = group.First();
					return new TournamentPlayerResultViewModel(
						Rank: 0,
						UserId: first.UserId,
						PlayerName: first.PlayerName,
						GamesPlayed: group.Count(),
						Wins: group.Count(a => a.FinalRank == 1),
						TotalScore: group.Sum(a => a.FinalScore)
					);
				})
				.OrderByDescending(r => r.TotalScore)
				.ThenBy(r => r.GamesPlayed)
				.Select((r, idx) => r with { Rank = idx + 1 })
				.ToList();

			return Ok(new TournamentResultsViewModel(
				TournamentId: tournamentId,
				TotalGames: games.Count,
				Rankings: rankings
			));
		}

		/// <summary>Returns all games in the tournament.</summary>
		/// <param name="tournamentId">The tournament identifier.</param>
		[AllowAnonymous]
		[HttpGet("{tournamentId}/games")]
		[ProducesResponseType(typeof(GameListViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<GameListViewModel> GetGames(string tournamentId) {
			var games = globalState.GetGames()
				.Where(g => g.TournamentId == tournamentId)
				.ToList();

			if (games.Count == 0) return NotFound();

			var summaries = games.Select(record => {
				var instance = gameRegistry.TryGetInstance(record.GameId);
				var playerCount = instance?.PlayerCount ?? 0;
				bool canJoin = (record.Status == GameStatus.Upcoming || record.Status == GameStatus.Active)
					&& (record.MaxPlayers == 0 || playerCount < record.MaxPlayers);

				string? winnerName = record.WinnerId != null
					? globalState.GetAchievements()
						.FirstOrDefault(a => a.GameId == record.GameId && a.PlayerId == record.WinnerId)
						?.PlayerName
					: null;

				return new GameSummaryViewModel(
					GameId: record.GameId.Id,
					Name: record.Name,
					GameDefType: record.GameDefType,
					Status: record.Status.ToString(),
					PlayerCount: playerCount,
					MaxPlayers: record.MaxPlayers,
					StartTime: record.StartTime,
					EndTime: record.EndTime,
					CanJoin: canJoin,
					WinnerId: record.WinnerId?.Id,
					WinnerName: winnerName,
					IsPlayerEnrolled: false,
					VictoryConditionType: record.VictoryConditionType,
					DiscordWebhookUrl: record.DiscordWebhookUrl,
					CreatedByUserId: record.CreatedByUserId,
					TournamentId: record.TournamentId
				);
			}).ToList();

			return Ok(new GameListViewModel(summaries));
		}
	}
}
