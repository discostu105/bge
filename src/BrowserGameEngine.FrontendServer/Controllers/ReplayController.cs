using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/replay")]
	public class ReplayController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly GameReplayRepository gameReplayRepository;

		public ReplayController(CurrentUserContext currentUserContext, GameReplayRepository gameReplayRepository) {
			this.currentUserContext = currentUserContext;
			this.gameReplayRepository = gameReplayRepository;
		}

		/// <summary>Returns the replay data for a specific game, including final standings and battle events for the current player.</summary>
		/// <param name="gameId">The game identifier.</param>
		[HttpGet("{gameId}")]
		[ProducesResponseType(typeof(GameReplayViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetReplay(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var data = await gameReplayRepository.GetGameReplayData(new GameId(gameId), currentUserContext.UserId!);
			if (data?.Record == null) return NotFound();

			var raceMap = data.WorldState?.Players
				.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value.PlayerType.Id)
				?? [];

			var finalStandings = data.GameAchievements
				.OrderBy(a => a.FinalRank)
				.Select(a => new ReplayPlayerViewModel(
					PlayerId: a.PlayerId.Id,
					PlayerName: a.PlayerName,
					Race: raceMap.GetValueOrDefault(a.PlayerId.Id, a.GameDefType),
					FinalRank: a.FinalRank,
					FinalScore: a.FinalScore
				))
				.ToList();

			var battleEvents = new List<ReplayBattleEventViewModel>();
			if (data.CurrentPlayerAchievement != null && data.WorldState != null) {
				var currentPlayerId = data.CurrentPlayerAchievement.PlayerId;
				if (data.WorldState.Players.TryGetValue(currentPlayerId, out var currentPlayer)) {
					battleEvents = (currentPlayer.State.BattleReports ?? [])
						.OrderBy(r => r.CreatedAt)
						.Select(r => new ReplayBattleEventViewModel(
							ReportId: r.Id,
							OccurredAt: r.CreatedAt,
							AttackerName: r.AttackerName,
							DefenderName: r.DefenderName,
							Outcome: r.Outcome,
							IsCurrentPlayerAttacker: r.AttackerId == currentPlayerId,
							IsCurrentPlayerDefender: r.DefenderId == currentPlayerId
						))
						.ToList();
				}
			}

			var vm = new GameReplayViewModel(
				GameId: data.Record.GameId.Id,
				GameName: data.Record.Name,
				GameDefType: data.Record.GameDefType,
				StartTime: data.Record.StartTime,
				ActualEndTime: data.Record.ActualEndTime,
				Status: data.Record.Status.ToString(),
				FinalStandings: finalStandings,
				BattleEvents: battleEvents
			);

			return Ok(vm);
		}
	}
}
