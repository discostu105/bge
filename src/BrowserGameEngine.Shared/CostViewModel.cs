using BrowserGameEngine.GameDefinition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace BrowserGameEngine.Shared {
	public record CostViewModel {
		public IDictionary<string, decimal> Cost { get; init; }

		public static CostViewModel Create(Cost cost) {
			return new CostViewModel {
				Cost = cost.Resources.ToDictionary(x => x.Key.Id, y => y.Value)
			};
		}
	}
}
