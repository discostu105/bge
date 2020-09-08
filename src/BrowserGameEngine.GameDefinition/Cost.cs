using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.GameDefinition {
	public record Cost(
		IDictionary<ResourceDefId, decimal> Resources
	) {
		public override string ToString() => string.Join(", ", this.ToPlainDictionary().Select(x => $"{x.Key}:{x.Value}"));
	}

	public static class CostExtensions {
		public static IDictionary<string, decimal> ToPlainDictionary(this Cost cost) {
			return cost.Resources.ToDictionary(x => x.Key.Id, y => y.Value);
		}
	}

	public static class CostHelper {
		public static Cost Create(params (string, decimal)[] resources) {
			return new Cost(resources.ToDictionary(x => new ResourceDefId(x.Item1), y => y.Item2));
		}
	}
}
