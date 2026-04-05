using System;

namespace BrowserGameEngine.GameModel {
	public record ResourceSnapshot(
		int Tick,
		DateTime Timestamp,
		decimal Minerals,
		decimal Gas,
		decimal Land
	);
}
