"""DecisionLogger — append-only JSONL log of per-tick dispatched actions."""
from __future__ import annotations

import json
import logging
import os
import threading
from datetime import datetime, timezone
from typing import Optional

logger = logging.getLogger(__name__)


class DecisionLogger:
	"""Writes one JSONL record per dispatched action to *log_path*.

	Each record contains:
	``timestamp``, ``game_tick``, ``decision_type``, ``target_player_id``,
	``resource_type``, ``amount``, ``strategy_module``, ``confidence_score``.

	When *enabled* is ``False``, all ``log()`` calls are no-ops.
	"""

	def __init__(self, log_path: str = "./logs/decisions.jsonl", enabled: bool = True) -> None:
		self._log_path = log_path
		self._enabled = enabled
		self._lock = threading.Lock()

	# ------------------------------------------------------------------
	# Public API
	# ------------------------------------------------------------------

	def log(
		self,
		game_tick: int,
		decision_type: str,
		target_player_id: Optional[str] = None,
		resource_type: Optional[str] = None,
		amount: Optional[int] = None,
		strategy_module: Optional[str] = None,
		confidence_score: Optional[float] = None,
	) -> None:
		"""Append a decision record to the JSONL file.

		Args:
			game_tick: Current tick identifier.
			decision_type: One of ``"attack"``, ``"build_units"``, ``"build_asset"``, etc.
			target_player_id: Enemy player targeted (for attack actions).
			resource_type: Resource consumed (for build actions).
			amount: Quantity involved.
			strategy_module: Name of the strategy module that produced the decision.
			confidence_score: Confidence value in ``[0.0, 1.0]`` if applicable.
		"""
		if not self._enabled:
			return

		record = {
			"timestamp": datetime.now(timezone.utc).isoformat(),
			"game_tick": game_tick,
			"decision_type": decision_type,
			"target_player_id": target_player_id,
			"resource_type": resource_type,
			"amount": amount,
			"strategy_module": strategy_module,
			"confidence_score": confidence_score,
		}
		self._write(record)

	def tail(self, limit: int = 50) -> list[dict]:
		"""Return the last *limit* records from the log file.

		Returns an empty list when the log does not yet exist.
		"""
		if not os.path.exists(self._log_path):
			return []
		with self._lock:
			with open(self._log_path, encoding="utf-8") as fh:
				lines = fh.readlines()
		records: list[dict] = []
		for line in lines[-limit:]:
			line = line.strip()
			if line:
				try:
					records.append(json.loads(line))
				except json.JSONDecodeError:
					logger.warning("Skipping malformed JSONL line in %s", self._log_path)
		return records

	# ------------------------------------------------------------------
	# Internals
	# ------------------------------------------------------------------

	def _write(self, record: dict) -> None:
		os.makedirs(os.path.dirname(self._log_path) or ".", exist_ok=True)
		with self._lock:
			with open(self._log_path, "a", encoding="utf-8") as fh:
				fh.write(json.dumps(record) + "\n")
