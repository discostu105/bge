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
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;
using System.Security.Claims;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class PlayerProfileController : ControllerBase {
		private readonly ILogger<PlayerProfileController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;

		public PlayerProfileController(ILogger<PlayerProfileController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
		}

		[HttpGet]
		public PlayerProfileViewModel Get() {
			var player = playerRepository.Get(currentUserContext.PlayerId);
			return new PlayerProfileViewModel { 
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name
			};
		}

		[HttpGet]
		[Route("init")]
		public CreatePlayerViewModel Init() {
			ClaimsPrincipal? user = User;
			if (user == null) throw new Exception("TODO exception type not authenticated");
			IIdentity? identity = user.Identity;
			if (identity == null) throw new Exception("TODO exception type not authenticated");
			if (!identity.IsAuthenticated) throw new Exception("TODO exception type not authenticated");
			var name = identity.Name;
			var id = user.Claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

			Console.WriteLine(id);
			return new CreatePlayerViewModel {
				PlayerName = name
			};
		}


		/* Example:
		  	http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier: 690094355519766528
			http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name: Christoph Neumüller
			urn:discord:user:discriminator: 0075
			urn:discord:avatar:url: https://cdn.discordapp.com/avatars/690094355519766528/.png
		*/

		[HttpPost]
		[Route("changename")]
		public IActionResult ChangePlayerName(PlayerProfileViewModel playerProfile) {
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(currentUserContext.PlayerId, playerProfile.PlayerName));
			return Ok();
		}

		[HttpPost]
		[Route("create")]
		public void Create(CreatePlayerViewModel model) {
			ClaimsPrincipal? user = User;
			if (user == null) throw new Exception("TODO exception type not authenticated");
			IIdentity? identity = user.Identity;
			if (identity == null) throw new Exception("TODO exception type not authenticated");
			if (!identity.IsAuthenticated) throw new Exception("TODO exception type not authenticated");
			var name = identity.Name;
			var id = user.Claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

			var playerId = GameModel.PlayerIdFactory.Create(id);
			playerRepositoryWrite.CreatePlayer(playerId);
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, model.PlayerName));
			currentUserContext.Activate(playerId);
		}
	}
}
