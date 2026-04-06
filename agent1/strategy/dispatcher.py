"""ActionDispatcher — translates strategy decisions into BGE API calls."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING, Optional

if TYPE_CHECKING:
	from agent1.analytics.decision_logger import DecisionLogger
	from agent1.client import BgeClient

logger = logging.getLogger(__name__)


class ActionDispatcher:
	"""Wraps ``BgeClient`` and provides intent-level dispatch methods.

	When a :class:`~agent1.analytics.decision_logger.DecisionLogger` is
	supplied, each successfully dispatched action is appended to the
	decision log.  Errors are never logged as decisions.
	"""

	def __init__(
		self,
		client: BgeClient,
		decision_logger: Optional[DecisionLogger] = None,
		current_tick: int = 0,
	) -> None:
		self._client = client
		self._decision_logger = decision_logger
		self.current_tick = current_tick

	def attack(self, unit_id: str, enemy_player_id: str) -> None:
		"""Send *unit_id* to attack *enemy_player_id*."""
		logger.info("Dispatching attack: unit=%s → enemy=%s", unit_id, enemy_player_id)
		self._client.send_units(unit_id, enemy_player_id)
		if self._decision_logger is not None:
			self._decision_logger.log(
				game_tick=self.current_tick,
				decision_type="attack",
				target_player_id=enemy_player_id,
			)

	def build_units(self, unit_def_id: str, count: int) -> None:
		"""Train *count* units of *unit_def_id*."""
		logger.info("Dispatching build_units: %s × %d", unit_def_id, count)
		self._client.build_units(unit_def_id, count)
		if self._decision_logger is not None:
			self._decision_logger.log(
				game_tick=self.current_tick,
				decision_type="build_units",
				resource_type=unit_def_id,
				amount=count,
			)

	def build_asset(self, asset_def_id: str) -> None:
		"""Construct asset *asset_def_id*."""
		logger.info("Dispatching build_asset: %s", asset_def_id)
		self._client.build_asset(asset_def_id)
		if self._decision_logger is not None:
			self._decision_logger.log(
				game_tick=self.current_tick,
				decision_type="build_asset",
				resource_type=asset_def_id,
			)
