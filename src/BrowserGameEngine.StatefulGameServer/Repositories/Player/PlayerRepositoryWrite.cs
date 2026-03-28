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

		public void AssignWorkers(AssignWorkersCommand command, int totalWorkers) {
			if (command.MineralWorkers < 0 || command.GasWorkers < 0)
				throw new ArgumentOutOfRangeException("Worker counts cannot be negative.");
			if (command.MineralWorkers + command.GasWorkers > totalWorkers)
				throw new ArgumentOutOfRangeException($"Cannot assign {command.MineralWorkers + command.GasWorkers} workers: only {totalWorkers} available.");
			lock (_lock) {
				var state = world.GetPlayer(command.PlayerId).State;
				state.MineralWorkers = command.MineralWorkers;
				state.GasWorkers = command.GasWorkers;
			}
		}

		public void GrantEmergencyWorkers(PlayerId playerId) {
			lock (_lock) {
				var state = world.GetPlayer(playerId).State;
				state.MineralWorkers = 1;
				state.GasWorkers = 1;
			}
		}

		internal GameTick IncrementTick(PlayerId playerId) {
			lock (_lock) {
				var player = world.GetPlayer(playerId);
				player.State.CurrentGameTick = player.State.CurrentGameTick with { Tick = player.State.CurrentGameTick.Tick + 1 };
				return player.State.CurrentGameTick;
			}
		}

		public void ResetPlayer(PlayerId playerId) {
			lock (_lock) {
				var state = world.GetPlayer(playerId).State;
				state.Resources = new Dictionary<ResourceDefId, decimal> {
					{ Id.ResDef("land"), 50 },
					{ Id.ResDef("minerals"), 5000 },
					{ Id.ResDef("gas"), 3000 }
				};
				state.Assets = new HashSet<Asset> {
					new Asset {
						AssetDefId = Id.AssetDef("commandcenter"),
						Level = 1
					}
				};
				state.Units = new List<Unit>();
				state.CurrentGameTick = world.GameTickState.CurrentGameTick;
				state.LastGameTickUpdate = timeProvider.GetLocalNow().DateTime;
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