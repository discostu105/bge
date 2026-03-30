namespace BrowserGameEngine.Shared;

public record UserPreferencesViewModel(
	bool WantsGameNotification,
	bool AutoJoinNextGame
);

public record UpdateUserPreferencesRequest(
	bool? WantsGameNotification,
	bool? AutoJoinNextGame
);
