using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerImmutable (
		PlayerId PlayerId,
		PlayerTypeDefId PlayerType,
		string Name,
		DateTime Created,
		PlayerStateImmutable State
	);
}
