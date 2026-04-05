using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// xUnit collection that shares a single <see cref="BgeWebApplicationFactory"/> instance
	/// across all integration test classes, avoiding multiple server startups (and multiple
	/// DotNetRuntime metric collector registrations) within the same test process.
	/// </summary>
	[CollectionDefinition("Integration")]
	public class IntegrationTestCollection : ICollectionFixture<BgeWebApplicationFactory> { }
}
