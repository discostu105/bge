using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Notifications;
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
		private readonly INotificationService notificationService;
		private readonly AllianceInviteRepository allianceInviteRepository;
		private readonly AllianceInviteRepositoryWrite allianceInviteRepositoryWrite;
		private readonly AllianceWarRepository allianceWarRepository;
		private readonly AllianceWarRepositoryWrite allianceWarRepositoryWrite;

		public AlliancesController(
			CurrentUserContext currentUserContext,
			AllianceRepository allianceRepository,
			AllianceRepositoryWrite allianceRepositoryWrite,
			AllianceChatRepository allianceChatRepository,
			AllianceChatRepositoryWrite allianceChatRepositoryWrite,
			PlayerRepository playerRepository,
			INotificationService notificationService,
			AllianceInviteRepository allianceInviteRepository,
			AllianceInviteRepositoryWrite allianceInviteRepositoryWrite,
			AllianceWarRepository allianceWarRepository,
			AllianceWarRepositoryWrite allianceWarRepositoryWrite
		) {
			this.currentUserContext = currentUserContext;
			this.allianceRepository = allianceRepository;
			this.allianceRepositoryWrite = allianceRepositoryWrite;
			this.allianceChatRepository = allianceChatRepository;
			this.allianceChatRepositoryWrite = allianceChatRepositoryWrite;
			this.playerRepository = playerRepository;
			this.notificationService = notificationService;
			this.allianceInviteRepository = allianceInviteRepository;
			this.allianceInviteRepositoryWrite = allianceInviteRepositoryWrite;
			this.allianceWarRepository = allianceWarRepository;
			this.allianceWarRepositoryWrite = allianceWarRepositoryWrite;
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
			var alliance = allianceRepository.GetByPlayerId(currentUserContext.PlayerId!);
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
					new CreateAllianceCommand(currentUserContext.PlayerId!, request.AllianceName, request.Password));
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
				var allianceId = AllianceIdFactory.Create(id);
				allianceRepositoryWrite.JoinAlliance(
					new JoinAllianceCommand(currentUserContext.PlayerId!, allianceId, request.Password));

				// Notify alliance leader if join request is pending approval
				var alliance = allianceRepository.Get(allianceId);
				if (alliance != null) {
					var joiningMember = alliance.Members.FirstOrDefault(m => m.PlayerId == currentUserContext.PlayerId && m.IsPending);
					if (joiningMember != null) {
						var joiningPlayer = playerRepository.Get(currentUserContext.PlayerId!);
						notificationService.Notify(alliance.LeaderId, GameNotificationType.AllianceRequest,
							$"Alliance join request from {joiningPlayer.Name}");
					}
				}
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
					new AcceptMemberCommand(currentUserContext.PlayerId!, PlayerIdFactory.Create(pid)));
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
					new RejectMemberCommand(currentUserContext.PlayerId!, PlayerIdFactory.Create(pid)));
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
				allianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(currentUserContext.PlayerId!));
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
					new KickMemberCommand(currentUserContext.PlayerId!, PlayerIdFactory.Create(pid)));
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
					new VoteLeaderCommand(currentUserContext.PlayerId!, PlayerIdFactory.Create(request.VoteePlayerId)));
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
					new SetAlliancePasswordCommand(currentUserContext.PlayerId!, request.NewPassword));
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
					new SetAllianceMessageCommand(currentUserContext.PlayerId!, request.Message));
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
			if (!allianceRepository.IsMember(currentUserContext.PlayerId!, allianceId)) {
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
					new PostAllianceChatCommand(currentUserContext.PlayerId!, AllianceIdFactory.Create(id), request.Body));
				return Ok(postId.ToString());
			} catch (AllianceNotFoundException) {
				return NotFound();
			} catch (NotAllianceMemberException e) {
				return StatusCode(403, e.Message);
			}
		}

		/// <summary>Invites a player to the alliance (leader only).</summary>
		/// <param name="id">The alliance ID.</param>
		/// <param name="request">The invite request containing the target player ID.</param>
		[HttpPost("{id}/invite")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult InvitePlayer(string id, [FromBody] InvitePlayerRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var targetPlayerId = PlayerIdFactory.Create(request.TargetPlayerId);
			if (!playerRepository.Exists(targetPlayerId)) return NotFound();
			try {
				allianceInviteRepositoryWrite.InvitePlayer(
					new InvitePlayerToAllianceCommand(currentUserContext.PlayerId!, targetPlayerId));
				return Ok();
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (AlreadyInAllianceException e) {
				return Conflict(e.Message);
			} catch (InviteAlreadyExistsException e) {
				return Conflict(e.Message);
			}
		}

		/// <summary>Accepts an alliance invite.</summary>
		/// <param name="id">The alliance ID.</param>
		/// <param name="request">The accept invite request containing the invite ID.</param>
		[HttpPost("{id}/accept-invite")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult AcceptInvite(string id, [FromBody] AcceptInviteRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceInviteRepositoryWrite.AcceptInvite(
					new AcceptAllianceInviteCommand(currentUserContext.PlayerId!, AllianceInviteIdFactory.Create(request.InviteId)));
				return Ok();
			} catch (InviteNotFoundException) {
				return NotFound();
			}
		}

		/// <summary>Declines an alliance invite.</summary>
		/// <param name="id">The alliance ID.</param>
		/// <param name="request">The decline invite request containing the invite ID.</param>
		[HttpPost("{id}/decline-invite")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult DeclineInvite(string id, [FromBody] AcceptInviteRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceInviteRepositoryWrite.DeclineInvite(
					new DeclineAllianceInviteCommand(currentUserContext.PlayerId!, AllianceInviteIdFactory.Create(request.InviteId)));
				return Ok();
			} catch (InviteNotFoundException) {
				return NotFound();
			}
		}

		/// <summary>Returns all active invites for the current player.</summary>
		[HttpGet("my-invites")]
		[ProducesResponseType(typeof(IEnumerable<AllianceInviteViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<IEnumerable<AllianceInviteViewModel>> GetMyInvites() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var invites = allianceInviteRepository.GetActiveInvitesForPlayer(currentUserContext.PlayerId!);
			return Ok(invites.Select(i => {
				string allianceName;
				try { allianceName = allianceRepository.Get(i.AllianceId)?.Name ?? i.AllianceId.ToString(); }
				catch { allianceName = i.AllianceId.ToString(); }
				string inviterName;
				try { inviterName = playerRepository.Get(i.InviterPlayerId).Name; }
				catch { inviterName = i.InviterPlayerId.Id; }
				return new AllianceInviteViewModel {
					InviteId = i.InviteId.ToString(),
					AllianceId = i.AllianceId.ToString(),
					AllianceName = allianceName,
					InviterPlayerName = inviterName,
					ExpiresAt = i.ExpiresAt
				};
			}));
		}

		/// <summary>Declares war on another alliance (leader only).</summary>
		/// <param name="id">The declaring alliance ID.</param>
		/// <param name="request">The declare war request containing the target alliance ID.</param>
		[HttpPost("{id}/declare-war")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult DeclareWar(string id, [FromBody] DeclareWarRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				allianceWarRepositoryWrite.DeclareWar(
					new DeclareAllianceWarCommand(currentUserContext.PlayerId!, AllianceIdFactory.Create(request.TargetAllianceId)));
				return Ok();
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			} catch (NotAllianceLeaderException e) {
				return StatusCode(403, e.Message);
			} catch (AllianceNotFoundException) {
				return NotFound();
			} catch (AlreadyAtWarException e) {
				return Conflict(e.Message);
			}
		}

		/// <summary>Proposes or accepts peace for a war (based on current war state).</summary>
		/// <param name="warId">The war ID.</param>
		/// <param name="request">The peace request.</param>
		[HttpPost("wars/{warId}/peace")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult Peace(string warId, [FromBody] PeaceRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var warIdTyped = AllianceWarIdFactory.Create(warId);
				var war = allianceWarRepository.GetWar(warIdTyped);
				var player = playerRepository.Get(currentUserContext.PlayerId!);

				if (war.Status == GameModel.AllianceWarStatus.Active) {
					allianceWarRepositoryWrite.ProposePeace(new ProposeAlliancePeaceCommand(currentUserContext.PlayerId!, warIdTyped));
					return Ok();
				} else if (war.Status == GameModel.AllianceWarStatus.PeaceProposed) {
					// Proposer cannot accept own proposal — map the leader exception from AcceptPeace to 409
					try {
						allianceWarRepositoryWrite.AcceptPeace(new AcceptAlliancePeaceCommand(currentUserContext.PlayerId!, warIdTyped));
						return Ok();
					} catch (NotAllianceLeaderException e) {
						// If the leader tried to accept their own proposal, treat as conflict
						return Conflict(e.Message);
					}
				} else {
					return BadRequest("War is not in an active state.");
				}
			} catch (WarNotFoundException) {
				return NotFound();
			} catch (NotAllianceMemberException e) {
				return BadRequest(e.Message);
			} catch (PeaceAlreadyProposedException e) {
				return Conflict(e.Message);
			} catch (NotAtWarException e) {
				return BadRequest(e.Message);
			}
		}

		/// <summary>Returns all active and recent wars for an alliance.</summary>
		/// <param name="id">The alliance ID.</param>
		[HttpGet("{id}/wars")]
		[ProducesResponseType(typeof(IEnumerable<AllianceWarViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<IEnumerable<AllianceWarViewModel>> GetWars(string id) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var allianceId = AllianceIdFactory.Create(id);
			if (allianceRepository.Get(allianceId) == null) return NotFound();
			var wars = allianceWarRepository.GetActiveWars(allianceId);
			return Ok(wars.Select(w => {
				string attackerName;
				try { attackerName = allianceRepository.Get(w.AttackerAllianceId)?.Name ?? w.AttackerAllianceId.ToString(); }
				catch { attackerName = w.AttackerAllianceId.ToString(); }
				string defenderName;
				try { defenderName = allianceRepository.Get(w.DefenderAllianceId)?.Name ?? w.DefenderAllianceId.ToString(); }
				catch { defenderName = w.DefenderAllianceId.ToString(); }
				return new AllianceWarViewModel {
					WarId = w.WarId.ToString(),
					AttackerAllianceId = w.AttackerAllianceId.ToString(),
					AttackerAllianceName = attackerName,
					DefenderAllianceId = w.DefenderAllianceId.ToString(),
					DefenderAllianceName = defenderName,
					Status = w.Status.ToString(),
					DeclaredAt = w.DeclaredAt,
					ProposerAllianceId = w.ProposerAllianceId?.ToString()
				};
			}));
		}
	}
}
