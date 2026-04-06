"""Unit tests for the main.py game loop wiring.

Verifies that DecisionEngine.run_tick is called exactly once per new game
tick, and is NOT called again when the server returns the same tick.
"""
from __future__ import annotations

from unittest.mock import MagicMock, patch

import pytest

from agent1.config import AgentConfig
from agent1.main import run


def _make_tick(next_tick_at: str) -> dict:
	return {
		"serverTime": "2026-01-01T00:00:00Z",
		"nextTickAt": next_tick_at,
		"unreadMessageCount": 0,
	}


def _make_resources() -> dict:
	return {
		"primaryResource": {"cost": {"minerals": 500}},
		"secondaryResources": {"cost": {"gas": 100, "land": 10}},
		"colonizationCostPerLand": 0,
	}


def _make_units() -> dict:
	return {"units": []}


def _make_attackable() -> dict:
	return {"attackablePlayers": []}


def _run_loop_for_n_polls(config: AgentConfig, tick_info_sequence: list[dict], mock_engine: MagicMock) -> None:
	"""Patch run() internals and drive *n* poll iterations, then stop."""
	poll_count = 0

	def fake_sleep(_secs: float) -> None:
		nonlocal poll_count
		poll_count += 1
		if poll_count >= len(tick_info_sequence):
			raise StopIteration

	mock_client = MagicMock()
	mock_client.get_tick_info.side_effect = tick_info_sequence
	mock_client.get_resources.return_value = _make_resources()
	mock_client.get_units.return_value = _make_units()
	mock_client.get_attackable_players.return_value = _make_attackable()

	with (
		patch("agent1.main.BgeClient", return_value=mock_client),
		patch("agent1.main.DecisionEngine", return_value=mock_engine),
		patch("agent1.main.load_strategy"),
		patch("agent1.main.ActionDispatcher"),
		patch("agent1.main.start_server"),
		patch("agent1.main.time.sleep", side_effect=fake_sleep),
	):
		with pytest.raises(StopIteration):
			run(config)


def test_run_tick_called_once_per_new_tick() -> None:
	"""engine.run_tick is called exactly once when the tick advances."""
	config = AgentConfig(api_key="bge_k_test", game_id="g1")
	mock_engine = MagicMock()

	# Poll 1: tick A (new → run_tick called)
	# Poll 2: tick A again (same → no call)
	# Poll 3: tick B (new → run_tick called again) — drives StopIteration
	ticks = [
		_make_tick("2026-01-01T00:01:00Z"),
		_make_tick("2026-01-01T00:01:00Z"),
		_make_tick("2026-01-01T00:02:00Z"),
	]
	_run_loop_for_n_polls(config, ticks, mock_engine)

	assert mock_engine.run_tick.call_count == 2


def test_run_tick_not_called_on_repeated_same_tick() -> None:
	"""engine.run_tick is NOT called when the same nextTickAt is returned twice."""
	config = AgentConfig(api_key="bge_k_test", game_id="g1")
	mock_engine = MagicMock()

	# All three polls return the same tick
	ticks = [
		_make_tick("2026-01-01T00:01:00Z"),
		_make_tick("2026-01-01T00:01:00Z"),
		_make_tick("2026-01-01T00:01:00Z"),
	]
	_run_loop_for_n_polls(config, ticks, mock_engine)

	# First poll is always "new" (last_next_tick_at starts empty), second and
	# third are duplicates — only 1 call total.
	assert mock_engine.run_tick.call_count == 1


def test_loop_continues_after_engine_exception() -> None:
	"""A crash inside run_tick does not kill the loop."""
	config = AgentConfig(api_key="bge_k_test", game_id="g1")
	mock_engine = MagicMock()
	mock_engine.run_tick.side_effect = RuntimeError("strategy exploded")

	ticks = [
		_make_tick("2026-01-01T00:01:00Z"),
		_make_tick("2026-01-01T00:02:00Z"),
		_make_tick("2026-01-01T00:03:00Z"),
	]
	# Should not raise — the loop catches exceptions
	_run_loop_for_n_polls(config, ticks, mock_engine)

	# run_tick was attempted 3 times despite crashing each time
	assert mock_engine.run_tick.call_count == 3
