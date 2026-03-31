using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class SpyController : ControllerBase {
		private readonly ILogger<SpyController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly SpyRepositoryWrite spyRepositoryWrite;
		private readonly SpyRepository spyRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly ScoreRepository scoreRepository;
		private readonly GameDef gameDef;

		public SpyController(
			ILogger<SpyController> logger,
			CurrentUserContext currentUserContext,
			SpyRepositoryWrite spyRepositoryWrite,
			SpyRepository spyRepository,
			PlayerRepository playerRepository,
			UserRepository userRepository,
			ScoreRepository scoreRepository,
			GameDef gameDef
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.spyRepositoryWrite = spyRepositoryWrite;
			this.spyRepository = spyRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.scoreRepository = scoreRepository;
			this.gameDef = gameDef;
		}

		/// <summary>Returns all players except the current player, with per-target spy cooldown status, sorted by score descending.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(IEnumerable<SpyPlayerEntryViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<IEnumerable<SpyPlayerEntryViewModel>> Players() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var currentPlayerId = currentUserContext.PlayerId!;
			return playerRepository.GetAll()
				.Where(p => p.PlayerId != currentPlayerId)
				.Select(p => new SpyPlayerEntryViewModel {
					PlayerId = p.PlayerId.Id,
					PlayerName = p.UserId != null
						? userRepository.GetDisplayNameByUserId(p.UserId) ?? p.Name
						: p.Name,
					Score = scoreRepository.GetScore(p.PlayerId),
					CooldownExpiresAt = spyRepository.GetCooldownExpiry(currentPlayerId, p.PlayerId)
				})
				.OrderByDescending(p => p.Score)
				.ToList();
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
			}
		}
	}
}
