"""DecisionEngine — drives one strategy module per tick."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.strategy.base import StrategyContext
	from agent1.strategy.dispatcher import ActionDispatcher

logger = logging.getLogger(__name__)


class DecisionEngine:
	"""Orchestrates strategy evaluation and action dispatch for one tick.

	Args:
		strategy: Any object implementing ``run_tick(ctx, dispatcher)``.
		dispatcher: ``ActionDispatcher`` instance used for all API calls.
	"""

	def __init__(self, strategy: object, dispatcher: ActionDispatcher) -> None:
		self._strategy = strategy
		self._dispatcher = dispatcher

	def run_tick(self, ctx: StrategyContext) -> None:
		"""Evaluate the strategy and dispatch resulting actions.

		Errors from the strategy or dispatcher are logged and swallowed so
		the main loop continues running even on transient API failures.
		"""
		try:
			self._strategy.run_tick(ctx, self._dispatcher)
		except Exception:
			logger.exception("Strategy tick failed — skipping dispatch for this tick")
