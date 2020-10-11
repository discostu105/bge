using BrowserGameEngine.GameModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class BattleTest {

		// Pass ITestOutputHelper into the test class, which xunit provides per-test
		public BattleTest(ITestOutputHelper outputHelper) {
			OutputHelper = outputHelper;
		}

		public ITestOutputHelper OutputHelper { get; }

		[Fact]
		public void BattleSco1() {
			var battleBehavior = new BattleBehaviorScoOriginal(OutputHelper.ToLogger<IBattleBehavior>());

			var attackers = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 100, Attack = 10, Defense = 10, Count = 10 }
			};
			var defenders = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 100, Attack = 10, Defense = 10, Count = 10 }
			};

			var result = battleBehavior.CalculateResult(attackers, defenders);

			Assert.Equal(6, result.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Equal(5, result.AttackingUnitsDestroyed.Sum(x => x.Count));
		}

		[Fact]
		public void BattleSco2() {
			var battleBehavior = new BattleBehaviorScoOriginal(OutputHelper.ToLogger<IBattleBehavior>());

			var attackers = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 100, Attack = 20, Defense = 10, Count = 10 }
			};
			var defenders = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 100, Attack = 10, Defense = 10, Count = 10 }
			};

			var result = battleBehavior.CalculateResult(attackers, defenders);

			Assert.Equal(10, result.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Equal(2, result.AttackingUnitsDestroyed.Sum(x => x.Count));
		}

		[Fact]
		public void BattleSco3() {
			var battleBehavior = new BattleBehaviorScoOriginal(OutputHelper.ToLogger<IBattleBehavior>());

			int attackerCount = 1000;
			var attackersSingle = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 10, Attack = 1, Defense = 0, Count = attackerCount }
			};

			// same number of units as attackersSingle, but split up in multiple units 
			var attackersMulti = new List<BtlUnit>(
				Enumerable.Repeat<BtlUnit>(new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 10, Attack = 1, Defense = 0, Count = 1 }, attackerCount)
			);

			var defenders = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("unit1"), Hitpoints = 1000, Attack = 0, Defense = 100, Count = 10 }
			};
			
			var result1 = battleBehavior.CalculateResult(attackersSingle, defenders);
			Assert.Equal(5, result1.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Equal(550, result1.AttackingUnitsDestroyed.Sum(x => x.Count));

			// should yield the same result
			var result2 = battleBehavior.CalculateResult(attackersMulti, defenders);
			Assert.Equal(5, result2.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Equal(550, result2.AttackingUnitsDestroyed.Sum(x => x.Count));
		}

		[Fact]
		public void BattleScoSiegeTanks() {
			var battleBehavior = new BattleBehaviorScoOriginal(OutputHelper.ToLogger<IBattleBehavior>());

			var attackers = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("siegetank"), Hitpoints = 130, Attack = 10, Defense = 40, Count = 3 }
			};
			var defenders = new List<BtlUnit> {
				new BtlUnit { UnitDefId = Id.UnitDef("siegetank"), Hitpoints = 130, Attack = 10, Defense = 40, Count = 3 }
			};

			var result = battleBehavior.CalculateResult(attackers, defenders);

			Assert.Equal(0, result.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Equal(3, result.AttackingUnitsDestroyed.Sum(x => x.Count));
		}
	}

}
