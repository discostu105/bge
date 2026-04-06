"""Tests for AdaptiveDifficultyAdvisor (Phase 4B)."""
from __future__ import annotations

import json
from pathlib import Path

import pytest

from agent1.strategy.adaptive_difficulty import (
	INSUFFICIENT_DATA,
	AdaptiveDifficultyAdvisor,
	DifficultyRecommendation,
)


def _write_outcomes(path: Path, outcomes: list[str]) -> None:
	"""Write a list of outcome strings (``"win"``/``"loss"``) to a JSONL file."""
	with path.open("w", encoding="utf-8") as fh:
		for outcome in outcomes:
			fh.write(json.dumps({"outcome": outcome}) + "\n")


class TestAdaptiveDifficultyAdvisorInsufficientData:
	def test_returns_insufficient_data_when_file_missing(self, tmp_path: Path) -> None:
		advisor = AdaptiveDifficultyAdvisor(
			outcomes_path=tmp_path / "outcomes.jsonl",
			window=10,
		)
		assert advisor.recommend("medium") == INSUFFICIENT_DATA

	def test_returns_insufficient_data_when_fewer_than_window(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 5)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		assert advisor.recommend("medium") == INSUFFICIENT_DATA

	def test_returns_insufficient_data_when_exactly_window_minus_one(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 9)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		assert advisor.recommend("medium") == INSUFFICIENT_DATA

	def test_proceeds_when_exactly_window(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 10)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		result = advisor.recommend("easy")
		assert result != INSUFFICIENT_DATA


class TestAdaptiveDifficultyAdvisorRecommendations:
	def test_promotes_on_high_win_rate(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		# 8 wins out of 10 = 80% >= promote_threshold (0.7)
		_write_outcomes(outcomes_file, ["win"] * 8 + ["loss"] * 2)
		advisor = AdaptiveDifficultyAdvisor(
			outcomes_path=outcomes_file,
			window=10,
			promote_threshold=0.7,
			demote_threshold=0.3,
		)
		rec = advisor.recommend("easy")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "medium"
		assert rec.confidence > 0.0
		assert "increasing" in rec.reason

	def test_demotes_on_low_win_rate(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		# 2 wins out of 10 = 20% <= demote_threshold (0.3)
		_write_outcomes(outcomes_file, ["win"] * 2 + ["loss"] * 8)
		advisor = AdaptiveDifficultyAdvisor(
			outcomes_path=outcomes_file,
			window=10,
			promote_threshold=0.7,
			demote_threshold=0.3,
		)
		rec = advisor.recommend("hard")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "medium"
		assert rec.confidence > 0.0
		assert "decreasing" in rec.reason

	def test_no_change_on_acceptable_win_rate(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		# 5 wins out of 10 = 50% — in range [0.3, 0.7]
		_write_outcomes(outcomes_file, ["win"] * 5 + ["loss"] * 5)
		advisor = AdaptiveDifficultyAdvisor(
			outcomes_path=outcomes_file,
			window=10,
			promote_threshold=0.7,
			demote_threshold=0.3,
		)
		rec = advisor.recommend("medium")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "medium"
		assert "no change" in rec.reason

	def test_does_not_promote_beyond_expert(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 10)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		rec = advisor.recommend("expert")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "expert"

	def test_does_not_demote_below_easy(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["loss"] * 10)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		rec = advisor.recommend("easy")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "easy"

	def test_uses_only_last_window_records(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		# 90 losses followed by 10 wins — only recent window (wins) matters
		_write_outcomes(outcomes_file, ["loss"] * 90 + ["win"] * 10)
		advisor = AdaptiveDifficultyAdvisor(
			outcomes_path=outcomes_file,
			window=10,
			promote_threshold=0.7,
		)
		rec = advisor.recommend("easy")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.suggested_difficulty == "medium"

	def test_confidence_is_rounded_to_three_places(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 8 + ["loss"] * 2)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		rec = advisor.recommend("easy")
		assert isinstance(rec, DifficultyRecommendation)
		assert rec.confidence == round(rec.confidence, 3)

	def test_unknown_current_difficulty_falls_back_to_medium_index(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		_write_outcomes(outcomes_file, ["win"] * 10)
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=10)
		rec = advisor.recommend("unknown_level")
		assert isinstance(rec, DifficultyRecommendation)
		# Falls back to index 1 (medium), promote → hard
		assert rec.suggested_difficulty == "hard"


class TestAdaptiveDifficultyAdvisorEdgeCases:
	def test_empty_file_returns_insufficient_data(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		outcomes_file.write_text("")
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=1)
		assert advisor.recommend("medium") == INSUFFICIENT_DATA

	def test_corrupt_file_returns_insufficient_data(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		outcomes_file.write_text("not valid json\n")
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=1)
		assert advisor.recommend("medium") == INSUFFICIENT_DATA

	def test_mixed_valid_and_blank_lines(self, tmp_path: Path) -> None:
		outcomes_file = tmp_path / "outcomes.jsonl"
		with outcomes_file.open("w") as fh:
			fh.write('\n{"outcome": "win"}\n\n{"outcome": "win"}\n')
		advisor = AdaptiveDifficultyAdvisor(outcomes_path=outcomes_file, window=2)
		rec = advisor.recommend("easy")
		assert rec != INSUFFICIENT_DATA
