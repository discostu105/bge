using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceChatRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;
		private readonly IGameEventPublisher eventPublisher;

		public AllianceChatRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider, IGameEventPublisher eventPublisher) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
			this.eventPublisher = eventPublisher;
		}

		public AlliancePostId Post(PostAllianceChatCommand command) {
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
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime
				});
			}
			var authorName = world.GetPlayer(command.PlayerId).Name;
			eventPublisher.PublishToAlliance(command.AllianceId, GameEventTypes.ReceiveAllianceChatMessage, new {
				postId = postId.Id.ToString(),
				authorPlayerId = command.PlayerId.Id,
				authorName,
				body = command.Body,
				createdAt = timeProvider.GetUtcNow().UtcDateTime
			});
			return postId;
		}
	}
}
