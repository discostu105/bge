using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class ColonizeTest {
		[Fact]
		public void GetCostForTiles_SingleTile_MatchesGetCostPerLand() {
			for (decimal land = 0; land <= 1000; land += 17) {
				var single = ColonizeRepositoryWrite.GetCostForTiles(land, 1);
				var perLand = ColonizeRepositoryWrite.GetCostPerLand(land);
				Assert.Equal(perLand, single);
			}
		}

		[Fact]
		public void GetCostForTiles_LargeLand_MatchesClosedForm() {
			// L=100, N=10: tiles cost 100/4..109/4 = 25, 25.25, ..., 27.25
			// Sum = 10*(2*100 + 9)/8 = 10*209/8 = 261.25
			Assert.Equal(261.25m, ColonizeRepositoryWrite.GetCostForTiles(100m, 10));
			// L=1000, N=100: 100*(2*1000 + 99)/8 = 100*2099/8 = 26237.5
			Assert.Equal(26237.5m, ColonizeRepositoryWrite.GetCostForTiles(1000m, 100));
		}

		[Fact]
		public void GetCostForTiles_BoundaryAtFour_NoFloorEffect() {
			// L=4, N=1: cost = 4/4 = 1
			Assert.Equal(1m, ColonizeRepositoryWrite.GetCostForTiles(4m, 1));
			// L=4, N=4: tiles 4,5,6,7 → 4/4 + 5/4 + 6/4 + 7/4 = 22/4 = 5.5
			Assert.Equal(5.5m, ColonizeRepositoryWrite.GetCostForTiles(4m, 4));
		}

		[Fact]
		public void GetCostForTiles_SmallLand_FloorApplied() {
			// L=0, N=4: all four tiles floored to 1 (since (0+k)/4 < 1 for k<4)
			Assert.Equal(4m, ColonizeRepositoryWrite.GetCostForTiles(0m, 4));
			// L=0, N=8: first four floored to 1 each, then 4/4 + 5/4 + 6/4 + 7/4 = 22/4 = 5.5 → total 9.5
			Assert.Equal(9.5m, ColonizeRepositoryWrite.GetCostForTiles(0m, 8));
			// L=2, N=5: tiles k=0..4 → max(1, 2/4)=1, max(1,3/4)=1, max(1,4/4)=1, max(1,5/4)=1.25, max(1,6/4)=1.5
			// Sum = 1 + 1 + 1 + 1.25 + 1.5 = 5.75
			Assert.Equal(5.75m, ColonizeRepositoryWrite.GetCostForTiles(2m, 5));
		}

		[Fact]
		public void GetCostForTiles_ZeroAmount_ReturnsZero() {
			Assert.Equal(0m, ColonizeRepositoryWrite.GetCostForTiles(0m, 0));
			Assert.Equal(0m, ColonizeRepositoryWrite.GetCostForTiles(1000m, 0));
		}

		[Fact]
		public void GetCostForTiles_NegativeAmount_Throws() {
			Assert.Throws<ArgumentOutOfRangeException>(() => ColonizeRepositoryWrite.GetCostForTiles(100m, -1));
		}

		[Fact]
		public void GetCostForTiles_Monotonic() {
			// Cost should strictly increase with N (each new tile costs at least 1)
			for (decimal land = 0; land <= 500; land += 50) {
				decimal previous = -1;
				for (int n = 0; n <= 50; n++) {
					var current = ColonizeRepositoryWrite.GetCostForTiles(land, n);
					Assert.True(current > previous || n == 0, $"Cost not monotonic at L={land}, N={n}: prev={previous}, cur={current}");
					previous = current;
				}
			}
		}

		[Fact]
		public void GetMaxAffordable_RoundTripsWithGetCostForTiles() {
			// For any (L, minerals), GetMaxAffordable must satisfy:
			//   GetCostForTiles(L, M) <= minerals < GetCostForTiles(L, M+1)
			(decimal land, decimal minerals)[] cases = {
				(0m, 0m),
				(0m, 3m),     // affords 3 tiles at floor cost
				(0m, 4m),     // affords 4 tiles
				(100m, 0m),
				(100m, 25m),  // affords 1 tile at L=100 (cost=25)
				(100m, 26m),
				(100m, 261.25m),  // exactly 10 tiles
				(100m, 261.26m),
				(1000m, 1_000_000m),
				(50m, 10_000m),
			};
			foreach (var (land, minerals) in cases) {
				var max = ColonizeRepositoryWrite.GetMaxAffordable(land, minerals);
				Assert.True(max >= 0, $"max negative at L={land}, minerals={minerals}");
				var costAtMax = ColonizeRepositoryWrite.GetCostForTiles(land, max);
				var costAtNext = ColonizeRepositoryWrite.GetCostForTiles(land, max + 1);
				Assert.True(costAtMax <= minerals, $"cost({max})={costAtMax} > minerals={minerals} at L={land}");
				Assert.True(costAtNext > minerals, $"cost({max + 1})={costAtNext} <= minerals={minerals} at L={land} (max={max})");
			}
		}
	}
}
