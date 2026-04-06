using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class ReportStoreTest {

		private static Report CreateReport(string id = "abc123", string reporter = "user1", string target = "user2",
			ReportStatus status = ReportStatus.Pending, DateTime? createdAt = null) {
			return new Report(
				Id: id,
				CreatedAt: createdAt ?? DateTime.UtcNow,
				ReporterUserId: reporter,
				TargetUserId: target,
				Reason: "Cheating",
				Details: "Some details",
				Status: status,
				ResolvedByUserId: null,
				ResolutionNote: null,
				ResolvedAt: null
			);
		}

		[Fact]
		public void Add_And_GetAll_ReturnsReports() {
			var store = new ReportStore();
			store.Add(CreateReport("r1"));
			store.Add(CreateReport("r2"));

			var all = store.GetAll();
			Assert.Equal(2, all.Count);
		}

		[Fact]
		public void GetAll_ReturnsNewestFirst() {
			var store = new ReportStore();
			store.Add(CreateReport("r1", createdAt: new DateTime(2024, 1, 1)));
			store.Add(CreateReport("r2", createdAt: new DateTime(2024, 6, 1)));
			store.Add(CreateReport("r3", createdAt: new DateTime(2024, 3, 1)));

			var all = store.GetAll();
			Assert.Equal("r2", all[0].Id);
			Assert.Equal("r3", all[1].Id);
			Assert.Equal("r1", all[2].Id);
		}

		[Fact]
		public void GetById_ReturnsCorrectReport() {
			var store = new ReportStore();
			store.Add(CreateReport("r1"));
			store.Add(CreateReport("r2"));

			var result = store.GetById("r1");
			Assert.NotNull(result);
			Assert.Equal("r1", result!.Id);
		}

		[Fact]
		public void GetById_ReturnsNull_ForMissingId() {
			var store = new ReportStore();
			store.Add(CreateReport("r1"));

			Assert.Null(store.GetById("nonexistent"));
		}

		[Fact]
		public void Update_ReplacesReport() {
			var store = new ReportStore();
			var original = CreateReport("r1");
			store.Add(original);

			var updated = original with {
				Status = ReportStatus.Resolved,
				ResolvedByUserId = "admin",
				ResolutionNote = "Confirmed cheating",
				ResolvedAt = DateTime.UtcNow,
			};
			store.Update(original, updated);

			var result = store.GetById("r1");
			Assert.Equal(ReportStatus.Resolved, result!.Status);
			Assert.Equal("admin", result.ResolvedByUserId);
			Assert.Equal("Confirmed cheating", result.ResolutionNote);
		}

		[Fact]
		public void Update_DoesNothing_WhenOldNotFound() {
			var store = new ReportStore();
			var r1 = CreateReport("r1");
			store.Add(r1);

			var phantom = CreateReport("phantom");
			var updated = phantom with { Status = ReportStatus.Dismissed };
			store.Update(phantom, updated);

			// r1 should be unchanged
			Assert.Equal(ReportStatus.Pending, store.GetById("r1")!.Status);
			Assert.Single(store.GetAll());
		}

		[Fact]
		public void GetAll_ReturnsEmpty_WhenNoReports() {
			var store = new ReportStore();
			Assert.Empty(store.GetAll());
		}
	}
}
