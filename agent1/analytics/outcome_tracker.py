"""OutcomeTracker — records per-tick resource and rank deltas to outcomes.jsonl."""
from __future__ import annotations

import json
import logging
import os
import threading
from datetime import datetime, timezone
from typing import TYPE_CHECKING, Optional

if TYPE_CHECKING:
	from agent1.state.tracker import GameStateTracker

logger = logging.getLogger(__name__)


class OutcomeTracker:
	"""Compares two consecutive ``GameStateTracker`` snapshots and writes a
	delta record to *outcomes_path* after each tick.

	Each record contains:
	``timestamp``, ``game_tick``, ``minerals_delta``, ``gas_delta``,
	``units_delta``, ``minerals``, ``gas``, ``units``.
	"""

	def __init__(
		self,
		outcomes_path: str = "./logs/outcomes.jsonl",
		enabled: bool = True,
	) -> None:
		self._outcomes_path = outcomes_path
		self._enabled = enabled
		self._lock = threading.Lock()

	def record(
		self,
		prev: Optional[GameStateTracker],
		curr: GameStateTracker,
		game_tick: int,
	) -> None:
		"""Write an outcome record comparing *prev* to *curr*.

		When *prev* is ``None`` (first tick), deltas are reported as 0.

		Args:
			prev: The tracker snapshot from the previous tick, or ``None``.
			curr: The tracker snapshot from the current tick.
			game_tick: Current tick identifier.
		"""
		if not self._enabled:
			return

		minerals_delta = (curr.minerals - prev.minerals) if prev is not None else 0
		gas_delta = (curr.gas - prev.gas) if prev is not None else 0
		units_delta = (curr.units - prev.units) if prev is not None else 0

		record = {
			"timestamp": datetime.now(timezone.utc).isoformat(),
			"game_tick": game_tick,
			"minerals": curr.minerals,
			"gas": curr.gas,
			"units": curr.units,
			"minerals_delta": minerals_delta,
			"gas_delta": gas_delta,
			"units_delta": units_delta,
		}
		self._write(record)

	def tail(self, limit: int = 50) -> list[dict]:
		"""Return the last *limit* records from the outcomes file.

		Returns an empty list when the file does not yet exist.
		"""
		if not os.path.exists(self._outcomes_path):
			return []
		with self._lock:
			with open(self._outcomes_path, encoding="utf-8") as fh:
				lines = fh.readlines()
		records: list[dict] = []
		for line in lines[-limit:]:
			line = line.strip()
			if line:
				try:
					records.append(json.loads(line))
				except json.JSONDecodeError:
					logger.warning("Skipping malformed JSONL line in %s", self._outcomes_path)
		return records

	def _write(self, record: dict) -> None:
		os.makedirs(os.path.dirname(self._outcomes_path) or ".", exist_ok=True)
		with self._lock:
			with open(self._outcomes_path, "a", encoding="utf-8") as fh:
				fh.write(json.dumps(record) + "\n")
