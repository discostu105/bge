using System;

namespace BrowserGameEngine.GameModel {
	public record PlayerId(string Id) {
		public override string ToString() => Id;
	}

	public static class PlayerIdFactory {
		public static PlayerId Create(string id) {
			return new PlayerId(id);
		}
	}
}
