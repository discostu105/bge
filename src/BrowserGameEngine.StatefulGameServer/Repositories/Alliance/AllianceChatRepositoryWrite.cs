using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceChatRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;

		public AllianceChatRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
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
			return postId;
		}
	}
}
