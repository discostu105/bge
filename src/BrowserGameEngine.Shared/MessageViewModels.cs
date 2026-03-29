using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class MessageViewModel {
		public string MessageId { get; set; } = "";
		public string? SenderId { get; set; }
		public string SenderName { get; set; } = "";
		public string RecipientId { get; set; } = "";
		public string Subject { get; set; } = "";
		public string Body { get; set; } = "";
		public bool IsRead { get; set; }
		public DateTime SentAt { get; set; }
	}

	public class MessageInboxViewModel {
		public List<MessageViewModel> Messages { get; set; } = new();
	}

	public class SendMessageViewModel {
		public string RecipientId { get; set; } = "";
		public string Subject { get; set; } = "";
		public string Body { get; set; } = "";
	}
}
