using BrowserGameEngine.GameModel;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class NullGameNotificationService : IGameNotificationService {
		public Task NotifyGameStartedAsync(GameRecordImmutable record, int playerCount) => Task.CompletedTask;
		public Task NotifyGameFinishedAsync(GameRecordImmutable record, PlayerId? winnerId, string? winnerName, int playerCount) => Task.CompletedTask;
	}
}
