﻿using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerStateImmutable(
		DateTime? LastGameTickUpdate,
		GameTick CurrentGameTick,
		IDictionary<ResourceDefId, decimal> Resources,
		List<AssetImmutable> Assets,
		List<UnitImmutable> Units
	);
}