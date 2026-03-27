using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly WorldState world;
		private readonly TimeProvider timeProvider;
		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepositoryWrite(WorldState world, TimeProvider timeProvider) {
			this.world = world;
			this.timeProvider = timeProvider;
		}

		public void ChangePlayerName(ChangePlayerNameCommand command) {
			Players[command.PlayerId].Name = command.NewName;
		}

		internal GameTick IncrementTick(PlayerId playerId) {
			lock (_lock) {
				var player = world.GetPlayer(playerId);
				player.State.CurrentGameTick = player.State.CurrentGameTick with { Tick = player.State.CurrentGameTick.Tick + 1 };
				return player.State.CurrentGameTick;
			}
		}

		public void CreatePlayer(PlayerId playerId) {
			lock (_lock) {
				if (world.PlayerExists(playerId)) throw new PlayerAlreadyExistsException(playerId);
				world.Players[playerId] = new Player() {
					Created = timeProvider.GetLocalNow().DateTime,
					PlayerId = playerId,
					Name = playerId.Id,
					PlayerType = Id.PlayerType("terran"),
					State = new PlayerState {
						LastGameTickUpdate = timeProvider.GetLocalNow().DateTime,
						CurrentGameTick = world.GameTickState.CurrentGameTick,
						Resources = new Dictionary<ResourceDefId, decimal> {
							{ Id.ResDef("land"), 50 },
							{ Id.ResDef("minerals"), 5000 },
							{ Id.ResDef("gas"), 3000 }
						},
						Assets = new HashSet<Asset> {
							new Asset {
								AssetDefId = Id.AssetDef("commandcenter"),
								Level = 1
							},
						}
					}
				};
			}
		}
	}
}