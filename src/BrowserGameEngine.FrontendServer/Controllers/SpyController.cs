using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class SpyController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly SpyRepositoryWrite spyRepositoryWrite;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly GameDef gameDef;

		public SpyController(
			CurrentUserContext currentUserContext,
			SpyRepositoryWrite spyRepositoryWrite,
			PlayerRepository playerRepository,
			UserRepository userRepository,
			GameDef gameDef
		) {
			this.currentUserContext = currentUserContext;
			this.spyRepositoryWrite = spyRepositoryWrite;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.gameDef = gameDef;
		}

		/// <summary>Executes a spy mission against a target player, returning fuzzy intel at a mineral cost. Subject to a 30-minute per-target cooldown.</summary>
		/// <param name="targetPlayerId">The player ID to spy on.</param>
		[HttpPost]
		[ProducesResponseType(typeof(SpyReportViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
		public ActionResult<SpyReportViewModel> Execute([FromQuery] string targetPlayerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var result = spyRepositoryWrite.ExecuteSpy(new SpyCommand(
					currentUserContext.PlayerId!,
					PlayerIdFactory.Create(targetPlayerId)
				));

				var targetPlayer = playerRepository.Get(result.TargetPlayerId);
				var targetName = targetPlayer.UserId != null
					? userRepository.GetDisplayNameByUserId(targetPlayer.UserId) ?? targetPlayer.Name
					: targetPlayer.Name;

				var mineralDefId = Id.ResDef("minerals");
				var gasDefId = Id.ResDef("gas");

				result.ApproximateResources.TryGetValue(mineralDefId, out var approxMinerals);
				result.ApproximateResources.TryGetValue(gasDefId, out var approxGas);

				var unitEstimates = result.UnitEstimates
					.Select(u => new UnitEstimateViewModel {
						UnitDefId = u.UnitDefId.Id,
						UnitTypeName = gameDef.GetUnitDef(u.UnitDefId)?.Name ?? u.UnitDefId.Id,
						ApproximateCount = u.ApproximateCount
					})
					.ToList();

				return new SpyReportViewModel {
					TargetPlayerId = result.TargetPlayerId.ToString(),
					TargetPlayerName = targetName,
					ApproximateMinerals = Math.Round(approxMinerals, 0),
					ApproximateGas = Math.Round(approxGas, 0),
					UnitEstimates = unitEstimates,
					ReportTime = result.ReportTime,
					CooldownExpiresAt = result.CooldownExpiresAt
				};
			} catch (SpyCooldownException e) {
				Response.Headers.Append("Retry-After", ((int)(e.CooldownExpiresAt - DateTime.UtcNow).TotalSeconds).ToString());
				return StatusCode(StatusCodes.Status429TooManyRequests, e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			} catch (ArgumentException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
