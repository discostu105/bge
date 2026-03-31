using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>Simple controllable time provider for tests.</summary>
	internal class ManualTimeProvider : TimeProvider {
		private DateTimeOffset _now;

		public ManualTimeProvider(DateTimeOffset start) {
			_now = start;
		}

		public override DateTimeOffset GetUtcNow() => _now;

		public void Advance(TimeSpan span) {
			_now = _now.Add(span);
		}
	}

	public class FogOfWarRepositoryTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		private static (FogOfWarRepository fog, ManualTimeProvider time, SpyRepositoryWrite spyWrite, SpyRepository spyRepo) CreateFogComponents() {
			var time = new ManualTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
			var world = new TestWorldStateFactory().CreateDevWorldState(2).ToMutable();
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var accessor = new SingletonWorldStateAccessor(world);
			var spyRepo = new SpyRepository(accessor, time);
			var resourceRepo = new ResourceRepository(accessor, gameDef);
			var resourceRepoWrite = new ResourceRepositoryWrite(accessor, resourceRepo, gameDef);
			var techRepo = new TechRepository(accessor, gameDef);
			var spyRepoWrite = new SpyRepositoryWrite(accessor, spyRepo, resourceRepoWrite, techRepo, gameDef, time);
			var fog = new FogOfWarRepository(accessor, time);
			return (fog, time, spyRepoWrite, spyRepo);
		}

		[Fact]
		public void GetValidIntel_WithinWindow_ReturnsIntel() {
			var (fog, _, spyWrite, _) = CreateFogComponents();
			spyWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			var intel = fog.GetValidIntel(Player1, Player2);

			Assert.NotNull(intel);
			Assert.Equal(Player2, intel!.TargetPlayerId);
		}

		[Fact]
		public void GetValidIntel_AfterWindowExpired_ReturnsNull() {
			var (fog, time, spyWrite, _) = CreateFogComponents();
			spyWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			// Advance past the 30-minute visibility window
			time.Advance(TimeSpan.FromMinutes(31));

			var intel = fog.GetValidIntel(Player1, Player2);

			Assert.Null(intel);
		}

		[Fact]
		public void GetValidIntel_NoSpyExecuted_ReturnsNull() {
			var (fog, _, _, _) = CreateFogComponents();

			var intel = fog.GetValidIntel(Player1, Player2);

			Assert.Null(intel);
		}

		[Fact]
		public void ExecuteSpy_PersistsLastSpyResult_InPlayerState() {
			var time = new ManualTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
			var game = new TestGame(playerCount: 2);
			// Use TestGame's SpyRepositoryWrite (uses TimeProvider.System) to confirm the field is populated
			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			var player1State = game.PlayerRepository.Get(Player1).State;
			Assert.True(player1State.LastSpyResults != null && player1State.LastSpyResults.ContainsKey(Player2.ToString()));
			var stored = player1State.LastSpyResults![Player2.ToString()];
			Assert.Equal(Player2, stored.TargetPlayerId);
			Assert.True(stored.CooldownExpiresAt > stored.ReportTime);
		}
	}
}
