using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record GlobalStateImmutable(
		IDictionary<string, UserImmutable> Users,
		IList<GameRecordImmutable> Games,
		IList<TournamentImmutable>? Tournaments = null
	);
}
