using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class RealisticDevScenarioTest {
		[Fact]
		public void Passes_WorldStateVerifier() {
			var gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
			var state = RealisticDevScenario.Build();
			new WorldStateVerifier().Verify(gameDef, state);
		}

		[Fact]
		public void Has_Expected_Shape() {
			var state = RealisticDevScenario.Build();

			Assert.Equal(20, state.Players.Count);
			Assert.NotNull(state.Alliances);
			Assert.Equal(4, state.Alliances!.Count);
			Assert.NotNull(state.Wars);
			Assert.Single(state.Wars!);
			Assert.NotNull(state.ChatMessages);
			Assert.Equal(15, state.ChatMessages!.Count);
			Assert.NotNull(state.MarketOrders);
			Assert.Equal(6, state.MarketOrders!.Count);
			Assert.NotNull(state.TradeOffers);
			Assert.Equal(3, state.TradeOffers!.Count);

			var unaffiliated = state.Players.Values.Count(p => p.AllianceId == null);
			Assert.Equal(2, unaffiliated);

			var activeElections = state.Alliances.Values.Count(a => a.ActiveElection != null);
			Assert.Equal(1, activeElections);

			var allianceWithInvite = state.Alliances.Values.Single(a => a.Invites != null && a.Invites.Count > 0);
			Assert.Equal("Nomad Guild", allianceWithInvite.Name);
		}

		[Fact]
		public void StarcraftFactory_Produces_Same_Realistic_State() {
			IWorldStateFactory factory = new StarcraftOnlineWorldStateFactory();
			var state = factory.CreateRealisticDevWorldState();
			Assert.Equal(20, state.Players.Count);
		}
	}
}
