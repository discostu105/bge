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
			ValidateAllUnique(gameDef.Units.Select(x => x.Id.Id), "Units");
			foreach(var unit in gameDef.Units) {
				gameDef.ValidatePlayerType(unit.PlayerTypeRestriction, unit.Id.Id + " PlayerTypeRestriction");
				if (unit.Prerequisites.Count == 0) throw new InvalidGameDefException($"Unit '{unit.Id}' has no prerequisites. At least one prerequisite is required.");
				unit.Prerequisites.ForEach(x => gameDef.ValidateAssetDefId(x, unit.Id.Id + " Prerequisites"));
				gameDef.ValidateCost(unit.Cost, unit.Id.Id + " Cost");
			}
		}

		private void VerifyAssets(GameDef gameDef) {
			ValidateAllUnique(gameDef.Assets.Select(x => x.Id.Id), "Assets");
			foreach (var asset in gameDef.Assets) {
				gameDef.ValidatePlayerType(asset.PlayerTypeRestriction, asset.Id.Id + " PlayerTypeRestriction");
				asset.Prerequisites.ForEach(x => gameDef.ValidateAssetDefId(x, asset.Id.Id + " Prerequisites"));
				gameDef.ValidateCost(asset.Cost, asset.Id.Id + " Cost");
			}
		}

		private void VerifyResources(GameDef gameDef) {
			ValidateAllUnique(gameDef.Resources.Select(x => x.Id.Id), "Resources");
			gameDef.ValidateResourceDefId(gameDef.ScoreResource, "ScoreResource");
		}

		public static void ValidateAllUnique(IEnumerable<string> values, string name) {
			if (!AllUnique(values)) throw new InvalidGameDefException($"Not all '{name}' are unique. (Values: {string.Join(", ", values)})");
		}

		private static bool AllUnique(IEnumerable<string> values) => values.Distinct().Count() == values.Count();

	}
}
