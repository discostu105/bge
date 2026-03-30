using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/users/me/preferences")]
	public class UserPreferencesController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly UserRepository userRepository;
		private readonly UserRepositoryWrite userRepositoryWrite;

		public UserPreferencesController(
			CurrentUserContext currentUserContext,
			UserRepository userRepository,
			UserRepositoryWrite userRepositoryWrite
		) {
			this.currentUserContext = currentUserContext;
			this.userRepository = userRepository;
			this.userRepositoryWrite = userRepositoryWrite;
		}

		[HttpGet]
		public ActionResult<UserPreferencesViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var user = userRepository.GetByGithubId(currentUserContext.GithubId!);
			if (user == null) return NotFound();
			return Ok(new UserPreferencesViewModel(
				WantsGameNotification: user.WantsGameNotification,
				AutoJoinNextGame: user.AutoJoinNextGame
			));
		}

		[HttpPatch]
		public ActionResult Patch([FromBody] UpdateUserPreferencesRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var user = userRepository.GetByGithubId(currentUserContext.GithubId!);
			if (user == null) return NotFound();
			var wantsNotification = request.WantsGameNotification ?? user.WantsGameNotification;
			var autoJoin = request.AutoJoinNextGame ?? user.AutoJoinNextGame;
			userRepositoryWrite.SetGamePreferences(currentUserContext.GithubId!, wantsNotification, autoJoin);
			return NoContent();
		}
	}
}
