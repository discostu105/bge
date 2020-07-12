using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerImmutable (
		PlayerId PlayerId,
		string Name,
		DateTime Created,
		PlayerStateImmutable State
	);
}
