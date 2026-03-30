using BrowserGameEngine.GameModel;
using System.Text.Json;

namespace BrowserGameEngine.Persistence {
	public class GlobalStateJsonSerializer {
		public byte[] Serialize(GlobalStateImmutable state) {
			return JsonSerializer.SerializeToUtf8Bytes(state, GetOptions());
		}

		public GlobalStateImmutable Deserialize(byte[] blob) {
			return JsonSerializer.Deserialize<GlobalStateImmutable>(blob, GetOptions())!;
		}

		private static JsonSerializerOptions GetOptions() => new() { WriteIndented = true };
	}
}
