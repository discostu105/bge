"""ActionDispatcher — translates strategy decisions into BGE API calls."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.client import BgeClient
	from agent1.strategy.alliance_module import AllianceAction

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

	def dispatch_alliance_action(self, action: AllianceAction) -> None:
		"""Execute an ``AllianceAction`` returned by ``AllianceStrategyModule``."""
		kind = action.kind
		kw = action.kwargs
		if kind == "create":
			logger.info("Dispatching create_alliance: name=%s", kw.get("name"))
			self._client.create_alliance(kw["name"], kw.get("password", ""))
		elif kind == "join":
			logger.info("Dispatching join_alliance: id=%s", kw.get("alliance_id"))
			self._client.join_alliance(kw["alliance_id"], kw.get("password", ""))
		elif kind == "declare_war":
			logger.info(
				"Dispatching declare_war: my_alliance=%s → target=%s",
				kw.get("my_alliance_id"),
				kw.get("target_alliance_id"),
			)
			self._client.declare_war(kw["my_alliance_id"], kw["target_alliance_id"])
		else:
			logger.warning("Unknown alliance action kind: %s", kind)
