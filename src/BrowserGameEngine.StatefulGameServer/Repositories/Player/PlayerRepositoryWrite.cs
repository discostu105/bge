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
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;
		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
		}

		public void ChangePlayerName(ChangePlayerNameCommand command) {
			lock (_lock) {
				Players[command.PlayerId].Name = command.NewName;
			}
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

		public void CaptureWorkers(PlayerId playerId, int count) {
			lock (_lock) {
				var state = world.GetPlayer(playerId).State;
				int mineralRemoved = Math.Min(count, state.MineralWorkers);
				state.MineralWorkers -= mineralRemoved;
				int remaining = count - mineralRemoved;
				int gasRemoved = Math.Min(remaining, state.GasWorkers);
				state.GasWorkers -= gasRemoved;
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

		public void UpdateLastOnline(PlayerId playerId, DateTime time) {
			if (!world.PlayerExists(playerId)) return;
			world.Players[playerId].LastOnline = time;
		}

		public void CreatePlayer(PlayerId playerId, string? userId = null, string playerType = "terran") {
			lock (_lock) {
				if (world.PlayerExists(playerId)) throw new PlayerAlreadyExistsException(playerId);
				world.Players[playerId] = new Player() {
					Created = timeProvider.GetLocalNow().DateTime,
					PlayerId = playerId,
					Name = playerId.Id,
					PlayerType = Id.PlayerType(playerType),
					UserId = userId,
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
						},
						ProtectionTicksRemaining = 480  // 4 hours at 30s/tick
					}
				};
			}
		}

		public void BanPlayer(PlayerId playerId) {
			lock (_lock) {
				world.GetPlayer(playerId).IsBanned = true;
			}
		}

		public void UnbanPlayer(PlayerId playerId) {
			lock (_lock) {
				world.GetPlayer(playerId).IsBanned = false;
			}
		}

		public void DeletePlayer(PlayerId playerId) {
			lock (_lock) {
				if (!world.PlayerExists(playerId)) return;
				var player = world.Players[playerId];
				player.ApiKeyHash = null;
				world.Players.Remove(playerId);
			}
		}
	}
}