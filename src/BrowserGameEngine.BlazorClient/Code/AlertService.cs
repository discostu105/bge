using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.BlazorClient.Code {
	public enum AlertType { Info, Warning, Danger }

	public class Alert {
		public Guid Id { get; } = Guid.NewGuid();
		public string Message { get; }
		public AlertType Type { get; }
		public DateTime CreatedAt { get; } = DateTime.UtcNow;
		public bool Seen { get; set; }

		public Alert(string message, AlertType type = AlertType.Info) {
			Message = message;
			Type = type;
		}
	}

	public class AlertService {
		private readonly List<Alert> _alerts = new();
		public event Action? AlertsChanged;

		public IReadOnlyList<Alert> Alerts {
			get { lock (_alerts) return _alerts.ToList(); }
		}

		public int UnseenCount {
			get { lock (_alerts) return _alerts.Count(a => !a.Seen); }
		}

		public void AddAlert(string message, AlertType type = AlertType.Info) {
			lock (_alerts) {
				_alerts.Add(new Alert(message, type));
			}
			AlertsChanged?.Invoke();
		}

		public void Dismiss(Guid id) {
			lock (_alerts) {
				var alert = _alerts.FirstOrDefault(a => a.Id == id);
				if (alert != null) _alerts.Remove(alert);
			}
			AlertsChanged?.Invoke();
		}

		public void MarkAllSeen() {
			lock (_alerts) {
				foreach (var a in _alerts) a.Seen = true;
			}
			AlertsChanged?.Invoke();
		}
	}
}
