"""ActionDispatcher — translates strategy decisions into BGE API calls."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.client import BgeClient

logger = logging.getLogger(__name__)


class ActionDispatcher:
	"""Wraps ``BgeClient`` and provides intent-level dispatch methods."""

	def __init__(self, client: BgeClient) -> None:
		self._client = client

	def attack(self, unit_id: str, enemy_player_id: str) -> None:
		"""Send *unit_id* to attack *enemy_player_id*."""
		logger.info("Dispatching attack: unit=%s → enemy=%s", unit_id, enemy_player_id)
		self._client.send_units(unit_id, enemy_player_id)

	def build_units(self, unit_def_id: str, count: int) -> None:
		"""Train *count* units of *unit_def_id*."""
		logger.info("Dispatching build_units: %s × %d", unit_def_id, count)
		self._client.build_units(unit_def_id, count)

	def build_asset(self, asset_def_id: str) -> None:
		"""Construct asset *asset_def_id*."""
		logger.info("Dispatching build_asset: %s", asset_def_id)
		self._client.build_asset(asset_def_id)
