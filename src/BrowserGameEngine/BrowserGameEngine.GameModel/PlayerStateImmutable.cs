using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerStateImmutable (
		IDictionary<ResourceDefId, decimal> Resources,
		DateTime? LastUpdate
	);
}
