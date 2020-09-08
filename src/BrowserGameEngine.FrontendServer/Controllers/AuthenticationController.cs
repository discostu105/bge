﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mvc.Client.Extensions;

namespace BrowserGameEngine.FrontendServer.Controllers {
	public class AuthenticationController : Controller {
		private readonly PlayerRepositoryWrite playerRepositoryWrite;

		public AuthenticationController(ILogger<AuthenticationController> logger
				, PlayerRepositoryWrite playerRepositoryWrite
			) {
			this.playerRepositoryWrite = playerRepositoryWrite;
		}


		[HttpGet("~/signin")]
		public async Task<IActionResult> SignIn() => View("SignIn", await HttpContext.GetExternalProvidersAsync());


		[HttpPost("~/signin")]
		public async Task<IActionResult> SignIn([FromForm] string provider) {
			// Note: the "provider" parameter corresponds to the external
			// authentication provider choosen by the user agent.
			if (string.IsNullOrWhiteSpace(provider)) {
				return BadRequest();
			}

			if (!await HttpContext.IsProviderSupportedAsync(provider)) {
				return BadRequest();
			}

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
	}
}