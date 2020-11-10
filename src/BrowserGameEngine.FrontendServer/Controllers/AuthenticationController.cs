using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvc.Client.Extensions;

namespace BrowserGameEngine.FrontendServer.Controllers {
	public class AuthenticationController : Controller {
		private readonly IOptions<BgeOptions> options;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;

		public AuthenticationController(ILogger<AuthenticationController> logger
				, IOptions<BgeOptions> options
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
			) {
			this.options = options;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
		}

		[HttpGet("~/signin")]
		public async Task<IActionResult> SignIn() => View("SignIn", await HttpContext.GetExternalProvidersAsync());

		[HttpPost("~/signin")]
		public async Task<IActionResult> SignIn([FromForm] string provider) {
			// Note: the "provider" parameter corresponds to the external
			// authentication provider choosen by the user agent.
			if (string.IsNullOrWhiteSpace(provider)) return BadRequest();
			if (!await HttpContext.IsProviderSupportedAsync(provider)) return BadRequest();

			// Instruct the middleware corresponding to the requested external identity
			// provider to redirect the user agent to its own authorization endpoint.
			// Note: the authenticationScheme parameter must match the value configured in Startup.cs
			return Challenge(new AuthenticationProperties { RedirectUri = "/" }, provider);
		}

		[HttpGet("~/signout")]
		[HttpPost("~/signout")]
		public IActionResult SignOut() {
			// Instruct the cookies middleware to delete the local cookie created
			// when the user agent is redirected from the external identity provider
			// after a successful authentication flow (e.g Google or Facebook).
			return SignOut(new AuthenticationProperties { RedirectUri = "/" },
				CookieAuthenticationDefaults.AuthenticationScheme);
		}

		/// <summary>
		/// For DEV purposes only.
		/// Direct login without any password. A new user gets created directly if it does not exist.
		/// </summary>
		[HttpPost("~/signindev")]
		public async Task<IActionResult> SignInDev([FromForm] string playerid) {
			// only works if DevAuth setting in appsettings is set (dev only!)
			if (!options.Value.DevAuth) return BadRequest();
			if (string.IsNullOrWhiteSpace(playerid)) return BadRequest();
			return CreatePlayer(PlayerIdFactory.Create(playerid));
		}

		private IActionResult CreatePlayer(PlayerId playerId) {
			if (!playerRepository.Exists(playerId)) {
				// create player
				playerRepositoryWrite.CreatePlayer(playerId);
			}
			currentUserContext.PlayerId = playerId;
			return Challenge();
		}
	}
}
