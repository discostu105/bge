using System;
using System.Collections.Generic;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameStateJsonSerializerTest {
		/// <summary>
		/// Regression test: <see cref="WorldStateImmutable.Wars"/> is keyed by
		/// <see cref="AllianceWarId"/>. Without a custom JSON converter for that ID
		/// type, System.Text.Json throws NotSupportedException when serializing the
		/// dictionary because auto-synthesized converters don't support property-name
		/// writes. See <c>GameStateJsonSerializer.GetIdConverters()</c>.
		/// </summary>
		[Fact]
		public void Serialize_WorldStateWithNonEmptyWarsDict_DoesNotThrow() {
			var serializer = new GameStateJsonSerializer();
			var warId = AllianceWarIdFactory.NewAllianceWarId();
			var war = new AllianceWarImmutable(
				WarId: warId,
				AttackerAllianceId: AllianceIdFactory.NewAllianceId(),
				DefenderAllianceId: AllianceIdFactory.NewAllianceId(),
				Status: AllianceWarStatus.Active,
				DeclaredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
			);
			var world = new WorldStateImmutable(
				Players: new Dictionary<PlayerId, PlayerImmutable>(),
				GameTickState: new GameTickStateImmutable(new GameTick(0), DateTime.UtcNow),
				GameActionQueue: new List<GameActionImmutable>(),
				Wars: new Dictionary<AllianceWarId, AllianceWarImmutable> { { warId, war } }
			);

			var bytes = serializer.Serialize(world);

			Assert.NotNull(bytes);
			Assert.NotEmpty(bytes);
		}

		[Fact]
		public void Deserialize_RoundTripsWarsEntry() {
			var serializer = new GameStateJsonSerializer();
			var warId = AllianceWarIdFactory.NewAllianceWarId();
			var war = new AllianceWarImmutable(
				WarId: warId,
				AttackerAllianceId: AllianceIdFactory.NewAllianceId(),
				DefenderAllianceId: AllianceIdFactory.NewAllianceId(),
				Status: AllianceWarStatus.Active,
				DeclaredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
			);
			var world = new WorldStateImmutable(
				Players: new Dictionary<PlayerId, PlayerImmutable>(),
				GameTickState: new GameTickStateImmutable(new GameTick(0), DateTime.UtcNow),
				GameActionQueue: new List<GameActionImmutable>(),
				Wars: new Dictionary<AllianceWarId, AllianceWarImmutable> { { warId, war } }
			);

			var bytes = serializer.Serialize(world);
			var roundTripped = serializer.Deserialize(bytes);

			Assert.NotNull(roundTripped.Wars);
			Assert.Single(roundTripped.Wars!);
			Assert.True(roundTripped.Wars!.ContainsKey(warId));
			Assert.Equal(war.AttackerAllianceId, roundTripped.Wars![warId].AttackerAllianceId);
		}
	}
}
