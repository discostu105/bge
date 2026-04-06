using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Achievements;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api")]
	public class AchievementsController : ControllerBase {
		private readonly GlobalState globalState;
		private readonly PlayerRepository playerRepository;
		private readonly MilestoneRepository milestoneRepository;
		private readonly MilestoneRepositoryWrite milestoneRepositoryWrite;

		public AchievementsController(
			GlobalState globalState,
			PlayerRepository playerRepository,
			MilestoneRepository milestoneRepository,
			MilestoneRepositoryWrite milestoneRepositoryWrite
		) {
			this.globalState = globalState;
			this.playerRepository = playerRepository;
			this.milestoneRepository = milestoneRepository;
			this.milestoneRepositoryWrite = milestoneRepositoryWrite;
		}

		/// <summary>Returns game-completion trophies for the specified player.</summary>
		/// <param name="playerId">The player identifier.</param>
		[AllowAnonymous]
		[HttpGet("players/{playerId}/achievements")]
		[ProducesResponseType(typeof(PlayerAchievementsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<PlayerAchievementsViewModel> GetPlayerAchievements(string playerId) {
			PlayerImmutable player;
			try {
				player = playerRepository.Get(PlayerIdFactory.Create(playerId));
			} catch {
				return NotFound();
			}
			if (player.UserId == null) return Ok(new PlayerAchievementsViewModel([]));

			var achievements = AchievementMapper.GetForUser(globalState, player.UserId);
			return Ok(new PlayerAchievementsViewModel(achievements));
		}

		/// <summary>Manually awards a milestone to a user. Admin use only.</summary>
		[Authorize(Policy = "Admin")]
		[HttpPost("achievements/award")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public ActionResult AwardAchievement([FromBody] AwardAchievementRequest request) {
			if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.MilestoneId))
				return BadRequest("UserId and MilestoneId are required.");

			milestoneRepositoryWrite.UnlockIfNew(request.UserId, request.MilestoneId, DateTime.UtcNow);
			return Ok();
		}
	}

	public record AwardAchievementRequest(string UserId, string MilestoneId);
}
