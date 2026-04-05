using BrowserGameEngine.GameModel;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public interface IGameNotificationService {
		Task NotifyGameStartedAsync(GameRecordImmutable record, int playerCount);
		Task NotifyGameFinishedAsync(GameRecordImmutable record, PlayerId? winnerId, string? winnerName, int playerCount, string? victoryConditionLabel = null);
	}
}
