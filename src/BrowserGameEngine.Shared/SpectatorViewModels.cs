using System.Collections.Generic;

namespace BrowserGameEngine.Shared;

public record SpectatorPlayerEntryViewModel(
	int Rank,
	string PlayerId,
	string PlayerName,
	decimal Score,
	bool IsOnline,
	bool IsAgent
);

public record SpectatorSnapshotViewModel(
	string GameId,
	string GameName,
	string GameStatus,
	IReadOnlyList<SpectatorPlayerEntryViewModel> TopPlayers,
	long Tick
);
