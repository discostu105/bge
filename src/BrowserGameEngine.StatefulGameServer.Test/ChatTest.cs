using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Repositories.Chat;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class ChatTest {
		[Fact]
		public void PostMessage_ValidPlayer_MessageStored() {
			var game = new TestGame();
			var chatRepo = new ChatRepository(game.Accessor);
			var chatWrite = new ChatRepositoryWrite(game.Accessor, TimeProvider.System);

			chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, "Hello world"));

			var messages = chatRepo.GetMessages();
			Assert.Single(messages);
			Assert.Equal("Hello world", messages[0].Body);
			Assert.Equal(game.Player1, messages[0].AuthorPlayerId);
		}

		[Fact]
		public void PostMessage_RingBuffer_KeepsLast200() {
			var game = new TestGame();
			var chatWrite = new ChatRepositoryWrite(game.Accessor, TimeProvider.System);
			var chatRepo = new ChatRepository(game.Accessor);

			for (int i = 0; i < 205; i++) {
				chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, $"Message {i}"));
			}

			var messages = chatRepo.GetMessages(300);
			Assert.Equal(200, messages.Count);
			Assert.Equal("Message 5", messages[0].Body);
			Assert.Equal("Message 204", messages[^1].Body);
		}

		[Fact]
		public void GetMessagesAfter_ReturnsOnlyNewMessages() {
			var game = new TestGame();
			var chatWrite = new ChatRepositoryWrite(game.Accessor, TimeProvider.System);
			var chatRepo = new ChatRepository(game.Accessor);

			chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, "First"));
			chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, "Second"));
			chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, "Third"));

			var all = chatRepo.GetMessages();
			Assert.Equal(3, all.Count);

			var afterFirst = chatRepo.GetMessagesAfter(all[0].MessageId.ToString());
			Assert.Equal(2, afterFirst.Count);
			Assert.Equal("Second", afterFirst[0].Body);
			Assert.Equal("Third", afterFirst[1].Body);
		}

		[Fact]
		public void GetMessagesAfter_UnknownId_ReturnsEmpty() {
			var game = new TestGame();
			var chatWrite = new ChatRepositoryWrite(game.Accessor, TimeProvider.System);
			var chatRepo = new ChatRepository(game.Accessor);

			chatWrite.PostMessage(new PostChatMessageCommand(game.Player1, "Hello"));

			var result = chatRepo.GetMessagesAfter(System.Guid.NewGuid().ToString());
			Assert.Empty(result);
		}
	}
}
