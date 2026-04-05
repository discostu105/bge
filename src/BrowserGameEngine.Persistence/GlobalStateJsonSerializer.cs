using BrowserGameEngine.GameModel;
using System.Text.Json;

namespace BrowserGameEngine.Persistence {
	public class GlobalStateJsonSerializer {
		private static readonly JsonSerializerOptions Options = new JsonSerializerOptions();

		public byte[] Serialize(GlobalStateImmutable state) {
			return JsonSerializer.SerializeToUtf8Bytes(state, Options);
		}

		public GlobalStateImmutable Deserialize(byte[] blob) {
			return JsonSerializer.Deserialize<GlobalStateImmutable>(blob, Options)!;
		}
	}
}
