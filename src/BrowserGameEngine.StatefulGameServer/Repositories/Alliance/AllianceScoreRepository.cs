using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public record AllianceScore(
		string AllianceId,
		string Name,
		int MemberCount,
		decimal TotalLand,
		decimal AvgLand,
		decimal Score
	);

	public class AllianceScoreRepository {
		private readonly AllianceRepository allianceRepository;
		private readonly ScoreRepository scoreRepository;

		public AllianceScoreRepository(AllianceRepository allianceRepository, ScoreRepository scoreRepository) {
			this.allianceRepository = allianceRepository;
			this.scoreRepository = scoreRepository;
		}

		public IEnumerable<AllianceScore> GetRanked() {
			return allianceRepository.GetAll()
				.Select(alliance => {
					var acceptedMembers = alliance.Members.Where(m => !m.IsPending).ToList();
					return new { alliance, acceptedMembers };
				})
				.Where(x => x.acceptedMembers.Count >= 2)
				.Select(x => {
					decimal totalLand = x.acceptedMembers.Sum(m => scoreRepository.GetScore(m.PlayerId));
					decimal avgLand = totalLand / x.acceptedMembers.Count;
					decimal score = avgLand + totalLand / 12;
					return new AllianceScore(
						AllianceId: x.alliance.AllianceId.ToString(),
						Name: x.alliance.Name,
						MemberCount: x.acceptedMembers.Count,
						TotalLand: totalLand,
						AvgLand: avgLand,
						Score: score
					);
				})
				.OrderByDescending(vm => vm.Score);
		}
	}
}
