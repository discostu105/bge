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
		private readonly AllianceChatRepository allianceChatRepository;
		private readonly AllianceChatRepositoryWrite allianceChatRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public AlliancesController(
			CurrentUserContext currentUserContext,
			AllianceRepository allianceRepository,
			AllianceRepositoryWrite allianceRepositoryWrite,
			AllianceChatRepository allianceChatRepository,
			AllianceChatRepositoryWrite allianceChatRepositoryWrite,
			PlayerRepository playerRepository
		) {
			this.currentUserContext = currentUserContext;
			this.allianceRepository = allianceRepository;
			this.allianceRepositoryWrite = allianceRepositoryWrite;
			this.allianceChatRepository = allianceChatRepository;
			this.allianceChatRepositoryWrite = allianceChatRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		/// <summary>Returns all alliances in the current game.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(IEnumerable<AllianceViewModel>), StatusCodes.Status200OK)]
		public ActionResult<IEnumerable<AllianceViewModel>> GetAll() {
			return Ok(allianceRepository.GetAll().Select(a => new AllianceViewModel {
				AllianceId = a.AllianceId.ToString(),
				Name = a.Name,
				Message = a.Message,
				MemberCount = a.Members.Count(m => !m.IsPending),
				Created = a.Created
			}));
		}

		/// <summary>Returns the current player's alliance membership status.</summary>
		[HttpGet("my-status")]
		[ProducesResponseType(typeof(MyAllianceStatusViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

		/// <summary>Returns detailed information about a specific alliance including member list.</summary>
		/// <param name="id">The alliance ID.</param>
		[HttpGet("{id}")]
		[ProducesResponseType(typeof(AllianceDetailViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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

		/// <summary>Creates a new alliance with the current player as leader.</summary>
		[HttpPost]
		[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
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

		/// <summary>Requests to join an alliance (requires leader approval if password-protected).</summary>
		/// <param name="id">The alliance ID to join.</param>
		/// <param name="request">Join request containing the optional password.</param>
		[HttpPost("{id}/join")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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

		[HttpGet("{id}/posts")]
		public ActionResult<IEnumerable<AllianceChatPostViewModel>> GetPosts(string id) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var allianceId = AllianceIdFactory.Create(id);
			if (!allianceRepository.IsMember(currentUserContext.PlayerId, allianceId)) {
				return StatusCode(403, "You are not a member of this alliance.");
			}
			var posts = allianceChatRepository.GetPosts(allianceId);
			return Ok(posts.Select(p => {
				string authorName;
				try { authorName = playerRepository.Get(p.AuthorPlayerId).Name; }
				catch { authorName = p.AuthorPlayerId.Id; }
				return new AllianceChatPostViewModel {
					PostId = p.PostId.ToString(),
					AuthorPlayerId = p.AuthorPlayerId.Id,
					AuthorName = authorName,
					Body = p.Body,
					CreatedAt = p.CreatedAt
				};
			}));
		}

		[HttpPost("{id}/posts")]
		public ActionResult<string> PostChat(string id, [FromBody] PostAllianceChatRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var postId = allianceChatRepositoryWrite.Post(
					new PostAllianceChatCommand(currentUserContext.PlayerId, AllianceIdFactory.Create(id), request.Body));
				return Ok(postId.ToString());
			} catch (AllianceNotFoundException) {
				return NotFound();
			} catch (NotAllianceMemberException e) {
				return StatusCode(403, e.Message);
			}
		}
	}
}
