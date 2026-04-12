using System;

namespace BrowserGameEngine.Shared {
	public enum GameNotificationTypeViewModel {
		AttackReceived,
		AllianceRequest,
		MessageReceived
	}

	public class GameNotificationViewModel {
		public Guid Id { get; set; }
		public GameNotificationTypeViewModel Type { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? Body { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsRead { get; set; }
	}
}
