using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.GameDefinition {
	public record Cost(
		IDictionary<ResourceDefId, decimal> Resources
	) {
		public override string ToString() => string.Join(", ", this.ToPlainDictionary().Select(x => $"{x.Key}:{x.Value}"));

		public static Cost FromSingle(ResourceDefId resourceDefId, decimal value) {
			return new Cost(new Dictionary<ResourceDefId, decimal> { { resourceDefId, value } });
		}
	}

	public static class CostExtensions {
		public static IDictionary<string, decimal> ToPlainDictionary(this Cost cost) {
			return cost.Resources.ToDictionary(x => x.Key.Id, y => y.Value);
		}

		public static Cost Multiply(this Cost cost, int count) {
			return new Cost(cost.Resources.ToDictionary(
				x => x.Key,
				y => y.Value * count
			));
		}
	}

	public static class CostHelper {
		public static Cost Create(params (string, decimal)[] resources) {
			return new Cost(resources.ToDictionary(x => new ResourceDefId(x.Item1), y => y.Item2));
		}
	}
}
