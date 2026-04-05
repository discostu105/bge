using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record UnitDefinitionViewModel {
		public required string Id { get; set; }
		public required string Name { get; set; }
		public required string PlayerTypeRestriction { get; set; }
		public required CostViewModel Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public int Shields { get; set; }
		public int Speed { get; set; }
		public bool IsMobile { get; set; }
		public required List<string> Prerequisites { get; set; }
		public bool PrerequisitesMet { get; set; }

		public static UnitDefinitionViewModel Create(UnitDef unitDefinition, bool prerequisitesMet) {
			return new UnitDefinitionViewModel {
				Id = unitDefinition.Id.Id,
				Name = unitDefinition.Name,
				PlayerTypeRestriction = unitDefinition.PlayerTypeRestriction.Id,
				Cost = CostViewModel.Create(unitDefinition.Cost),
				Attack = unitDefinition.Attack,
				Defense = unitDefinition.Defense,
				Hitpoints = unitDefinition.Hitpoints,
				Shields = unitDefinition.Shields,
				Speed = unitDefinition.Speed,
				IsMobile = unitDefinition.IsMobile,
				Prerequisites = unitDefinition.Prerequisites.Select(x => x.Id).ToList(),
				PrerequisitesMet = prerequisitesMet
			};
		}
	}
}
