using BrowserGameEngine.StatefulGameServer;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class SingletonWorldStateAccessorTest {
		[Fact]
		public void Accessor_ReturnsInjectedWorldState() {
			var game = new TestGame();
			var accessor = new SingletonWorldStateAccessor(game.World);
			Assert.Same(game.World, accessor.WorldState);
		}
	}
}
