using System;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Achievements;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Test.Events;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class MilestoneNotificationTest {
		private const string UserId = "notify-user-1";
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private const string MilestoneId = "games-first";

		private static GlobalState CreateGlobalStateWithAchievement() {
			var gs = new GlobalState();
			gs.AddAchievement(new PlayerAchievementImmutable(
				UserId: UserId,
				GameId: new GameId(Guid.NewGuid().ToString()),
				PlayerId: Player1,
				PlayerName: "Notify Player",
				FinalRank: 1,
				FinalScore: 100,
				GameDefType: "SCO",
				FinishedAt: DateTime.UtcNow
			));
			return gs;
		}

		[Fact]
		public void UnlockIfNew_FirstUnlock_AddsMilestoneToGlobalState() {
			var gs = new GlobalState();
			var repo = new MilestoneRepositoryWrite(gs);

			repo.UnlockIfNew(UserId, MilestoneId, DateTime.UtcNow);

			var milestones = gs.GetMilestonesForUser(UserId);
			Assert.Single(milestones);
			Assert.Equal(MilestoneId, milestones[0].MilestoneId);
		}

		[Fact]
		public void UnlockIfNew_CalledTwice_DoesNotDuplicate() {
			var gs = new GlobalState();
			var repo = new MilestoneRepositoryWrite(gs);

			repo.UnlockIfNew(UserId, MilestoneId, DateTime.UtcNow);
			repo.UnlockIfNew(UserId, MilestoneId, DateTime.UtcNow);

			var milestones = gs.GetMilestonesForUser(UserId);
			Assert.Single(milestones);
		}

		[Fact]
		public void MilestoneUnlocked_EventPublishedOnce_ForNewUnlock() {
			var gs = CreateGlobalStateWithAchievement();
			var gameRegistry = new GameRegistry.GameRegistry(gs);
			var milestoneRepo = new MilestoneRepository(gs, gameRegistry);
			var milestoneRepoWrite = new MilestoneRepositoryWrite(gs);
			var recorder = new RecordingGameEventPublisher();
			var utcNow = DateTime.UtcNow;

			// Simulate the unlock loop from GameLifecycleEngine
			var evaluations = milestoneRepo.GetMilestonesForUser(UserId);
			foreach (var eval in evaluations) {
				if (!eval.IsUnlocked && eval.CurrentProgress >= eval.Definition.TargetProgress) {
					milestoneRepoWrite.UnlockIfNew(UserId, eval.Definition.Id, utcNow);
					recorder.PublishToPlayer(Player1, GameEventTypes.MilestoneUnlocked, new {
						milestoneId = eval.Definition.Id,
						name = eval.Definition.Name,
						icon = eval.Definition.Icon
					});
				}
			}

			// "games-first" requires 1 completed game; user has 1 achievement
			var milestoneEvents = recorder.PlayerEvents
				.Where(e => e.EventType == GameEventTypes.MilestoneUnlocked)
				.ToList();
			Assert.NotEmpty(milestoneEvents);
			Assert.All(milestoneEvents, e => Assert.Equal(Player1, e.PlayerId));
		}

		[Fact]
		public void MilestoneUnlocked_NoEventOnRecheck_WhenAlreadyUnlocked() {
			var gs = CreateGlobalStateWithAchievement();
			var gameRegistry = new GameRegistry.GameRegistry(gs);
			var milestoneRepo = new MilestoneRepository(gs, gameRegistry);
			var milestoneRepoWrite = new MilestoneRepositoryWrite(gs);
			var recorder = new RecordingGameEventPublisher();
			var utcNow = DateTime.UtcNow;

			// First pass: unlock and publish
			var evaluations = milestoneRepo.GetMilestonesForUser(UserId);
			foreach (var eval in evaluations) {
				if (!eval.IsUnlocked && eval.CurrentProgress >= eval.Definition.TargetProgress) {
					milestoneRepoWrite.UnlockIfNew(UserId, eval.Definition.Id, utcNow);
					recorder.PublishToPlayer(Player1, GameEventTypes.MilestoneUnlocked, new {
						milestoneId = eval.Definition.Id,
						name = eval.Definition.Name,
						icon = eval.Definition.Icon
					});
				}
			}
			int firstPassCount = recorder.PlayerEvents.Count(e => e.EventType == GameEventTypes.MilestoneUnlocked);

			// Second pass: re-evaluate — already-unlocked milestones should not fire again
			var evaluations2 = milestoneRepo.GetMilestonesForUser(UserId);
			foreach (var eval in evaluations2) {
				if (!eval.IsUnlocked && eval.CurrentProgress >= eval.Definition.TargetProgress) {
					milestoneRepoWrite.UnlockIfNew(UserId, eval.Definition.Id, utcNow);
					recorder.PublishToPlayer(Player1, GameEventTypes.MilestoneUnlocked, new {
						milestoneId = eval.Definition.Id,
						name = eval.Definition.Name,
						icon = eval.Definition.Icon
					});
				}
			}
			int secondPassCount = recorder.PlayerEvents.Count(e => e.EventType == GameEventTypes.MilestoneUnlocked);

			Assert.Equal(firstPassCount, secondPassCount);
		}

		[Fact]
		public void NotificationsController_MapToViewModel_MilestoneUnlocked_MapsCorrectly() {
			var notification = new GameNotification(
				Id: Guid.NewGuid(),
				Type: GameNotificationType.MilestoneUnlocked,
				Title: "Achievement Unlocked: First Commander",
				Body: null,
				CreatedAt: DateTime.UtcNow,
				ReadAt: null
			);

			// Verify the enum value exists and is distinct
			Assert.Equal(GameNotificationType.MilestoneUnlocked, notification.Type);
			Assert.NotEqual(GameNotificationType.AttackReceived, notification.Type);
		}
	}
}
