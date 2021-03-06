﻿using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	// can be anything that enables stuff or improves capabilities
	// e.g. buildings or upgrades
	public record AssetImmutable(
		AssetDefId AssetDefId,
		int Level
	);
}
