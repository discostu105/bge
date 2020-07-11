using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public class AssetDefinitionViewModel {
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? PlayerTypeRestriction { get; set; }
		public Dictionary<string, decimal>? Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public List<string>? Prerequisites { get; set; }
		public int Buildtime { get; set; }

		public static AssetDefinitionViewModel Create(AssetDef assetDefinition) {
			return new AssetDefinitionViewModel {
				Id = assetDefinition.Id,
				Name = assetDefinition.Name,
				PlayerTypeRestriction = assetDefinition.PlayerTypeRestriction,
				Cost = assetDefinition.Cost,
				Attack = assetDefinition.Attack,
				Defense = assetDefinition.Defense,
				Hitpoints = assetDefinition.Hitpoints,
				Prerequisites = assetDefinition.Prerequisites,
				Buildtime = assetDefinition.Buildtime
			};
		}
	}
}
