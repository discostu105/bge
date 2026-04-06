using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public enum ReportStatus { Pending, Resolved, Dismissed }

	public record Report(
		string Id,
		DateTime CreatedAt,
		string ReporterUserId,
		string TargetUserId,
		string Reason,
		string? Details,
		ReportStatus Status,
		string? ResolvedByUserId,
		string? ResolutionNote,
		DateTime? ResolvedAt
	);

	public class ReportStore {
		private readonly object _lock = new();
		private readonly List<Report> _reports = new();

		public void Add(Report report) {
			lock (_lock) _reports.Add(report);
		}

		public IReadOnlyList<Report> GetAll() {
			lock (_lock) return _reports.OrderByDescending(r => r.CreatedAt).ToList();
		}

		public Report? GetById(string id) {
			lock (_lock) return _reports.FirstOrDefault(r => r.Id == id);
		}

		public void Update(Report old, Report updated) {
			lock (_lock) {
				var idx = _reports.IndexOf(old);
				if (idx >= 0) _reports[idx] = updated;
			}
		}
	}
}
