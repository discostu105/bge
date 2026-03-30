using BrowserGameEngine.Shared;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/version")]
	public class VersionController : ControllerBase {
		private static readonly string CommitHash =
			Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev";

		[HttpGet]
		public ActionResult<VersionInfoViewModel> GetVersion() {
			return Ok(new VersionInfoViewModel(CommitHash));
		}
	}
}
