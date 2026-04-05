using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Repositories.Chat;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceChatRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;
		private readonly IGameEventPublisher eventPublisher;
		private static readonly TimeSpan RateLimitInterval = TimeSpan.FromSeconds(2);
		private readonly ConcurrentDictionary<PlayerId, DateTime> lastMessageTime = new();

		public AllianceChatRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider, IGameEventPublisher eventPublisher) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
			this.eventPublisher = eventPublisher;
		}

		public AlliancePostId Post(PostAllianceChatCommand command) {
			var now = timeProvider.GetUtcNow().UtcDateTime;
			if (lastMessageTime.TryGetValue(command.PlayerId, out var lastTime)
				&& (now - lastTime) < RateLimitInterval) {
				throw new ChatRateLimitException();
			}

			var postId = AlliancePostIdFactory.NewPostId();
			lock (_lock) {
				var alliance = world.GetAlliance(command.AllianceId);
				if (!alliance.Members.Exists(m => m.PlayerId == command.PlayerId && !m.IsPending)) {
					throw new NotAllianceMemberException();
				}
				alliance.Posts.Add(new AlliancePost {
					PostId = postId,
					AllianceId = command.AllianceId,
					AuthorPlayerId = command.PlayerId,
					Body = command.Body,
					CreatedAt = now
				});
			}
			lastMessageTime[command.PlayerId] = now;
			var player = world.GetPlayer(command.PlayerId);
			eventPublisher.PublishToAlliance(command.AllianceId, GameEventTypes.ReceiveAllianceChatMessage, new {
				postId = postId.Id.ToString(),
				authorPlayerId = command.PlayerId.Id,
				authorName = player.Name,
				playerType = player.PlayerType.Id,
				body = command.Body,
				createdAt = now
			});
			return postId;
		}
	}
}
