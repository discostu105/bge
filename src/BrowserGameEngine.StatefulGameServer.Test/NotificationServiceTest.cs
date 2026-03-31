using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class NotificationServiceTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void Notify_AddsNotificationToPlayer() {
			var game = new TestGame(playerCount: 2);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.AttackReceived, "Attack!", "You were attacked.");

			var notifications = service.GetNotifications(Player1);
			Assert.Single(notifications);
			Assert.Equal(GameNotificationType.AttackReceived, notifications[0].Type);
			Assert.Equal("Attack!", notifications[0].Title);
			Assert.Equal("You were attacked.", notifications[0].Body);
			Assert.False(notifications[0].IsRead);
		}

		[Fact]
		public void MarkRead_SetsReadAt() {
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.MessageReceived, "Hello");
			var notifications = service.GetNotifications(Player1);
			var notifId = notifications[0].Id;

			service.MarkRead(Player1, notifId);

			var updated = service.GetNotifications(Player1);
			Assert.True(updated[0].IsRead);
		}

		[Fact]
		public void MarkAllRead_MarksAllNotificationsRead() {
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.AttackReceived, "Attack 1");
			service.Notify(Player1, GameNotificationType.MessageReceived, "Msg 1");
			service.Notify(Player1, GameNotificationType.AllianceRequest, "Alliance 1");

			service.MarkAllRead(Player1);

			var notifications = service.GetNotifications(Player1);
			Assert.Equal(3, notifications.Count);
			Assert.All(notifications, n => Assert.True(n.IsRead));
		}

		[Fact]
		public void GetNotifications_UnreadOnly_ReturnsOnlyUnread() {
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.AttackReceived, "Attack 1");
			service.Notify(Player1, GameNotificationType.MessageReceived, "Msg 1");

			var allNotifs = service.GetNotifications(Player1);
			var firstId = allNotifs[allNotifs.Count - 1].Id; // oldest
			service.MarkRead(Player1, firstId);

			var unread = service.GetNotifications(Player1, unreadOnly: true);
			Assert.Single(unread);
			Assert.False(unread[0].IsRead);
		}

		[Fact]
		public void GetNotifications_OrderedByCreatedAtDescending() {
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.AttackReceived, "First");
			service.Notify(Player1, GameNotificationType.MessageReceived, "Second");
			service.Notify(Player1, GameNotificationType.AllianceRequest, "Third");

			var notifications = service.GetNotifications(Player1);
			Assert.Equal(3, notifications.Count);
			// Most recent should be first
			Assert.True(notifications[0].CreatedAt >= notifications[1].CreatedAt);
			Assert.True(notifications[1].CreatedAt >= notifications[2].CreatedAt);
		}

		[Fact]
		public void Notify_NonExistentPlayer_DoesNotThrow() {
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);
			var fakePlayerId = PlayerIdFactory.Create("nonexistent");

			// Should silently do nothing
			var ex = Record.Exception(() => service.Notify(fakePlayerId, GameNotificationType.AttackReceived, "test"));
			Assert.Null(ex);
		}

		[Fact]
		public void Notifications_PersistedInPlayerStateImmutable() {
			// Verify notifications are stored in player state (would survive serialization)
			var game = new TestGame(playerCount: 1);
			var service = new NotificationService(game.Accessor);

			service.Notify(Player1, GameNotificationType.SpyAttempted, "Spy detected!");

			// Check via repository (returns immutable snapshot)
			var playerState = game.PlayerRepository.Get(Player1).State;
			Assert.NotNull(playerState.Notifications);
			Assert.Single(playerState.Notifications!);
			Assert.Equal("Spy detected!", playerState.Notifications![0].Title);
		}

		[Fact]
		public void SendMessage_CreatesMessageReceivedNotification() {
			var game = new TestGame(playerCount: 2);
			// Use a real NotificationService (not the NullNotificationService from TestGame)
			var notificationService = new NotificationService(game.Accessor);
			var messageRepositoryWrite = new MessageRepositoryWrite(game.Accessor, TimeProvider.System, notificationService);

			messageRepositoryWrite.Send(new SendMessageCommand(Player1, Player2, "Hello", "World message body"));

			var notifications = notificationService.GetNotifications(Player2);
			Assert.Single(notifications);
			Assert.Equal(GameNotificationType.MessageReceived, notifications[0].Type);
		}
	}
}
