using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using BrowserGameEngine.StatefulGameServer.Repositories.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Events {
	/// <summary>
	/// Records all events published through IGameEventPublisher for assertion.
	/// </summary>
	public class RecordingGameEventPublisher : IGameEventPublisher {
		public List<(PlayerId PlayerId, string EventType, object Payload)> PlayerEvents { get; } = new();
		public List<(AllianceId AllianceId, string EventType, object Payload)> AllianceEvents { get; } = new();
		public List<(string EventType, object Payload)> GameEvents { get; } = new();

		public void PublishToPlayer(PlayerId playerId, string eventType, object payload) {
			PlayerEvents.Add((playerId, eventType, payload));
		}

		public void PublishToAlliance(AllianceId allianceId, string eventType, object payload) {
			AllianceEvents.Add((allianceId, eventType, payload));
		}

		public void PublishToGame(string eventType, object payload) {
			GameEvents.Add((eventType, payload));
		}
	}

	public class GameEventPublisherIntegrationTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void NotificationService_Notify_PublishesReceiveNotification() {
			var game = new TestGame();
			var recorder = new RecordingGameEventPublisher();
			var service = new NotificationService(game.Accessor, recorder);

			service.Notify(Player1, GameNotificationType.AttackReceived, "Under attack!", "Details here");

			Assert.Single(recorder.PlayerEvents);
			Assert.Equal(Player1, recorder.PlayerEvents[0].PlayerId);
			Assert.Equal(GameEventTypes.ReceiveNotification, recorder.PlayerEvents[0].EventType);
		}

		[Fact]
		public void NotificationService_NotifyNonExistentPlayer_DoesNotPublish() {
			var game = new TestGame();
			var recorder = new RecordingGameEventPublisher();
			var service = new NotificationService(game.Accessor, recorder);

			service.Notify(PlayerIdFactory.Create("nonexistent"), GameNotificationType.AttackReceived, "test");

			Assert.Empty(recorder.PlayerEvents);
		}

		[Fact]
		public void ChatRepositoryWrite_PostMessage_PublishesReceiveChatMessage() {
			var game = new TestGame();
			var recorder = new RecordingGameEventPublisher();
			var chatRepo = new ChatRepositoryWrite(game.Accessor, System.TimeProvider.System, recorder);

			chatRepo.PostMessage(new PostChatMessageCommand(Player1, "Hello everyone!"));

			Assert.Single(recorder.GameEvents);
			Assert.Equal(GameEventTypes.ReceiveChatMessage, recorder.GameEvents[0].EventType);
		}

		[Fact]
		public void GameTickEngine_IncrementWorldTick_PublishesGameTickCompleted() {
			var game = new TestGame();
			var recorder = new RecordingGameEventPublisher();
			var tickEngine = new GameTicks.GameTickEngine(
				NullLogger<GameTicks.GameTickEngine>.Instance,
				game.Accessor,
				game.GameDef,
				game.GameTickModuleRegistry,
				game.PlayerRepositoryWrite,
				System.TimeProvider.System,
				recorder
			);

			tickEngine.IncrementWorldTick();

			Assert.Single(recorder.GameEvents);
			Assert.Equal(GameEventTypes.GameTickCompleted, recorder.GameEvents[0].EventType);
		}

		[Fact]
		public void TradeRepositoryWrite_Accept_PublishesMarketOrderFilled() {
			var game = new TestGame(playerCount: 2);
			var recorder = new RecordingGameEventPublisher();
			var tradeWriteRepo = new TradeRepositoryWrite(
				game.Accessor, System.TimeProvider.System,
				NullNotificationService.Instance, recorder,
				game.ResourceRepository, game.ResourceRepositoryWrite);

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: null
			));

			tradeWriteRepo.Accept(new AcceptTradeOfferCommand(
				AcceptingPlayerId: Player2,
				OfferId: offerId
			));

			// Should have published to both players
			var fillEvents = recorder.PlayerEvents.FindAll(e => e.EventType == GameEventTypes.MarketOrderFilled);
			Assert.Equal(2, fillEvents.Count);
			Assert.Contains(fillEvents, e => e.PlayerId == Player1);
			Assert.Contains(fillEvents, e => e.PlayerId == Player2);
		}

		[Fact]
		public void InMemoryPlayerNotificationService_Push_PublishesReceiveAlert() {
			var recorder = new RecordingGameEventPublisher();
			var service = new InMemoryPlayerNotificationService(recorder);

			service.Push("player0", "Game started!", NotificationKind.GameEvent);

			Assert.Single(recorder.PlayerEvents);
			Assert.Equal(GameEventTypes.ReceiveAlert, recorder.PlayerEvents[0].EventType);
			Assert.Equal(Player1, recorder.PlayerEvents[0].PlayerId);
		}
	}
}
