using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public class UnitDefinitionViewModel {
		public string Id { get; set; }
		public string Name { get; set; }
		public string PlayerTypeRestriction { get; set; }
		public IDictionary<string, decimal> Cost { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }
		public int Hitpoints { get; set; }
		public int Speed { get; set; }
		public List<string> Prerequisites { get; set; }

		public static UnitDefinitionViewModel Create(UnitDef unitDefinition) {
			return new UnitDefinitionViewModel {
				Id = unitDefinition.Id,
				Name = unitDefinition.Name,
				PlayerTypeRestriction = unitDefinition.PlayerTypeRestriction,
				Cost = unitDefinition.Cost,
				Attack = unitDefinition.Attack,
				Defense = unitDefinition.Defense,
				Hitpoints = unitDefinition.Hitpoints,
				Speed = unitDefinition.Speed,
				Prerequisites = unitDefinition.Prerequisites
			};
		}
	}
}
