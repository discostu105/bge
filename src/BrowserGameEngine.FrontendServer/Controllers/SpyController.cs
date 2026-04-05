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
		private readonly SpyMissionRepositoryWrite spyMissionRepositoryWrite;
		private readonly SpyMissionRepository spyMissionRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly ScoreRepository scoreRepository;
		private readonly GameDef gameDef;

		public SpyController(
			ILogger<SpyController> logger,
			CurrentUserContext currentUserContext,
			SpyRepositoryWrite spyRepositoryWrite,
			SpyRepository spyRepository,
			SpyMissionRepositoryWrite spyMissionRepositoryWrite,
			SpyMissionRepository spyMissionRepository,
			PlayerRepository playerRepository,
			UserRepository userRepository,
			ScoreRepository scoreRepository,
			GameDef gameDef
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.spyRepositoryWrite = spyRepositoryWrite;
			this.spyRepository = spyRepository;
			this.spyMissionRepositoryWrite = spyMissionRepositoryWrite;
			this.spyMissionRepository = spyMissionRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.scoreRepository = scoreRepository;
			this.gameDef = gameDef;
		}

		/// <summary>Returns detected spy attempts against the current player, most recent first. Supports optional pagination via page/pageSize query params.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PaginatedResponse<SpyAttemptViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PaginatedResponse<SpyAttemptViewModel>> Attempts([FromQuery] int page = 1, [FromQuery] int pageSize = 25) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var currentPlayerId = currentUserContext.PlayerId!;
			var attempts = spyRepository.GetDetectedSpyAttempts(currentPlayerId);
			var vms = attempts.Select(a => {
				string attackerName;
				try {
					var attacker = playerRepository.Get(a.AttackerPlayerId);
					attackerName = attacker.UserId != null
						? userRepository.GetDisplayNameByUserId(attacker.UserId) ?? attacker.Name
						: attacker.Name;
				} catch (Exception ex) {
					logger.LogWarning(ex, "Could not resolve attacker display name for player {PlayerId}", a.AttackerPlayerId);
					attackerName = a.AttackerPlayerId.ToString();
				}
				return new SpyAttemptViewModel {
					Id = a.Id,
					AttackerName = attackerName,
					ActionType = a.ActionType,
					Detected = a.Detected,
					Timestamp = a.Timestamp
				};
			});
			return PaginatedResponse<SpyAttemptViewModel>.Create(vms, page, pageSize);
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

		/// <summary>Dispatches an offensive spy mission against a target player.</summary>
		[HttpPost("send")]
		[ProducesResponseType(typeof(SendSpyMissionResponse), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<SendSpyMissionResponse> Send([FromBody] SendSpyMissionRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (!Enum.TryParse<SpyMissionType>(request.MissionType, ignoreCase: true, out var missionType)) {
				return BadRequest($"Unknown mission type: {request.MissionType}");
			}
			try {
				var (missionId, estimatedResolveAt) = spyMissionRepositoryWrite.SendMission(new SpyMissionCommand(
					currentUserContext.PlayerId!,
					PlayerIdFactory.Create(request.TargetPlayerId),
					missionType
				));
				return StatusCode(StatusCodes.Status201Created, new SendSpyMissionResponse {
					MissionId = missionId,
					EstimatedResolveAt = estimatedResolveAt
				});
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}

		/// <summary>Returns outgoing spy missions for the current player, most recent first. Supports optional pagination via page/pageSize query params.</summary>
		[HttpGet("missions")]
		[ProducesResponseType(typeof(PaginatedResponse<SpyMissionViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PaginatedResponse<SpyMissionViewModel>> Missions([FromQuery] int page = 1, [FromQuery] int pageSize = 25) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var currentPlayerId = currentUserContext.PlayerId!;
			var missions = spyMissionRepository.GetMissions(currentPlayerId);
			var vms = missions.Select(m => {
				string targetName;
				try {
					var target = playerRepository.Get(m.TargetPlayerId);
					targetName = target.UserId != null
						? userRepository.GetDisplayNameByUserId(target.UserId) ?? target.Name
						: target.Name;
				} catch (Exception ex) {
					logger.LogWarning(ex, "Could not resolve target display name for player {PlayerId}", m.TargetPlayerId);
					targetName = m.TargetPlayerId.ToString();
				}
				return new SpyMissionViewModel {
					Id = m.Id,
					TargetPlayerId = m.TargetPlayerId.ToString(),
					TargetPlayerName = targetName,
					MissionType = m.MissionType.ToString(),
					Status = m.Status.ToString(),
					CreatedAt = m.CreatedAt,
					Result = m.Result
				};
			});
			return PaginatedResponse<SpyMissionViewModel>.Create(vms, page, pageSize);
		}
	}
}
