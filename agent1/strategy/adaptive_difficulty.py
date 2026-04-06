"""AdaptiveDifficultyAdvisor — recommends difficulty based on outcome history.

Reads *outcomes_path* (a JSONL file written by OutcomeTracker from Phase 4A)
and computes a rolling win-rate over the last *window* games.  Returns a
:class:`DifficultyRecommendation` when enough data exists, or the sentinel
string ``"insufficient_data"`` when fewer than *window* records are available.
"""
from __future__ import annotations

import json
import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Union

logger = logging.getLogger(__name__)

DIFFICULTY_ORDER = ["easy", "medium", "hard", "expert"]

# Sentinel returned when the outcome history is too short for a recommendation.
INSUFFICIENT_DATA = "insufficient_data"


@dataclass
class DifficultyRecommendation:
	"""Structured recommendation produced by :class:`AdaptiveDifficultyAdvisor`."""

	suggested_difficulty: str
	confidence: float
	reason: str


class AdaptiveDifficultyAdvisor:
	"""Recommends difficulty adjustments from recent game outcome history.

	Args:
		outcomes_path: Path to the JSONL outcomes file (written by OutcomeTracker).
			Each line must be a JSON object with at least an ``"outcome"`` field
			whose value is ``"win"`` or ``"loss"``.
		window: Number of recent games to consider.  Default 10.
		promote_threshold: Win-rate at or above which difficulty is increased.
			Default 0.7 (won 7 of last 10).
		demote_threshold: Win-rate at or below which difficulty is decreased.
			Default 0.3 (won 3 of last 10).
	"""

	def __init__(
		self,
		outcomes_path: Union[str, Path] = "./logs/outcomes.jsonl",
		window: int = 10,
		promote_threshold: float = 0.7,
		demote_threshold: float = 0.3,
	) -> None:
		self._outcomes_path = Path(outcomes_path)
		self._window = window
		self._promote_threshold = promote_threshold
		self._demote_threshold = demote_threshold

	def recommend(self, current_difficulty: str) -> Union[DifficultyRecommendation, str]:
		"""Return a :class:`DifficultyRecommendation`, or ``INSUFFICIENT_DATA``.

		No auto-adjust is triggered until the outcomes window is filled.

		Args:
			current_difficulty: The bot's current difficulty name (e.g. ``"medium"``).

		Returns:
			:class:`DifficultyRecommendation` when enough history exists, or the
			string ``"insufficient_data"`` when fewer than *window* records are
			available.
		"""
		outcomes = self._load_outcomes()
		if len(outcomes) < self._window:
			return INSUFFICIENT_DATA

		recent = outcomes[-self._window:]
		wins = sum(1 for o in recent if o.get("outcome") == "win")
		win_rate = wins / len(recent)

		if current_difficulty in DIFFICULTY_ORDER:
			current_idx = DIFFICULTY_ORDER.index(current_difficulty)
		else:
			current_idx = 1  # fall back to medium index

		if win_rate >= self._promote_threshold:
			new_idx = min(current_idx + 1, len(DIFFICULTY_ORDER) - 1)
			suggested = DIFFICULTY_ORDER[new_idx]
			confidence = (win_rate - self._promote_threshold) / (1.0 - self._promote_threshold)
			reason = (
				f"Win-rate {win_rate:.0%} >= {self._promote_threshold:.0%} "
				f"over last {self._window} games — increasing difficulty"
			)
		elif win_rate <= self._demote_threshold:
			new_idx = max(current_idx - 1, 0)
			suggested = DIFFICULTY_ORDER[new_idx]
			confidence = (self._demote_threshold - win_rate) / self._demote_threshold
			reason = (
				f"Win-rate {win_rate:.0%} <= {self._demote_threshold:.0%} "
				f"over last {self._window} games — decreasing difficulty"
			)
		else:
			suggested = current_difficulty
			confidence = 0.5
			reason = (
				f"Win-rate {win_rate:.0%} in acceptable range "
				f"over last {self._window} games — no change"
			)

		return DifficultyRecommendation(
			suggested_difficulty=suggested,
			confidence=round(confidence, 3),
			reason=reason,
		)

	def _load_outcomes(self) -> list[dict]:
		"""Read all records from the outcomes JSONL file."""
		if not self._outcomes_path.exists():
			return []
		records: list[dict] = []
		try:
			with self._outcomes_path.open("r", encoding="utf-8") as fh:
				for line in fh:
					line = line.strip()
					if line:
						records.append(json.loads(line))
		except (OSError, json.JSONDecodeError) as exc:
			logger.warning("Failed to read outcomes file %s: %s", self._outcomes_path, exc)
		return records
