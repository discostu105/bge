using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record AssetDefinitionViewModel {
		public required string Id { get; set; }
		public required string Name { get; set; }
		public required string PlayerTypeRestriction { get; set; }
		public required IDictionary<string, decimal> Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public required List<string> Prerequisites { get; set; }
		public int BuildTimeTicks { get; set; }

		public static AssetDefinitionViewModel Create(AssetDef assetDefinition) {
			return new AssetDefinitionViewModel {
				Id = assetDefinition.Id.Id,
				Name = assetDefinition.Name,
				PlayerTypeRestriction = assetDefinition.PlayerTypeRestriction.Id,
				Cost = assetDefinition.Cost.ToPlainDictionary(),
				Attack = assetDefinition.Attack,
				Defense = assetDefinition.Defense,
				Hitpoints = assetDefinition.Hitpoints,
				Prerequisites = assetDefinition.Prerequisites.Select(x => x.Id).ToList(),
				BuildTimeTicks = assetDefinition.BuildTimeTicks.Tick
			};
		}
	}
}
