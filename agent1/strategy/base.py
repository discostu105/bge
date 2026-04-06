"""StrategyContext — the context object passed to DecisionEngine.run_tick().

This is the *game-loop* context.  It wraps a ``GameStateTracker`` snapshot
plus loop-level metadata (current tick counter, full config, tick history).
It is distinct from the per-strategy ``StrategyContext`` in ``context.py``
which holds flat scalar fields used by individual strategy functions.
"""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass, field
from typing import TYPE_CHECKING

if TYPE_CHECKING:
	from agent1.config import AgentConfig
	from agent1.state.tracker import GameStateTracker


@dataclass
class StrategyContext:
	"""Immutable-ish snapshot fed to the engine once per tick."""

	state: GameStateTracker
	tick: int
	config: AgentConfig
	history: deque = field(default_factory=deque)
