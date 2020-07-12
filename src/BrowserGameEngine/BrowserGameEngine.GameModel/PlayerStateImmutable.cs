using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerStateImmutable (
		IDictionary<string, decimal> Resources,
		DateTime? LastUpdate
	);
}
