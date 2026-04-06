using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AdminAuditLogTest {

		[Fact]
		public void Record_AddsEntry() {
			var log = new AdminAuditLog();
			log.Record("BanPlayer", "admin1", "Banned player X", "player1");

			var entries = log.GetPage(0, 10, "all");
			Assert.Single(entries);
			Assert.Equal("BanPlayer", entries[0].ActionType);
			Assert.Equal("admin1", entries[0].ActorUserId);
			Assert.Equal("Banned player X", entries[0].Description);
			Assert.Equal("player1", entries[0].TargetPlayerId);
			Assert.NotNull(entries[0].Id);
			Assert.Equal(12, entries[0].Id.Length);
		}

		[Fact]
		public void GetPage_ReturnsNewestFirst() {
			var log = new AdminAuditLog();
			log.Record("Action1", null, "First");
			log.Record("Action2", null, "Second");
			log.Record("Action3", null, "Third");

			var entries = log.GetPage(0, 10, "all");
			Assert.Equal(3, entries.Count);
			Assert.Equal("Third", entries[0].Description);
			Assert.Equal("First", entries[2].Description);
		}

		[Fact]
		public void GetPage_Paginates() {
			var log = new AdminAuditLog();
			for (int i = 0; i < 5; i++)
				log.Record("Action", null, $"Entry {i}");

			var page0 = log.GetPage(0, 2, "all");
			var page1 = log.GetPage(1, 2, "all");
			var page2 = log.GetPage(2, 2, "all");

			Assert.Equal(2, page0.Count);
			Assert.Equal(2, page1.Count);
			Assert.Single(page2);
		}

		[Fact]
		public void GetPage_FiltersbyActionType() {
			var log = new AdminAuditLog();
			log.Record("BanPlayer", null, "Ban 1");
			log.Record("UnbanPlayer", null, "Unban 1");
			log.Record("BanPlayer", null, "Ban 2");

			var bans = log.GetPage(0, 10, "BanPlayer");
			Assert.Equal(2, bans.Count);
			Assert.All(bans, e => Assert.Equal("BanPlayer", e.ActionType));

			var unbans = log.GetPage(0, 10, "UnbanPlayer");
			Assert.Single(unbans);
		}

		[Fact]
		public void GetPage_FilterIsCaseInsensitive() {
			var log = new AdminAuditLog();
			log.Record("BanPlayer", null, "Ban");

			var result = log.GetPage(0, 10, "banplayer");
			Assert.Single(result);
		}

		[Fact]
		public void GetTotal_ReturnsCountForAll() {
			var log = new AdminAuditLog();
			log.Record("A", null, "1");
			log.Record("B", null, "2");
			log.Record("A", null, "3");

			Assert.Equal(3, log.GetTotal("all"));
			Assert.Equal(3, log.GetTotal(null));
			Assert.Equal(3, log.GetTotal(""));
		}

		[Fact]
		public void GetTotal_FiltersbyActionType() {
			var log = new AdminAuditLog();
			log.Record("A", null, "1");
			log.Record("B", null, "2");
			log.Record("A", null, "3");

			Assert.Equal(2, log.GetTotal("A"));
			Assert.Equal(1, log.GetTotal("B"));
			Assert.Equal(0, log.GetTotal("C"));
		}

		[Fact]
		public void Record_EvictsOldestWhenOverMax() {
			var log = new AdminAuditLog();
			// MaxEntries is 1000
			for (int i = 0; i < 1002; i++)
				log.Record("Action", null, $"Entry {i}");

			Assert.Equal(1000, log.GetTotal("all"));
			// Oldest entries (0, 1) should be evicted; newest should be present
			var newest = log.GetPage(0, 1, "all");
			Assert.Equal("Entry 1001", newest[0].Description);
		}

		[Fact]
		public void Record_WithNullTargetPlayerId() {
			var log = new AdminAuditLog();
			log.Record("SetResources", "admin", "Set resources");

			var entries = log.GetPage(0, 10, "all");
			Assert.Single(entries);
			Assert.Null(entries[0].TargetPlayerId);
		}
	}
}
