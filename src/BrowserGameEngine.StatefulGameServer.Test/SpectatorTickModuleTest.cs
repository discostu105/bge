using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test;

public class SpectatorTickModuleTest
{
	private const string TestGameId = "default";

	private sealed class RecordingSpectatorEventPublisher : ISpectatorEventPublisher
	{
		public List<(GameId GameId, object Snapshot)> Calls { get; } = new();

		public void PublishSnapshot(GameId gameId, object snapshot)
			=> Calls.Add((gameId, snapshot));
	}

	private static (TestGame game, SpectatorTickModule module, RecordingSpectatorEventPublisher publisher) Setup(int playerCount = 3)
	{
		var game = new TestGame(playerCount);

		var record = new GameRecordImmutable(
			new GameId(TestGameId), "Test Game", "sco", GameStatus.Active,
			DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(30));
		game.GlobalState.AddGame(record);

		var publisher = new RecordingSpectatorEventPublisher();
		var module = new SpectatorTickModule(game.Accessor, game.GlobalState, game.GameDef, publisher);

		return (game, module, publisher);
	}

	private static JsonElement ToJson(object obj)
		=> JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(obj));

	[Fact]
	public void CalculateTick_PublishesSnapshotOnFirstCall()
	{
		var (game, module, publisher) = Setup();

		module.CalculateTick(game.Player1);

		Assert.Single(publisher.Calls);
		var json = ToJson(publisher.Calls[0].Snapshot);
		Assert.Equal(TestGameId, json.GetProperty("gameId").GetString());
		Assert.Equal("Test Game", json.GetProperty("gameName").GetString());
		Assert.Equal("Active", json.GetProperty("gameStatus").GetString());
		Assert.True(json.GetProperty("topPlayers").GetArrayLength() > 0);
	}

	[Fact]
	public void CalculateTick_DeduplicatesWithinSameTick()
	{
		var (game, module, publisher) = Setup(playerCount: 3);

		// Call once per player — all on the same tick
		module.CalculateTick(PlayerIdFactory.Create("player0"));
		module.CalculateTick(PlayerIdFactory.Create("player1"));
		module.CalculateTick(PlayerIdFactory.Create("player2"));

		// Should only publish once regardless of how many players were ticked
		Assert.Single(publisher.Calls);
	}

	[Fact]
	public void CalculateTick_PublishesTopPlayersRankedByScore()
	{
		var (game, module, publisher) = Setup(playerCount: 3);

		module.CalculateTick(game.Player1);

		var json = ToJson(publisher.Calls[0].Snapshot);
		var players = json.GetProperty("topPlayers");
		Assert.True(players.GetArrayLength() > 0);

		// Players should be ranked 1, 2, 3, ...
		int i = 0;
		decimal prevScore = decimal.MaxValue;
		foreach (var p in players.EnumerateArray())
		{
			Assert.Equal(i + 1, p.GetProperty("rank").GetInt32());
			var score = p.GetProperty("score").GetDecimal();
			Assert.True(score <= prevScore);
			prevScore = score;
			i++;
		}
	}

	[Fact]
	public void BuildSnapshot_LimitsToTop20Players()
	{
		var game = new TestGame(25);
		var record = new GameRecordImmutable(
			new GameId(TestGameId), "Big Game", "sco", GameStatus.Active,
			DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(30));
		game.GlobalState.AddGame(record);

		var publisher = new RecordingSpectatorEventPublisher();
		var module = new SpectatorTickModule(game.Accessor, game.GlobalState, game.GameDef, publisher);

		var snapshot = module.BuildSnapshot(new GameId(TestGameId), record, tick: 1);
		var json = ToJson(snapshot);

		Assert.True(json.GetProperty("topPlayers").GetArrayLength() <= 20);
	}

	[Fact]
	public void CalculateTick_DoesNotPublishIfNoGameRecord()
	{
		var game = new TestGame(1);
		// Do NOT add any game record to GlobalState

		var publisher = new RecordingSpectatorEventPublisher();
		var module = new SpectatorTickModule(game.Accessor, game.GlobalState, game.GameDef, publisher);

		module.CalculateTick(game.Player1);

		Assert.Empty(publisher.Calls);
	}
}
