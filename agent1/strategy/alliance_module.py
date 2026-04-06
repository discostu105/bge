"""AllianceStrategyModule — alliance-aware target scoring and alliance actions.

In *passive* mode (default) the module only prevents attacking allied players.
In *active* mode (``config.alliance_mode = "active"``) it will also create or
join alliances and declare war on isolated opponents, using thresholds from
``config.strategy_overrides``.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.strategy.base import StrategyContext
	from agent1.strategy.dispatcher import ActionDispatcher

logger = logging.getLogger(__name__)

# Score multipliers
_ALLY_SCORE: float = 0.0
_WAR_ENEMY_BOOST: float = 2.0
_DEFAULT_SCORE: float = 1.0


@dataclass
class AllianceAction:
	"""A single pending alliance action to be dispatched this tick."""

	kind: str  # "create", "join", "declare_war"
	kwargs: dict = field(default_factory=dict)


class AllianceStrategyModule:
	"""Scores attack targets and decides alliance-management actions each tick.

	This module is not a standalone strategy; it is wired into
	``DecisionEngine`` so any active strategy benefits from alliance awareness
	without duplicating logic.
	"""

	def score_attack_targets(self, ctx: StrategyContext) -> dict[str, float]:
		"""Return a mapping of ``player_id → attack score`` for all attackable enemies.

		Rules:
		- Allied players → score ``0.0`` (never attack).
		- War-enemy players → score ``2.0`` (prioritised attack).
		- All others → score ``1.0`` (neutral).

		Returns an empty dict when there is no alliance tracker or no
		attackable players.
		"""
		tracker = ctx.alliance_tracker
		scores: dict[str, float] = {}
		for enemy in ctx.state.attackable_players:
			pid = enemy.get("playerId", "")
			if not pid:
				continue
			if tracker is not None and tracker.is_ally(pid):
				scores[pid] = _ALLY_SCORE
			elif tracker is not None and tracker.is_war_target(pid):
				scores[pid] = _WAR_ENEMY_BOOST
			else:
				scores[pid] = _DEFAULT_SCORE
		return scores

	def decide_alliance_action(self, ctx: StrategyContext) -> list[AllianceAction]:
		"""Return a list of alliance actions to execute this tick.

		Always returns an empty list in passive mode.  In active mode, the
		module may propose creating an alliance (when not yet a member) or
		declaring war (when a lone powerful opponent is detected).
		"""
		if ctx.config.alliance_mode != "active":
			return []

		tracker = ctx.alliance_tracker
		if tracker is None:
			return []

		actions: list[AllianceAction] = []

		# Create an alliance when not already a member.
		if tracker.my_alliance_id is None:
			overrides = ctx.config.strategy_overrides
			actions.append(AllianceAction(
				kind="create",
				kwargs={
					"name": overrides.get("alliance_name", "BotAlliance"),
					"password": overrides.get("alliance_password", ""),
				},
			))
			return actions

		# Declare war on the strongest enemy if above a unit threshold and not already at war.
		if not tracker.war_enemy_player_ids:
			overrides = ctx.config.strategy_overrides
			war_unit_threshold = int(overrides.get("war_unit_threshold", 50))
			if ctx.state.units >= war_unit_threshold:
				# Find the most powerful enemy alliance (largest member count) to target.
				best_target: str | None = None
				best_members = 0
				for aid, alliance in tracker.known_alliances.items():
					if aid == tracker.my_alliance_id:
						continue
					mc = alliance.get("memberCount", 0)
					if mc > best_members:
						best_members = mc
						best_target = aid
				if best_target:
					actions.append(AllianceAction(
						kind="declare_war",
						kwargs={
							"my_alliance_id": tracker.my_alliance_id,
							"target_alliance_id": best_target,
						},
					))

		return actions
