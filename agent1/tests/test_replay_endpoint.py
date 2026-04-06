"""Tests for the /replay and /outcomes HTTP endpoints."""
from __future__ import annotations

import json
from unittest.mock import MagicMock

import pytest

from agent1.server import start_server


def _start_test_server(decision_logger, outcome_tracker, port: int):
	"""Start a server on a random port for testing and return it."""
	server = start_server(decision_logger, outcome_tracker, host="127.0.0.1", port=port)
	return server


def _get(server, path: str) -> tuple[int, object]:
	"""Make a GET request to the test server and return (status, parsed_json)."""
	import http.client

	host, port = server.server_address
	conn = http.client.HTTPConnection(f"{host}:{port}", timeout=5)
	conn.request("GET", path)
	response = conn.getresponse()
	status = response.status
	body = json.loads(response.read())
	conn.close()
	return status, body


def test_replay_endpoint_returns_records():
	records = [
		{"game_tick": 1, "decision_type": "attack"},
		{"game_tick": 2, "decision_type": "build_units"},
	]
	decision_logger = MagicMock()
	decision_logger.tail.return_value = records
	outcome_tracker = MagicMock()
	outcome_tracker.tail.return_value = []

	server = _start_test_server(decision_logger, outcome_tracker, port=19871)
	try:
		status, body = _get(server, "/replay?limit=10")
		assert status == 200
		assert body == records
		decision_logger.tail.assert_called_once_with(10)
	finally:
		server.shutdown()


def test_outcomes_endpoint_returns_records():
	outcomes = [{"game_tick": 3, "minerals_delta": 50}]
	decision_logger = MagicMock()
	decision_logger.tail.return_value = []
	outcome_tracker = MagicMock()
	outcome_tracker.tail.return_value = outcomes

	server = _start_test_server(decision_logger, outcome_tracker, port=19872)
	try:
		status, body = _get(server, "/outcomes?limit=5")
		assert status == 200
		assert body == outcomes
		outcome_tracker.tail.assert_called_once_with(5)
	finally:
		server.shutdown()


def test_replay_default_limit():
	decision_logger = MagicMock()
	decision_logger.tail.return_value = []
	outcome_tracker = MagicMock()
	outcome_tracker.tail.return_value = []

	server = _start_test_server(decision_logger, outcome_tracker, port=19873)
	try:
		_get(server, "/replay")
		decision_logger.tail.assert_called_once_with(50)
	finally:
		server.shutdown()


def test_unknown_path_returns_404():
	decision_logger = MagicMock()
	decision_logger.tail.return_value = []
	outcome_tracker = MagicMock()
	outcome_tracker.tail.return_value = []

	server = _start_test_server(decision_logger, outcome_tracker, port=19874)
	try:
		status, body = _get(server, "/nonexistent")
		assert status == 404
		assert "error" in body
	finally:
		server.shutdown()
