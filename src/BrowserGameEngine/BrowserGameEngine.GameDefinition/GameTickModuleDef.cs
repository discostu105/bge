using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.GameDefinition {
	public record GameTickModuleDef(
		string Name,
		IDictionary<string, string> Properties
	) {
		public override string ToString() => $"GameTickModuleDef:{Name}";
	}
}
