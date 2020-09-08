using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.GameDefinition {
	public class GameDefVerifier {
		public bool IsValid(GameDef gameDef) {
			try {
				Verify(gameDef);
			} catch (Exception e) {
				return false;
			}
			return true;
		}

		public void Verify(GameDef gameDef) {
			VerifyUnits(gameDef);
			VerifyAssets(gameDef);
			VerifyResources(gameDef);
		}

		private void VerifyUnits(GameDef gameDef) {
			TestAllUnique(gameDef.Units.Select(x => x.Id.Id), "Units");
			foreach(var unit in gameDef.Units) {
				TestPlayerType(gameDef, unit.PlayerTypeRestriction, unit.Id.Id + " PlayerTypeRestriction");
				unit.Prerequisites.ForEach(x => TestAsset(gameDef, x, unit.Id.Id + " Prerequisites"));
				TestCost(gameDef, unit.Cost, unit.Id.Id + " Cost");
			}
		}

		private void VerifyAssets(GameDef gameDef) {
			TestAllUnique(gameDef.Assets.Select(x => x.Id.Id), "Assets");
			foreach (var asset in gameDef.Assets) {
				TestPlayerType(gameDef, asset.PlayerTypeRestriction, asset.Id.Id + " PlayerTypeRestriction");
				asset.Prerequisites.ForEach(x => TestAsset(gameDef, x, asset.Id.Id + " Prerequisites"));
				TestCost(gameDef, asset.Cost, asset.Id.Id + " Cost");
			}
		}

		private void VerifyResources(GameDef gameDef) {
			TestAllUnique(gameDef.Resources.Select(x => x.Id.Id), "Resources");
			TestResource(gameDef, gameDef.ScoreResource, "ScoreResource");
		}

		private static void TestCost(GameDef gameDef, Cost cost, string name) {
			foreach (var resource in cost.Resources.Keys) TestResource(gameDef, resource, name);
		}

		private static void TestPlayerType(GameDef gameDef, PlayerTypeDefId playerTypeDefId, string name) {
			if (!gameDef.PlayerTypes.Any(x => x.Id.Equals(playerTypeDefId))) throw new InvalidGameDefException($"Player type '{playerTypeDefId}' not found. Check '{name}'!");
		}

		private static void TestResource(GameDef gameDef, ResourceDefId resourceDefId, string name) {
			if (!gameDef.Resources.Any(x => x.Id.Equals(resourceDefId))) throw new InvalidGameDefException($"Resource '{resourceDefId}' not found. Check '{name}'!");
		}

		private static void TestAsset(GameDef gameDef, AssetDefId assetDefId, string name) {
			if (!gameDef.Assets.Any(x => x.Id.Equals(assetDefId))) throw new InvalidGameDefException($"Asset '{assetDefId}' not found. Check '{name}'!");
		}

		private static void TestUnit(GameDef gameDef, UnitDefId unitDefId, string name) {
			if (!gameDef.Units.Any(x => x.Id.Equals(unitDefId))) throw new InvalidGameDefException($"Asset '{unitDefId}' not found. Check '{name}'!");
		}

		private void TestAllUnique(IEnumerable<string> values, string name) {
			if (!AllUnique(values)) throw new InvalidGameDefException($"Not all '{name}' are unique. (Values: {string.Join(", ", values)})");
		}

		private static bool AllUnique(IEnumerable<string> values) => values.Distinct().Count() == values.Count();

	}
}
