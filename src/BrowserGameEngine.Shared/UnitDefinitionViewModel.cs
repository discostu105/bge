using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record UnitDefinitionViewModel {
		public string Id { get; set; }
		public string Name { get; set; }
		public string PlayerTypeRestriction { get; set; }
		public IDictionary<string, decimal>? Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public int Speed { get; set; }
		public List<string> Prerequisites { get; set; }
		public bool PrerequisitesMet { get; set; }

		public static UnitDefinitionViewModel Create(UnitDef unitDefinition, bool prerequisitesMet) {
			return new UnitDefinitionViewModel {
				Id = unitDefinition.Id.Id,
				Name = unitDefinition.Name,
				PlayerTypeRestriction = unitDefinition.PlayerTypeRestriction.Id,
				Cost = unitDefinition.Cost.ToPlainDictionary(),
				Attack = unitDefinition.Attack,
				Defense = unitDefinition.Defense,
				Hitpoints = unitDefinition.Hitpoints,
				Speed = unitDefinition.Speed,
				Prerequisites = unitDefinition.Prerequisites.Select(x => x.Id).ToList(),
				PrerequisitesMet = prerequisitesMet
			};
		}
	}
}
