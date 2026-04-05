using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record ResourceSnapshotViewModel(
		int Tick,
		decimal Minerals,
		decimal Gas,
		decimal Land
	);

	public record ResourceHistoryViewModel(
		IList<ResourceSnapshotViewModel> Snapshots
	);
}
