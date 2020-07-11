using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class CostViewModel {
		public IDictionary<string, decimal>? Cost { get; set; }

		public static CostViewModel Create(Dictionary<string, decimal> cost) {
			return new CostViewModel {
				Cost = cost
			};
		}
	}
}