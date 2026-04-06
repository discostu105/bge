"""Tests for OutcomeTracker."""
from __future__ import annotations

import json
import os

import pytest

from agent1.analytics.outcome_tracker import OutcomeTracker
from agent1.state.tracker import GameStateTracker


def _make_tracker(minerals: int = 0, gas: int = 0, units: int = 0) -> GameStateTracker:
	t = GameStateTracker()
	t.minerals = minerals
	t.gas = gas
	t.units = units
	return t


def test_record_writes_delta_fields(tmp_path):
	path = str(tmp_path / "outcomes.jsonl")
	ot = OutcomeTracker(outcomes_path=path)

	prev = _make_tracker(minerals=100, gas=50, units=5)
	curr = _make_tracker(minerals=150, gas=40, units=8)

	ot.record(prev, curr, game_tick=10)

	with open(path) as fh:
		record = json.loads(fh.read())

	assert record["game_tick"] == 10
	assert record["minerals"] == 150
	assert record["gas"] == 40
	assert record["units"] == 8
	assert record["minerals_delta"] == 50
	assert record["gas_delta"] == -10
	assert record["units_delta"] == 3
	assert "timestamp" in record


def test_record_first_tick_deltas_are_zero(tmp_path):
	path = str(tmp_path / "outcomes.jsonl")
	ot = OutcomeTracker(outcomes_path=path)

	curr = _make_tracker(minerals=200, gas=100, units=10)
	ot.record(None, curr, game_tick=1)

	with open(path) as fh:
		record = json.loads(fh.read())

	assert record["minerals_delta"] == 0
	assert record["gas_delta"] == 0
	assert record["units_delta"] == 0
	assert record["minerals"] == 200


def test_tail_returns_last_n_records(tmp_path):
	path = str(tmp_path / "outcomes.jsonl")
	ot = OutcomeTracker(outcomes_path=path)

	curr = _make_tracker()
	for i in range(10):
		curr.minerals = i * 10
		ot.record(None, curr, game_tick=i)

	records = ot.tail(limit=4)
	assert len(records) == 4
	assert records[-1]["game_tick"] == 9


def test_tail_returns_empty_when_no_file(tmp_path):
	path = str(tmp_path / "missing.jsonl")
	ot = OutcomeTracker(outcomes_path=path)
	assert ot.tail() == []


def test_disabled_skips_write(tmp_path):
	path = str(tmp_path / "outcomes.jsonl")
	ot = OutcomeTracker(outcomes_path=path, enabled=False)

	curr = _make_tracker(minerals=100)
	ot.record(None, curr, game_tick=1)

	assert not os.path.exists(path)
