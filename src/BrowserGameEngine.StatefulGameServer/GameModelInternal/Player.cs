using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class Player {
		public required PlayerId PlayerId { get; init; }
		public required PlayerTypeDefId PlayerType { get; init; }
		public required string Name { get; set; }
		public DateTime Created { get; init; }
		public required PlayerState State { get; init; }
		public string? UserId { get; set; }
		public string? ApiKeyHash { get; set; }
		public DateTime? LastOnline { get; set; }
	}

	internal static class PlayerExtensions {
		internal static PlayerImmutable ToImmutable(this Player player) {
			return new PlayerImmutable (
				PlayerId: player.PlayerId,
				PlayerType: player.PlayerType,
				Name: player.Name,
				Created: player.Created,
				State: player.State.ToImmutable(),
				UserId: player.UserId,
				ApiKeyHash: player.ApiKeyHash,
				LastOnline: player.LastOnline
			);
		}

		internal static Player ToMutable(this PlayerImmutable playerImmutable) {
			return new Player {
				PlayerId = playerImmutable.PlayerId,
				PlayerType = playerImmutable.PlayerType,
				Name = playerImmutable.Name,
				Created = playerImmutable.Created,
				State = playerImmutable.State.ToMutable(),
				UserId = playerImmutable.UserId,
				ApiKeyHash = playerImmutable.ApiKeyHash,
				LastOnline = playerImmutable.LastOnline
			};
		}
	}
}
