using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace BrowserGameEngine.FrontendServer.Controllers {
	public class OidcConfigurationController : Controller {
		private readonly ILogger<OidcConfigurationController> _logger;
		private readonly LoginController loginController;

		public OidcConfigurationController(ILogger<OidcConfigurationController> logger, LoginController loginController) {
			_logger = logger;
			this.loginController = loginController;
		}

		[HttpGet("_configuration/{clientId}")]
		public IActionResult GetClientRequestParameters([FromRoute] string clientId) {
			//var parameters = loginController.GetLoginData(clientId);
			//return Ok(parameters);
			throw new NotImplementedException();
		}
	}
}
