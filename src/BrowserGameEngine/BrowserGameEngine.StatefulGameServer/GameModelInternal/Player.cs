using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal
{
	internal class Player {
		public PlayerId PlayerId { get; init; }
		public string? Name { get; set; }
		public DateTime Created { get; init; }
		public PlayerState State { get; init; }
	}

	internal static class PlayerExtensions {
		internal static PlayerImmutable ToImmutable(this Player player) {
			return new PlayerImmutable {
				PlayerId = player.PlayerId,
				Name = player.Name,
				Created = player.Created,
				State = player.State.ToImmutable()
			};
		}

		internal static Player ToMutable(this PlayerImmutable playerImmutable) {
			return new Player {
				PlayerId = playerImmutable.PlayerId,
				Name = playerImmutable.Name,
				Created = playerImmutable.Created,
				State = playerImmutable.State.ToMutable()
			};
		}
	}
}
