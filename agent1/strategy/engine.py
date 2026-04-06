"""DecisionEngine — drives one strategy module per tick."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.strategy.alliance_module import AllianceStrategyModule
	from agent1.strategy.base import StrategyContext
	from agent1.strategy.dispatcher import ActionDispatcher

logger = logging.getLogger(__name__)


class DecisionEngine:
	"""Orchestrates strategy evaluation and action dispatch for one tick.

	Args:
		strategy: Any object implementing ``run_tick(ctx, dispatcher)``.
		dispatcher: ``ActionDispatcher`` instance used for all API calls.
		alliance_module: Optional ``AllianceStrategyModule``.  When supplied,
			target scores are computed before the strategy runs (populating
			``ctx.attack_target_scores``) and any active-mode alliance actions
			are dispatched.
	"""

	def __init__(
		self,
		strategy: object,
		dispatcher: ActionDispatcher,
		alliance_module: AllianceStrategyModule | None = None,
	) -> None:
		self._strategy = strategy
		self._dispatcher = dispatcher
		self._alliance_module = alliance_module

	def run_tick(self, ctx: StrategyContext) -> None:
		"""Evaluate the strategy and dispatch resulting actions.

		If an ``alliance_module`` is configured, this method:
		1. Scores all attack targets (populates ``ctx.attack_target_scores``).
		2. Dispatches alliance-management actions (create/join/declare war).
		3. Calls the main strategy's ``run_tick``.

		Errors from any step are logged and swallowed so the main loop
		continues running even on transient API failures.
		"""
		try:
			if self._alliance_module is not None:
				ctx.attack_target_scores = self._alliance_module.score_attack_targets(ctx)
				for action in self._alliance_module.decide_alliance_action(ctx):
					self._dispatcher.dispatch_alliance_action(action)
			self._strategy.run_tick(ctx, self._dispatcher)
		except Exception:
			logger.exception("Strategy tick failed — skipping dispatch for this tick")
