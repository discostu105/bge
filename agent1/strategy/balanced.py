"""BalancedStrategy — default strategy using existing military/resource modules.

Bridges the game-loop ``StrategyContext`` (state + config) to the flat
per-strategy ``StrategyContext`` expected by ``military`` and ``resources``.
"""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from .context import StrategyContext as _LegacyCtx
from .military import should_attack
from .resources import worker_target

if TYPE_CHECKING:
	from agent1.strategy.base import StrategyContext
	from agent1.strategy.dispatcher import ActionDispatcher

logger = logging.getLogger(__name__)

# Default unit type to train when workers are needed
_WORKER_UNIT_DEF = "worker"


class BalancedStrategy:
	"""Balances attacking enemies and growing the economy."""

	def run_tick(self, ctx: StrategyContext, dispatcher: ActionDispatcher) -> None:
		state = ctx.state
		cfg = ctx.config

		legacy = _LegacyCtx(
			minerals=state.minerals,
			land=state.land,
			max_land=state.max_land,
			units=state.units,
			difficulty_profile=cfg.difficulty_profile,
			overrides=cfg.strategy_overrides,
		)

		# Attack decision: iterate enemies ordered by alliance score (highest first),
		# skipping allied players (score 0.0).
		scores = ctx.attack_target_scores
		scored_enemies = sorted(
			state.attackable_players,
			key=lambda e: scores.get(e.get("playerId", ""), 1.0),
			reverse=True,
		)
		for enemy in scored_enemies:
			pid = enemy.get("playerId", "")
			if scores.get(pid, 1.0) == 0.0:
				# Allied player — never attack.
				continue
			enemy_units = int(enemy.get("approxHomeUnitCount") or 0)
			if should_attack(legacy, enemy_units=enemy_units):
				home_units = [u for u in state.unit_list if not u.get("positionPlayerId") and u.get("count", 0) > 0]
				if home_units:
					unit = home_units[0]
					dispatcher.attack(unit["unitId"], pid)
				break  # one attack per tick

		# Worker / economy decision
		target = worker_target(legacy, max_workers=20)
		if target > state.units:
			needed = target - state.units
			dispatcher.build_units(_WORKER_UNIT_DEF, needed)
