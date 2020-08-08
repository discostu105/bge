using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public record Cost(
		IDictionary<ResourceDefId, decimal> Resources
	);
}
