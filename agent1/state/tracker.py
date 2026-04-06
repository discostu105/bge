"""GameStateTracker — mutable accumulator updated once per tick.

Parses raw API response dicts (ViewModels serialized as JSON) into typed
fields that strategy code can read without touching HTTP concerns.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass
class GameStateTracker:
	"""Holds the latest snapshot of game state for the current bot."""

	# Resources
	minerals: int = 0
	gas: int = 0
	land: int = 0
	max_land: int = 0

	# Units (total count of all home units)
	units: int = 0
	# Raw unit list for dispatch (each entry has unitId, count, positionPlayerId)
	unit_list: list[dict] = field(default_factory=list)

	# Attackable enemies returned by /api/battle/attackableplayers
	attackable_players: list[dict] = field(default_factory=list)

	def update(
		self,
		tick_info: dict[str, Any],
		resources: dict[str, Any],
		units: dict[str, Any],
		attackable_players: dict[str, Any],
	) -> None:
		"""Refresh state from raw API response dicts.

		Only the fields used by strategy modules are extracted; everything
		else is discarded to keep the footprint minimal.
		"""
		# Resources: primaryResource.cost may contain {"minerals": N}
		primary_cost: dict = (resources.get("primaryResource") or {}).get("cost") or {}
		secondary_cost: dict = (resources.get("secondaryResources") or {}).get("cost") or {}

		self.minerals = int(primary_cost.get("minerals", 0))
		self.gas = int(secondary_cost.get("gas", 0))
		self.land = int(secondary_cost.get("land", 0))
		self.max_land = int(secondary_cost.get("maxLand", self.land))

		# Units
		raw_units: list[dict] = (units.get("units") or [])
		# Home units only (positionPlayerId is None/empty when at home)
		home_units = [u for u in raw_units if not u.get("positionPlayerId")]
		self.units = sum(u.get("count", 0) for u in home_units)
		self.unit_list = raw_units

		# Attackable players
		self.attackable_players = attackable_players.get("attackablePlayers") or []
