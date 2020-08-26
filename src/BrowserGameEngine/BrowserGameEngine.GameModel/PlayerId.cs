using System;

namespace BrowserGameEngine.GameModel {
	public record PlayerId(string Id) {
		public override string ToString() => $"PlayerId{Id}";
	}

	public static class PlayerIdFactory {
		public static PlayerId Create(string id) {
			return new PlayerId(id);
		}
	}
}

// workaround for roslyn bug: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
namespace System.Runtime.CompilerServices {
	public class IsExternalInit { }
}