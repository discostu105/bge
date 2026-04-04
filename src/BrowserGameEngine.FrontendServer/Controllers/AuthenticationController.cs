using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
		private readonly UserRepositoryWrite userRepositoryWrite;

		public AuthenticationController(ILogger<AuthenticationController> logger
				, IOptions<BgeOptions> options
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, UserRepositoryWrite userRepositoryWrite
			) {
			this.options = options;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.userRepositoryWrite = userRepositoryWrite;
		}

		[HttpGet("~/signin")]
		public async Task<IActionResult> SignIn(string? returnUrl = null) {
			ViewBag.ReturnUrl = returnUrl;
			return View("SignIn", await HttpContext.GetExternalProvidersAsync());
		}

		[HttpPost("~/signin")]
		public async Task<IActionResult> SignIn([FromForm] string provider, [FromForm] string? returnUrl = null, [FromForm] bool rememberMe = false) {
			// Note: the "provider" parameter corresponds to the external
			// authentication provider choosen by the user agent.
			if (string.IsNullOrWhiteSpace(provider)) return BadRequest();
			if (!await HttpContext.IsProviderSupportedAsync(provider)) return BadRequest();

			// Redirect back to the original page after login, or home if none/external.
			var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

			// Instruct the middleware corresponding to the requested external identity
			// provider to redirect the user agent to its own authorization endpoint.
			// Note: the authenticationScheme parameter must match the value configured in Startup.cs
			return Challenge(new AuthenticationProperties { RedirectUri = redirectUri, IsPersistent = rememberMe }, provider);
		}

		[HttpGet("~/signout")]
		[HttpPost("~/signout")]
		public new IActionResult SignOut() {
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

			var playerId = PlayerIdFactory.Create(playerid);
			var user = userRepositoryWrite.GetOrCreateUser(
				githubId: playerid,
				githubLogin: playerid,
				displayName: playerid
			);
			if (!playerRepository.Exists(playerId)) {
				playerRepositoryWrite.CreatePlayer(playerId, user.UserId);
			}

			// Create a claims identity so the cookie auth and CurrentUserMiddleware work correctly
			var claims = new List<Claim> {
				new Claim(ClaimTypes.NameIdentifier, playerid),
				new Claim(ClaimTypes.Name, playerid)
			};
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
			return Redirect("/");
		}
	}
}
