using BrowserGameEngine.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/version")]
	public class VersionController : ControllerBase {
		private static readonly string CommitHash =
			typeof(VersionController).Assembly
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion ?? "dev";

		/// <summary>Returns the deployed version (git commit hash) of the server.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(VersionInfoViewModel), StatusCodes.Status200OK)]
		public ActionResult<VersionInfoViewModel> GetVersion() {
			return Ok(new VersionInfoViewModel(CommitHash));
		}
	}
}
