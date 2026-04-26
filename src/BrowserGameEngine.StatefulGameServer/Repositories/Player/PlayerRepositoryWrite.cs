using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public enum JoinGameResult {
		Success,
		GameFull,
		AlreadyJoined
	}

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
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				Players[command.PlayerId].Name = command.NewName;
			}
		}

		public void SetWorkerGasPercent(SetWorkerGasPercentCommand command) {
			if (command.GasPercent < 0 || command.GasPercent > 100)
				throw new ArgumentOutOfRangeException(nameof(command.GasPercent), $"Gas percent must be between 0 and 100, got {command.GasPercent}.");
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				state.GasPercent = command.GasPercent;
			}
		}

		internal GameTick IncrementTick(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.CurrentGameTick = state.CurrentGameTick with { Tick = state.CurrentGameTick.Tick + 1 };
				return state.CurrentGameTick;
			}
		}

		public void ResetPlayer(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
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

		public void CreatePlayer(PlayerId playerId, string? userId = null, string playerType = "terran", int? protectionTicks = null) {
			lock (_lock) {
				CreatePlayerInternal(playerId, userId, playerType, protectionTicks);
			}
		}

		/// <summary>
		/// Atomically checks max player capacity and duplicate user, then creates the player.
		/// Returns a <see cref="JoinGameResult"/> indicating the outcome.
		/// </summary>
		public JoinGameResult TryCreatePlayer(PlayerId playerId, string? userId, string playerType, int maxPlayers) {
			lock (_lock) {
				if (userId != null && world.Players.Values.Any(p => p.UserId == userId))
					return JoinGameResult.AlreadyJoined;
				if (maxPlayers > 0 && world.Players.Count >= maxPlayers)
					return JoinGameResult.GameFull;
				CreatePlayerInternal(playerId, userId, playerType);
				return JoinGameResult.Success;
			}
		}

		private void CreatePlayerInternal(PlayerId playerId, string? userId, string playerType, int? protectionTicks = null) {
			if (world.PlayerExists(playerId)) throw new PlayerAlreadyExistsException(playerId);
			var settings = world.GameSettings;
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
						{ Id.ResDef("land"), settings.StartingLand },
						{ Id.ResDef("minerals"), settings.StartingMinerals },
						{ Id.ResDef("gas"), settings.StartingGas }
					},
					Assets = new HashSet<Asset> {
						new Asset {
							AssetDefId = Id.AssetDef("commandcenter"),
							Level = 1
						},
					},
					ProtectionTicksRemaining = protectionTicks ?? settings.ProtectionTicks
				}
			};
		}

		public void CompleteTutorial(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.TutorialCompleted = true;
			}
		}

		public void BanPlayer(PlayerId playerId) {
			world.GetPlayer(playerId).IsBanned = true;
		}

		public void UnbanPlayer(PlayerId playerId) {
			world.GetPlayer(playerId).IsBanned = false;
		}

		public void DeletePlayer(PlayerId playerId) {
			lock (_lock) {
				if (!world.PlayerExists(playerId)) return;
				var player = world.Players[playerId];
				player.ApiKeys.Clear();
				world.Players.Remove(playerId);
			}
		}
	}
}
