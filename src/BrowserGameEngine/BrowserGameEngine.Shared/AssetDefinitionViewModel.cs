using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record AssetDefinitionViewModel {
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? PlayerTypeRestriction { get; set; }
		public IDictionary<string, decimal>? Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public List<string>? Prerequisites { get; set; }
		public int BuildTimeTicks { get; set; }

		public static AssetDefinitionViewModel Create(AssetDef assetDefinition) {
			return new AssetDefinitionViewModel {
				Id = assetDefinition.Id.Id,
				Name = assetDefinition.Name,
				PlayerTypeRestriction = assetDefinition.PlayerTypeRestriction,
				Cost = assetDefinition.Cost.ToPlainDictionary(),
				Attack = assetDefinition.Attack,
				Defense = assetDefinition.Defense,
				Hitpoints = assetDefinition.Hitpoints,
				Prerequisites = assetDefinition.Prerequisites,
				BuildTimeTicks = assetDefinition.BuildTimeTicks.Tick
			};
		}
	}
}
