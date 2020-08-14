using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.GameDefinition;
using Discord.Rest;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/[controller]")]
	public class LoginController : ControllerBase {
		private readonly ILogger<LoginController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly OAuthDef oAuthDef;

		public LoginController(ILogger<LoginController> logger
				, CurrentUserContext currentUserContext
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.oAuthDef = gameDef.OAuth;
		}

		[HttpGet]
		[HttpGet("logindata")]
		public LoginDataViewModel GetLoginData() {
			return new LoginDataViewModel {
				OAuthUrl = oAuthDef.GetUrl()
			};
		}

		[HttpGet("oauthlogin/{code}")]
		public UserModel OAuthLogin(string code) {
			var client = new Discord.Rest.DiscordRestClient();

			

			return new UserModel {
				PlayerId = currentUserContext.PlayerId.Id,
				JwtToken = GenerateJwtToken()
			};
		}

		private string GenerateJwtToken() {
			return "ugl";
		}
	}
}
