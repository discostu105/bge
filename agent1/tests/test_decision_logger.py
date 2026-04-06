"""Tests for DecisionLogger."""
from __future__ import annotations

import json
import os

import pytest

from agent1.analytics.decision_logger import DecisionLogger


def test_log_appends_jsonl(tmp_path):
	log_path = str(tmp_path / "decisions.jsonl")
	dl = DecisionLogger(log_path=log_path)

	dl.log(game_tick=1, decision_type="attack", target_player_id="p2")
	dl.log(game_tick=2, decision_type="build_units", resource_type="marine", amount=3)

	with open(log_path) as fh:
		lines = fh.readlines()

	assert len(lines) == 2
	r1 = json.loads(lines[0])
	assert r1["game_tick"] == 1
	assert r1["decision_type"] == "attack"
	assert r1["target_player_id"] == "p2"
	assert "timestamp" in r1

	r2 = json.loads(lines[1])
	assert r2["game_tick"] == 2
	assert r2["decision_type"] == "build_units"
	assert r2["resource_type"] == "marine"
	assert r2["amount"] == 3


def test_log_all_fields_present(tmp_path):
	log_path = str(tmp_path / "decisions.jsonl")
	dl = DecisionLogger(log_path=log_path)

	dl.log(
		game_tick=5,
		decision_type="build_asset",
		target_player_id=None,
		resource_type="barracks",
		amount=None,
		strategy_module="military",
		confidence_score=0.9,
	)

	with open(log_path) as fh:
		record = json.loads(fh.read())

	expected_keys = {
		"timestamp", "game_tick", "decision_type", "target_player_id",
		"resource_type", "amount", "strategy_module", "confidence_score",
	}
	assert expected_keys == set(record.keys())
	assert record["strategy_module"] == "military"
	assert record["confidence_score"] == 0.9


def test_disabled_flag_skips_write(tmp_path):
	log_path = str(tmp_path / "decisions.jsonl")
	dl = DecisionLogger(log_path=log_path, enabled=False)

	dl.log(game_tick=1, decision_type="attack")

	assert not os.path.exists(log_path)


def test_tail_returns_last_n_records(tmp_path):
	log_path = str(tmp_path / "decisions.jsonl")
	dl = DecisionLogger(log_path=log_path)

	for i in range(10):
		dl.log(game_tick=i, decision_type="attack")

	records = dl.tail(limit=3)
	assert len(records) == 3
	assert records[0]["game_tick"] == 7
	assert records[2]["game_tick"] == 9


def test_tail_returns_empty_when_no_file(tmp_path):
	log_path = str(tmp_path / "missing.jsonl")
	dl = DecisionLogger(log_path=log_path)
	assert dl.tail() == []
