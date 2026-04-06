"""AllianceTracker — refreshed once per tick from the BGE alliance API.

Maintains a lightweight view of the bot's alliance membership, allied player
IDs, and war-enemy player IDs so strategy modules can make alliance-aware
decisions without duplicating API logic.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.client import BgeClient

logger = logging.getLogger(__name__)


@dataclass
class AllianceTracker:
	"""Mutable alliance state updated once per game tick.

	Attributes:
		my_alliance_id: ID of the alliance the bot belongs to, or ``None``
			if the bot is not currently a member of any alliance.
		allied_player_ids: Set of player IDs who are active (non-pending)
			members of the bot's alliance.  Empty when not in an alliance.
		war_enemy_player_ids: Set of player IDs who belong to alliances that
			are actively at war with the bot's alliance.  Empty when not at war.
		known_alliances: Mapping from alliance ID to its ``AllianceViewModel``
			dict as returned by ``GET /api/alliances``.
	"""

	my_alliance_id: str | None = None
	allied_player_ids: set = field(default_factory=set)
	war_enemy_player_ids: set = field(default_factory=set)
	known_alliances: dict = field(default_factory=dict)

	def update(self, client: BgeClient) -> None:
		"""Refresh alliance state from the API.

		All HTTP errors are caught and logged so a transient failure never
		crashes the game loop.
		"""
		try:
			status = client.get_my_alliance_status()
		except Exception:
			logger.exception("AllianceTracker: failed to fetch my-status")
			return

		if not status.get("isMember"):
			self.my_alliance_id = None
			self.allied_player_ids = set()
			self.war_enemy_player_ids = set()
			return

		self.my_alliance_id = status.get("allianceId")

		# Populate known_alliances index from the public alliance list.
		try:
			all_alliances = client.get_all_alliances()
			self.known_alliances = {a["allianceId"]: a for a in all_alliances}
		except Exception:
			logger.exception("AllianceTracker: failed to fetch alliance list")

		if not self.my_alliance_id:
			return

		# Resolve allied player IDs from the detailed member list.
		try:
			detail = client.get_alliance_detail(self.my_alliance_id)
			self.allied_player_ids = {
				m["playerId"]
				for m in detail.get("members", [])
				if not m.get("isPending")
			}
		except Exception:
			logger.exception(
				"AllianceTracker: failed to fetch detail for alliance %s",
				self.my_alliance_id,
			)

		# Resolve war-enemy player IDs.
		try:
			wars = client.get_alliance_wars(self.my_alliance_id)
			enemy_alliance_ids: set[str] = set()
			for war in wars:
				if war.get("status") == "Active":
					if war["attackerAllianceId"] == self.my_alliance_id:
						enemy_alliance_ids.add(war["defenderAllianceId"])
					else:
						enemy_alliance_ids.add(war["attackerAllianceId"])

			war_enemy_ids: set[str] = set()
			for enemy_aid in enemy_alliance_ids:
				try:
					enemy_detail = client.get_alliance_detail(enemy_aid)
					war_enemy_ids.update(
						m["playerId"]
						for m in enemy_detail.get("members", [])
						if not m.get("isPending")
					)
				except Exception:
					logger.exception(
						"AllianceTracker: failed to fetch detail for enemy alliance %s",
						enemy_aid,
					)
			self.war_enemy_player_ids = war_enemy_ids
		except Exception:
			logger.exception("AllianceTracker: failed to fetch wars for alliance %s", self.my_alliance_id)

	def is_ally(self, player_id: str) -> bool:
		"""Return ``True`` if *player_id* is an active member of the bot's alliance."""
		return player_id in self.allied_player_ids

	def is_war_target(self, player_id: str) -> bool:
		"""Return ``True`` if *player_id* belongs to an alliance at war with us."""
		return player_id in self.war_enemy_player_ids
