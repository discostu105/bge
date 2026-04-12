using System;

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
}
