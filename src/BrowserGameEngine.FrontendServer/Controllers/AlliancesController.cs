using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class AlliancesController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly AllianceRepository allianceRepository;
		private readonly AllianceRepositoryWrite allianceRepositoryWrite;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly UnitRepository unitRepository;
		private readonly ResourceRepository resourceRepository;

		public AlliancesController(
			CurrentUserContext currentUserContext,
			AllianceRepository allianceRepository,
			AllianceRepositoryWrite allianceRepositoryWrite,
			PlayerRepository playerRepository,
			PlayerRepositoryWrite playerRepositoryWrite,
			UnitRepository unitRepository,
			ResourceRepository resourceRepository
		) {
			this.currentUserContext = currentUserContext;
			this.allianceRepository = allianceRepository;
			this.allianceRepositoryWrite = allianceRepositoryWrite;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.unitRepository = unitRepository;
			this.resourceRepository = resourceRepository;
		}

		[HttpGet]
		public ActionResult<IEnumerable<AllianceViewModel>> GetAll() {
			return Ok(allianceRepository.GetAll().Select(a => new AllianceViewModel {
				AllianceId = a.AllianceId.ToString(),
				Name = a.Name,
				Message = a.Message,
				MemberCount = a.Members.Count(m => !m.IsPending),
				Created = a.Created
			}));
		}

		[HttpGet("my-status")]
		public ActionResult<MyAllianceStatusViewModel> MyStatus() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var alliance = allianceRepository.GetByPlayerId(currentUserContext.PlayerId);
			if (alliance == null) {
				return Ok(new MyAllianceStatusViewModel { IsMember = false });
			}
			var member = alliance.Members.FirstOrDefault(m => m.PlayerId == currentUserContext.PlayerId);
			return Ok(new MyAllianceStatusViewModel {
				AllianceId = alliance.AllianceId.ToString(),
				AllianceName = alliance.Name,
				IsMember = true,
				IsPending = member?.IsPending ?? false,
				IsLeader = alliance.LeaderId == currentUserContext.PlayerId
			});
		}

		[HttpGet("members/stats")]
		public ActionResult<IEnumerable<AllianceMemberStatsViewModel>> GetMemberStats() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var alliance = allianceRepository.GetByPlayerId(currentUserContext.PlayerId);
			if (alliance == null) return StatusCode(403, "Not in an alliance.");
			var member = alliance.Members.FirstOrDefault(m => m.PlayerId == currentUserContext.PlayerId);
			if (member == null || member.IsPending) return StatusCode(403, "Not an accepted alliance member.");

			var stats = alliance.Members
				.Where(m => !m.IsPending)
				.Select(m => {
					var player = playerRepository.Get(m.PlayerId);
					if (player.State.ShareStatsWithAlliance) {
						return new AllianceMemberStatsViewModel {
							PlayerId = m.PlayerId.Id,
							PlayerName = player.Name,
							SharesStats = true,
							Land = resourceRepository.GetAmount(m.PlayerId, Id.ResDef("land")),
							Minerals = resourceRepository.GetAmount(m.PlayerId, Id.ResDef("minerals")),
							Gas = resourceRepository.GetAmount(m.PlayerId, Id.ResDef("gas")),
							ArmySize = unitRepository.GetTotalUnitCount(m.PlayerId)
						};
					}
					return new AllianceMemberStatsViewModel {
						PlayerId = m.PlayerId.Id,
						PlayerName = player.Name,
						SharesStats = false
					};
				});
			return Ok(stats);
		}

		[HttpPost("my-stat-share")]
		public ActionResult SetStatShare([FromBody] SetAllianceStatShareRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var alliance = allianceRepository.GetByPlayerId(currentUserContext.PlayerId);
			if (alliance == null) return StatusCode(403, "Not in an alliance.");
			var member = alliance.Members.FirstOrDefault(m => m.PlayerId == currentUserContext.PlayerId);
			if (member == null || member.IsPending) return StatusCode(403, "Not an accepted alliance member.");

			playerRepositoryWrite.SetAllianceStatShare(
				new SetAllianceStatShareCommand(currentUserContext.PlayerId, request.ShareStats));
			return Ok();
		}

		[HttpGet("{id}")]
		public ActionResult<AllianceDetailViewModel> GetById(string id) {
			var allianceId = AllianceIdFactory.Create(id);
			var alliance = allianceRepository.Get(allianceId);
			if (alliance == null) return NotFound();
			return Ok(new AllianceDetailViewModel {
				AllianceId = alliance.AllianceId.ToString(),
				Name = alliance.Name,
				Message = alliance.Message,
				Created = alliance.Created,
				LeaderId = alliance.LeaderId.Id,
				Members = alliance.Members.Select(m => {
					string playerName;
					try { playerName = playerRepository.Get(m.PlayerId).Name; }
					catch { playerName = m.PlayerId.Id; }
					return new AllianceMemberViewModel {
						PlayerId = m.PlayerId.Id,
						PlayerName = playerName,
						IsPending = m.IsPending,
						JoinedAt = m.JoinedAt,
						VoteCount = m.VoteCount,
						IsLeader = m.PlayerId == alliance.LeaderId
					};
				}).ToList()
			});
		}

		[HttpPost]
		public ActionResult<string> Create([FromBody] CreateAllianceRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var allianceId = allianceRepositoryWrite.CreateAlliance(
					new CreateAllianceCommand(currentUserContext.PlayerId, request.AllianceName, request.Password));
				return Ok(allianceId.ToString());
			} catch (AllianceNameTakenException e) {
				return Conflict(e.Message);
			} catch (AlreadyInAllianceException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost("{id}/join")]
		public ActionResult Join(string id, [FromBody] JoinAllianceRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.JoinAlliance(
					new JoinAllianceCommand(currentUserContext.PlayerId, AllianceIdFactory.Create(id), request.Password));
				return Ok();
			} catch (AllianceNotFoundException) {
				return NotFound();
			} catch (InvalidAlliancePasswordException e) {
				return BadRequest(e.Message);
			} catch (AlreadyInAllianceException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost("{id}/members/{pid}/accept")]
		public ActionResult AcceptMember(string id, string pid) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.AcceptMember(
					new AcceptMemberCommand(currentUserContext.PlayerId, PlayerIdFactory.Create(pid)));
				return Ok();
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost("{id}/members/{pid}/reject")]
		public ActionResult RejectMember(string id, string pid) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.RejectMember(
					new RejectMemberCommand(currentUserContext.PlayerId, PlayerIdFactory.Create(pid)));
				return Ok();
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpDelete("leave")]
		public ActionResult Leave() {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(currentUserContext.PlayerId));
				return Ok();
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpDelete("{id}/members/{pid}")]
		public ActionResult KickMember(string id, string pid) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.KickMember(
					new KickMemberCommand(currentUserContext.PlayerId, PlayerIdFactory.Create(pid)));
				return Ok();
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost("{id}/leader-vote")]
		public ActionResult VoteLeader(string id, [FromBody] VoteLeaderRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.VoteLeader(
					new VoteLeaderCommand(currentUserContext.PlayerId, PlayerIdFactory.Create(request.VoteePlayerId)));
				return Ok();
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPatch("{id}/password")]
		public ActionResult SetPassword(string id, [FromBody] SetAlliancePasswordRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.SetAlliancePassword(
					new SetAlliancePasswordCommand(currentUserContext.PlayerId, request.NewPassword));
				return Ok();
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPatch("{id}/message")]
		public ActionResult SetMessage(string id, [FromBody] SetAllianceMessageRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceRepositoryWrite.SetAllianceMessage(
					new SetAllianceMessageCommand(currentUserContext.PlayerId, request.Message));
				return Ok();
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
