using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class CounterIntelTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		/// <summary>Creates a 2-player world state with counter-intel techs pre-unlocked for Player2.</summary>
		private static WorldStateImmutable CreateWorldWithCounterIntel(IList<string> player2Techs) {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(2);
			var players = baseState.Players.ToDictionary(
				kv => kv.Key,
				kv => kv.Key == Player2
					? kv.Value with {
						State = kv.Value.State with {
							UnlockedTechs = player2Techs
						}
					}
					: kv.Value
			);
			return baseState with { Players = players };
		}

		[Fact]
		public void ExecuteSpy_NoCounterIntelTech_NeverDetected() {
			// With no counter-intel tech, detection chance = 0, so spy is never detected
			int detectedCount = 0;
			for (int i = 0; i < 20; i++) {
				var game = new TestGame(playerCount: 2);
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));
				var logs = game.SpyRepository.GetDetectedSpyAttempts(Player2);
				detectedCount += logs.Count;
			}
			Assert.Equal(0, detectedCount);
		}

		[Fact]
		public void ExecuteSpy_WithCounterIntelTech_SomeAttemptsDetected() {
			// With 50% detection (all 3 tiers), over 100 attempts expect significant detection
			int detected = 0;
			int total = 100;
			var techs = new List<string> { "counter-intel-basic", "counter-intel-advanced", "counter-intel-mastery" };
			for (int i = 0; i < total; i++) {
				var initialState = CreateWorldWithCounterIntel(techs);
				var game = new TestGame(initialState);
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));
				var logs = game.SpyRepository.GetDetectedSpyAttempts(Player2);
				detected += logs.Count;
			}

			// With 50% detection rate over 100 attempts, expect 30–70 (very safe range)
			Assert.InRange(detected, 20, 80);
		}

		[Fact]
		public void ExecuteSpy_DetectedAttempt_HasCorrectAttackerAndActionType() {
			// Run with 50% detection until we get at least one detection
			var techs = new List<string> { "counter-intel-basic", "counter-intel-advanced", "counter-intel-mastery" };
			SpyAttemptLog? foundLog = null;
			for (int attempt = 0; attempt < 200 && foundLog == null; attempt++) {
				var initialState = CreateWorldWithCounterIntel(techs);
				var game = new TestGame(initialState);
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));
				var logs = game.SpyRepository.GetDetectedSpyAttempts(Player2);
				foundLog = logs.FirstOrDefault();
			}

			Assert.NotNull(foundLog);
			Assert.Equal(Player1, foundLog!.AttackerPlayerId);
			Assert.Equal("Spy", foundLog.ActionType);
			Assert.True(foundLog.Detected);
		}

		[Fact]
		public void GetDetectedSpyAttempts_ReturnsOnlyDetectedAttempts() {
			// Spy attempts without counter-intel are never detected
			// So after running 5 spies (bypassing cooldown via fresh games), none should be detected
			int total = 5;
			int detected = 0;
			for (int i = 0; i < total; i++) {
				var game = new TestGame(playerCount: 2);
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));
				var logs = game.SpyRepository.GetDetectedSpyAttempts(Player2);
				detected += logs.Count;
			}
			Assert.Equal(0, detected);
		}

		[Fact]
		public void CounterIntelTechNodes_HaveCorrectCumulativeDetectionProbability() {
			// All three counter-intel tech tiers should sum to 0.50 detection probability
			var techs = new List<string> { "counter-intel-basic", "counter-intel-advanced", "counter-intel-mastery" };
			var initialState = CreateWorldWithCounterIntel(techs);
			var game = new TestGame(initialState);

			var totalDetection = game.TechRepository.GetTotalEffectValue(Player2, TechEffectType.CounterIntelDetection);
			Assert.Equal(0.50m, totalDetection);
		}

		[Fact]
		public void CounterIntelTier1_HasCorrectDetectionProbability() {
			var techs = new List<string> { "counter-intel-basic" };
			var initialState = CreateWorldWithCounterIntel(techs);
			var game = new TestGame(initialState);

			var detection = game.TechRepository.GetTotalEffectValue(Player2, TechEffectType.CounterIntelDetection);
			Assert.Equal(0.15m, detection);
		}

		[Fact]
		public void CounterIntelTier2_HasCorrectCumulativeDetectionProbability() {
			var techs = new List<string> { "counter-intel-basic", "counter-intel-advanced" };
			var initialState = CreateWorldWithCounterIntel(techs);
			var game = new TestGame(initialState);

			var detection = game.TechRepository.GetTotalEffectValue(Player2, TechEffectType.CounterIntelDetection);
			Assert.Equal(0.30m, detection);
		}
	}
}
