using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class AllianceRankingController : ControllerBase {
		private readonly AllianceScoreRepository allianceScoreRepository;

		public AllianceRankingController(AllianceScoreRepository allianceScoreRepository) {
			this.allianceScoreRepository = allianceScoreRepository;
		}

		/// <summary>Returns the current game's alliance ranking ordered by score.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<AllianceRankingViewModel>), StatusCodes.Status200OK)]
		public IEnumerable<AllianceRankingViewModel> Get() {
			return allianceScoreRepository.GetRanked().Select(s => new AllianceRankingViewModel(
				s.AllianceId, s.Name, s.MemberCount, s.TotalLand, s.AvgLand, s.Score));
		}
	}
}
